using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 씬에 아무것도 미리 놓지 않아도(빈 씬이어도) 게임이 부팅되게 하는 진입점
    /// (재사용 노트의 GameBootstrap 패턴). UI를 전부 코드로 짓는 이 프로젝트
    /// 방침대로, Canvas/EventSystem까지 전부 여기서 코드로 만든다.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var root = new GameObject("GameBootstrap");
            Object.DontDestroyOnLoad(root);

            EnsureEventSystem(root.transform);

            var canvas = UiFactory.CreateRootCanvas();
            canvas.transform.SetParent(root.transform, false);

            TitleScreen.Build(canvas.transform, "WaterSort", "같은 색을 모아 정리하세요!", new TitleScreen.Callbacks
            {
                OnStart = () => Debug.Log("[GameBootstrap] 시작 버튼 클릭 — 게임 화면 연결은 다음 단계"),
                OnSettings = () => Debug.Log("[GameBootstrap] 설정 버튼 클릭 — 설정 화면은 다음 단계"),
                OnQuit = () => Application.Quit()
            });
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
        }
    }
}
