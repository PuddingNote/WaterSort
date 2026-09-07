using System.Threading.Tasks;
using ColorSort.Core;
using ColorSort.Managers;
using ColorSort.Solver;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 씬에 아무것도 미리 놓지 않아도(빈 씬이어도) 게임이 부팅되게 하는 진입점
    /// (재사용 노트의 GameBootstrap 패턴). Canvas/EventSystem부터 화면 전환까지
    /// 전부 코드로 만든다.
    ///
    /// 라운드 진행: "다음에 시작할 라운드 번호"만 <see cref="ProgressStore"/>로
    /// 저장한다(중단된 판의 중간 상태는 저장 안 함 — 정책 확정, GameDesign.md
    /// 참고). 같은 라운드는 항상 같은 배치로 재생성돼야 하므로, 라운드 생성용
    /// 난수는 **라운드 번호 자체를 시드로 매번 새로 만든다**(공유 rng를 계속
    /// 돌리면 같은 라운드라도 호출 시점마다 결과가 달라진다).
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var root = new GameObject("GameBootstrap");
            UnityEngine.Object.DontDestroyOnLoad(root);
            EnsureEventSystem(root.transform);

            var canvas = UiFactory.CreateRootCanvas();
            canvas.transform.SetParent(root.transform, false);

            int roundId = ProgressStore.LoadNextRoundId();
            RectTransform activeScreen = null;
            bool transitioning = false; // Start 연타 등으로 ShowGame이 겹쳐 들어가는 것을 막음.

            void ShowTitle()
            {
                if (activeScreen != null) UnityEngine.Object.Destroy(activeScreen.gameObject);
                var title = TitleScreen.Build(canvas.transform, "WaterSort", "Sort the colors to clear the puzzle!", new TitleScreen.Callbacks
                {
                    OnStart = ShowGame,
                    OnSettings = () => Debug.Log("[GameBootstrap] 설정 — 아직 화면 없음"),
                    OnQuit = QuitGame
                });
                activeScreen = (RectTransform)title.transform;
            }

            // async void — TitleScreen.Callbacks.OnStart(Action<int?>)에 그대로 꽂는
            // 이벤트 핸들러라 async Task로 안 만든다(GameView.OnHintClicked과 같은 이유:
            // 예외가 나면 Unity SynchronizationContext를 통해 콘솔에 그대로 로그된다 —
            // Task를 반환해서 _ = ShowGame(...)처럼 버리면 오히려 예외가 조용히 묻힌다).
            async void ShowGame(int? overrideRoundId = null)
            {
                if (transitioning) return;
                transitioning = true;
                try
                {
                    // 에디터 테스트 입력으로 들어온 라운드가 있으면 그걸로 강제 진입
                    // (이후 클리어 진행은 이 번호부터 정상적으로 이어짐 — 저장도 갱신됨).
                    if (overrideRoundId.HasValue) roundId = overrideRoundId.Value;
                    int thisRoundId = roundId;

                    // 라운드 번호 = 생성 시드. 같은 라운드를 몇 번을 다시 열어도 항상 같은 배치.
                    var roundRng = new System.Random(thisRoundId);

                    // RoundBuilder.Build는 보드를 생성하면서 자체적으로 풀리는지 검증하고,
                    // 실측 수까지 한 번 더 계산한다 — 라운드가 많이 진행돼서 어려워질수록
                    // 이 계산이 눈에 띄게 오래 걸릴 수 있다(실제로 겪은 렉: Start를 누르는
                    // 순간 화면이 잠깐 멈춤). GameView.OnHintClicked과 같은 이유로 메인
                    // 스레드를 막지 않도록 백그라운드로 옮긴다(RoundBuilder/RoundGenerator/
                    // HintSolver 전부 UnityEngine 의존이 없는 순수 C#이라 안전).
                    var buildTask = Task.Run(() => RoundBuilder.Build(thisRoundId, WaterPalette.ThemeLimits, roundRng));

                    // 금방 끝나면(쉬운 라운드 등) 로딩 화면을 아예 안 띄우고, 일정 시간
                    // 넘도록 안 끝나면 그때 가서 띄운다(사용자 확정) — 그동안 이전 화면
                    // (타이틀 또는 클리어 직전 게임 화면)은 그대로 보여준 채로 둔다.
                    RectTransform loading = null;
                    var winner = await Task.WhenAny(buildTask, Task.Delay(UiTheme.LoadingOverlayShowDelayMs));
                    if (winner != buildTask)
                    {
                        loading = HintLoadingOverlay.Show(canvas.transform);
                        await buildTask;
                    }
                    if (loading != null) HintLoadingOverlay.Hide(loading);

                    var result = buildTask.Result;
                    var session = new PuzzleSession(result.Board);

                    if (activeScreen != null) UnityEngine.Object.Destroy(activeScreen.gameObject);

                    var gameView = GameView.Build(canvas.transform, thisRoundId, session, new GameView.Callbacks
                    {
                        OnBack = ShowTitle,
                        OnCleared = () =>
                        {
                            roundId++;
                            ProgressStore.SaveNextRoundId(roundId);
                            ShowGame();
                        }
                    });
                    activeScreen = (RectTransform)gameView.transform;
                }
                finally
                {
                    transitioning = false;
                }
            }

            ShowTitle();
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
        }

        /// <summary>Application.Quit()은 에디터 안에서는 아무 일도 안 한다(Unity
        /// 자체 제약) — 그래서 에디터에서는 Play 모드를 직접 꺼서 실제 빌드의
        /// "종료"와 같은 체감을 준다.</summary>
        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
