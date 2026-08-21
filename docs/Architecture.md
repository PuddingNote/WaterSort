# 아키텍처 결정 기록 (WaterSort)

> "무엇을 했는지"가 아니라 "왜 이렇게 정했는지"를 남기는 문서. 실물 asmdef 등은
> 코드 자체가 말해주므로 여기서는 반복하지 않는다.

## 이 프로젝트의 성격: 범용 엔진 + WaterSort 테마

이 저장소는 **색깔 정렬(Water Sort류) 퍼즐의 범용 엔진을 이 프로젝트에서 먼저
만들고, 그 위에 "물병의 물" 테마를 얹는 순서**로 진행한다. 이후 다른 소재
(볼트-너트, 레고 블록 등)로 새 게임을 만들 때는 이 저장소를 통째로 복사해
Presentation 계층(아트/연출/사운드)만 교체하는 것을 전제로 한다. 그래서 게임
로직 쪽 코드는 "물"이나 "병"을 모르게 짜고, 테마 고유의 것(붓기 연출, 병 비주얼
등)만 별도 계층에 둔다. 근거: `색깔정렬게임_기획서_범용형.md` 5장(Core Logic
Layer vs Presentation Layer), `모바일_캐주얼_게임_재사용_노트.md` 1장.

## asmdef 프리픽스: `ColorSort` (WaterSort가 아님)

일부러 프로젝트 이름(WaterSort)이 아니라 장르 이름(`ColorSort`)을 어셈블리
프리픽스로 썼다. 이 저장소를 복사해 다른 소재의 게임을 만들 때, 로직
어셈블리 이름이 "WaterSort.Core"처럼 이전 게임 이름을 달고 있으면 오히려
헷갈린다 — 복사 후 지울 것과 남길 것을 asmdef 이름만 보고 구분할 수 있어야
하므로, 소재 무관 로직은 항상 `ColorSort.*`로 유지한다. (물/병 관련 프리젠테이션
코드는 UI 어셈블리 안에 네임스페이스나 폴더로만 구분하고, 별도 어셈블리로
쪼개지 않는다 — 배포 전까지는 테마가 하나뿐이라 어셈블리를 더 쪼갤 이유가
없다. 테마가 실제로 여러 개 공존해야 하는 상황이 오면 그때 다시 판단한다.)

## 레이어 구성

```
Assets/
  Scripts/
    Core/      ColorSort.Core     — Board, Container, Unit(색상) 등 순수 모델 + 이동 판정 규칙.
                                    noEngineReferences:true, UnityEngine 심볼 자체를 못 씀.
    Solver/    ColorSort.Solver   — Core만 참조. 힌트 솔버(최적 다음 수), 라운드 자동 생성
                                    (역방향 셔플), 난이도 점수화. 이 게임의 "AI" 자리에 해당하는
                                    도메인 계층 (재사용 노트 1장: 게임에 AI가 없으면 핵심 도메인
                                    이름으로 바꾼다 — 여기서는 퍼즐이므로 Solver).
    Managers/  ColorSort.Managers — 외부 SDK(광고 등) 연동. SDK 타입이 UI로 새어나가지 않게 여기서만 참조.
    UI/        ColorSort.UI      — MonoBehaviour, 코드 기반 UI(UiFactory/UiTheme), 렌더링/입력.
                                    물병 비주얼·붓기 연출 등 WaterSort 테마 코드도 지금은 여기 안에 위치.
  Editor/      ColorSort.Editor  — 디버그 도구 등 에디터 전용.
  Tests/EditMode/ ColorSort.Tests.EditMode — Core+Solver 순수 로직 단위 테스트. UI는 참조하지 않음.
  Scriptable Object/ — 밸런스 수치(팔레트, 난이도 커브 등)를 담는 SO 에셋.
```

`Core`/`Solver`에는 `[assembly: InternalsVisibleTo("ColorSort.Tests.EditMode")]`를
심어 뒀다 — 모델 setter를 `internal`로 캡슐화하면서도 테스트에서 원하는 보드
국면을 직접 구성할 수 있게.

## UI: 코드 기반 (프리팹 없음)

`재사용 노트` 4장 판단을 그대로 따른다 — 1인 개발 + git diff 중요성 기준으로
`UiFactory`/`UiTheme` 패턴을 채택 예정. 프리팹/씬 편집 대신 런타임 코드로 UI를
쌓는다.

## Core/Solver 구현 완료 (2026-08-21)

Board/Container/MoveRules/ClearChecker/BoardHistory/PuzzleSession(Core)과
RoundGenerator/HintSolver/DifficultyScorer(Solver)를 구현하고, 스크래치패드
`dotnet test` 하네스(재사용 노트 2장)로 Unity 없이 검증했다(2026-08-21 기준
18개 EditMode 테스트 통과).

