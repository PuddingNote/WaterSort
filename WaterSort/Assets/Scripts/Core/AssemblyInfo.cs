using System.Runtime.CompilerServices;

// Tests.EditMode에서 Board 등 모델의 internal setter/생성자를 직접 써서
// 원하는 국면을 구성할 수 있게 한다. Core는 UnityEngine 비의존을 유지한다.
[assembly: InternalsVisibleTo("ColorSort.Tests.EditMode")]
