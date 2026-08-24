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

            void ShowGame(int? overrideRoundId = null)
            {
                // 에디터 테스트 입력으로 들어온 라운드가 있으면 그걸로 강제 진입
                // (이후 클리어 진행은 이 번호부터 정상적으로 이어짐 — 저장도 갱신됨).
                if (overrideRoundId.HasValue) roundId = overrideRoundId.Value;

                if (activeScreen != null) UnityEngine.Object.Destroy(activeScreen.gameObject);

                // 라운드 번호 = 생성 시드. 같은 라운드를 몇 번을 다시 열어도 항상 같은 배치.
                var roundRng = new System.Random(roundId);
                var result = RoundBuilder.Build(roundId, WaterPalette.ThemeLimits, roundRng);
                var session = new PuzzleSession(result.Board);

                var gameView = GameView.Build(canvas.transform, roundId, session, new GameView.Callbacks
                {
                    OnBack = ShowTitle,
                    OnSettings = () => Debug.Log("[GameBootstrap] 설정 — 아직 화면 없음"),
                    OnCleared = () =>
                    {
                        roundId++;
                        ProgressStore.SaveNextRoundId(roundId);
                        ShowGame();
                    }
                });
                activeScreen = (RectTransform)gameView.transform;
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