**라운드 생성 — 역방향 생성 중 발견한 버그와 수정**: 처음엔 역연산(색 덩어리를
다른 막대로 옮기기)에서 "도착 막대도 색이 맞아야 한다"는 조건을 정방향
`MoveRules.CanMove`와 동일하게 걸었는데, 이러면 **한 막대 안에 서로 다른 색이
수학적으로 절대 섞일 수 없다**(실제로 생성해서 찍어보고 발견 — 병마다 항상
단색이었음). 도착 쪽 색 제약을 없애고(자리만 있으면 어떤 색 위에도 얹을 수
있게) 출발 쪽 안전 조건(덩어리를 전부 걷어내 다른 색이 드러나면 안 됨)만
유지하도록 고쳤다 — 이후 여러 시드에서 실제로 뒤섞인 배치가 나오고 항상
클리어 가능함을 확인.

**힌트/생성-검증 솔버 — BFS의 한계와 휴리스틱 탐색 도입**: 위 수정 후에도
9색×9칸 근처(기획서 6.5의 "101+" 최고난도 구간)에서 순수 BFS 최단경로 탐색이
500만 states를 뒤져도 답을 못 찾는 사례가 나왔다 — 실제로 못 푸는 보드가
아니라, 최단 해가 길어지면 가지치기 없는 BFS가 지수적으로 감당 못 하는
것(실제 Water Sort 솔버들이 A*/IDA*류를 쓰는 이유와 동일). **사용자 확인 후,
"색 전이(transition) 수"를 휴리스틱으로 쓰는 가중치 Best-First 탐색을
추가**했다(`HintSolver.FindSolutionHeuristic`, `MinHeap<T>`는 Unity 런타임
호환성 때문에 BCL `PriorityQueue` 대신 직접 구현). 최단 경로는 보장하지
않지만 "풀리는 경로 하나"는 최고난도 구간에서도 수 ms 안에 찾는다.
- 라운드 생성 검증(존재 여부만 필요) → 휴리스틱 탐색만 사용.
- 실제 힌트 버튼(`HintSolver.FindNextMove`) → 먼저 BFS(작은 예산)로 진짜
  최적수를 시도하고, 못 찾으면 휴리스틱 탐색으로 폴백 — 일반적인 플레이
  상태(어느 정도 정리된 보드)에서는 최적수를, 극단적으로 뒤섞인 상태에서도
  "유효한 다음 수"를 보장한다.

## 라운드 번호 → 파라미터 자동 매핑 + 자기대전 실측 (2026-08-21)

`RoundDifficultyCurve.SampleParameters(roundId, themeLimits, rng)`가 범용형
기획서 6.5/6.6(물병판 7.5/7.6)의 "라운드 구간별 목표 난이도" 표를 구현한다 —
**절대 수치가 아니라 테마의 [min,max] 범위에 대한 비율(fraction)로 구간을
정의**해서, 색상 팔레트 한도가 다른 소재(예: 6색뿐인 테마)에도 그대로
재사용된다(`ThemeLimits`만 테마별로 바꿔 끼운다). `RoundBuilder.Build(roundId,
limits, rng)`가 실제 게임이 부를 단일 진입점 — 샘플링→생성→실측
난이도(`DifficultyScorer`)가 목표와 너무 벗어나면 shuffleDepth를 조정해
재시도한다(기획서 6.6 의사코드의 "adjust params and retry" 그대로).

**자기대전 실측 결과(재사용 노트 3장 원칙)**: 라운드 1~150 구간을 라운드당
15회씩 헤드리스로 생성해 측정한 결과, 평균 색상 수(3.5→8.5)·슬롯 수
(4.3→8.8)·최단 클리어 수(5.7→17~18)·DifficultyScore(22→194)가 전부 우상향—
100 라운드 근처부터는 색상/슬롯이 테마 최대치에 근접해 완만해지지만 여전히
증가 추세. 라운드 1개 생성(검증+보정 재시도 포함) 평균 수 ms 이내로 실시간
생성에 문제없음. **단, 여기서 "실측"은 자기대전(솔버 기준 최단 클리어 수)
검증이지 실제 사람이 체감하는 난이도 검증이 아니다 — 사람 플레이테스트
기반 가중치/커브 계수 조정은 여전히 남아 있다** (아래 "아직 정하지 않은 것"
참고).

## 아직 정하지 않은 것

- `docs/GameDesign.md`의 확정 필요 정책 항목(색상 접근성, DifficultyScore
  가중치 실측값, 병 추가 20% 슬롯 기준 등)은 여전히 미정 — UI/Managers 단계에서
  실제로 필요해질 때 확인한다.
- 광고 SDK 선택(AdMob vs 대안), 강제 업데이트/개인정보처리방침용 허브 저장소
  (`{계정}.github.io`) 준비 시점은 아직 안 다뤘다 — 필요해지는 시점(광고를 넣기로
  정하는 시점)에 `캐주얼_게임_재사용_시스템_모음.md` 2장 / `개인정보처리방침_재사용_가이드.md`를
  다시 참고해 진행한다.
