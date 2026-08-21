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

## 아직 정하지 않은 것

- Core/Solver 실제 게임 로직(Board, MoveValidator, 역방향 생성 알고리즘 등)은
  이번 스캐폴딩에는 포함하지 않았다 — `docs/GameDesign.md`의 확정 필요 정책
  항목(색상 접근성, DifficultyScore 가중치, 병 추가 20% 슬롯 기준 등)이 먼저
  정리되어야 구현 방향이 흔들리지 않는다.
- 광고 SDK 선택(AdMob vs 대안), 강제 업데이트/개인정보처리방침용 허브 저장소
  (`{계정}.github.io`) 준비 시점은 아직 안 다뤘다 — 필요해지는 시점(광고를 넣기로
  정하는 시점)에 `캐주얼_게임_재사용_시스템_모음.md` 2장 / `개인정보처리방침_재사용_가이드.md`를
  다시 참고해 진행한다.
