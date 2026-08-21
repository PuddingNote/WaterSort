using System.Runtime.CompilerServices;

// Tests.EditMode에서 Board 등 모델의 internal setter/생성자를 직접 써서
// 원하는 국면을 구성할 수 있게 한다. Core는 UnityEngine 비의존을 유지한다.
[assembly: InternalsVisibleTo("ColorSort.Tests.EditMode")]

// Solver(라운드 생성/힌트 솔버)는 Container를 직접 밀어넣고 빼내며 국면을
// 탐색해야 하므로, Core의 internal 조작 API(Push/Pop 등)를 그대로 쓸 수 있게
// 허용한다. UI/Managers에는 내주지 않는다 — 그쪽은 항상 MoveRules를 거친다.
[assembly: InternalsVisibleTo("ColorSort.Solver")]
