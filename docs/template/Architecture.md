# 아키텍처 결정 기록 ({게임 이름})

> WaterSort 프로젝트를 복사해 만든 새 소재({소재}) 프로젝트. 이 문서는
> WaterSort의 `docs/Architecture.md`를 소재만 바꿔 옮긴 것 — 아키텍처 결정
> 자체(레이어 구성, asmdef 프리픽스, 생성/솔버 알고리즘)는 소재 무관이라
> WaterSort와 동일하게 유지한다. "무엇을 했는지"가 아니라 "왜 이렇게
> 정했는지"를 남기는 문서 원칙도 동일.

## 이 프로젝트의 성격: WaterSort에서 갈라져 나온 같은 엔진 + {소재} 테마

Core/Solver(`ColorSort.*`)는 WaterSort와 **완전히 동일한 코드**를 그대로
가져왔다 — 색깔 정렬 퍼즐의 이동 규칙, 라운드 생성, 힌트 솔버, 난이도 곡선은
소재와 무관하게 설계돼 있어 손댈 필요가 없다(근거: WaterSort
`docs/Architecture.md`, `색깔정렬게임_기획서_범용형.md` 5장). 이 프로젝트에서
새로 만드는 건 Presentation 계층(UI 안의 {소재} 비주얼/연출)뿐이다.

이후 또 다른 소재로 게임을 만들 때는 **이 프로젝트가 아니라 WaterSort(또는
가장 최근에 정리된 마스터 버전)를 다시 복사하는 것을 권장** — 이 프로젝트에서
{소재} 전용으로 UI를 채우고 나면, 이 저장소 자체는 더 이상 "빈 템플릿"이
아니게 되기 때문이다.

## asmdef 프리픽스: 여전히 `ColorSort`

WaterSort에서 이미 내린 결정(소재 이름이 아니라 장르 이름을 어셈블리
프리픽스로 쓴다)을 그대로 유지한다 — 이 프로젝트도 또 복사될 수 있으므로.
`ColorSort.Core`/`ColorSort.Solver`는 WaterSort와 동일 코드, `ColorSort.UI`
안에서만 {소재} 관련 네임스페이스/폴더로 구분한다.

## 레이어 구성 (WaterSort와 동일)

```
Assets/
  Scripts/
    Core/      ColorSort.Core     — Board, Container, Unit(색상) 등 순수 모델 + 이동 판정 규칙.
    Solver/    ColorSort.Solver   — 힌트 솔버, 라운드 자동 생성(역방향 셔플+휴리스틱 검증),
                                    난이도 점수화, 라운드→파라미터 커브. WaterSort와 동일 코드.
    Managers/  ColorSort.Managers — 외부 SDK(광고 등) 연동. 이 프로젝트에서 광고 SDK를
                                    다시 선택/설정해야 함(WaterSort와 같은 SDK를 쓸지도 사용자 판단).
    UI/        ColorSort.UI      — {소재} 비주얼·연출 코드가 여기 새로 채워짐.
  Editor/      ColorSort.Editor
  Tests/EditMode/ ColorSort.Tests.EditMode — Core+Solver 테스트는 WaterSort와 동일하게 그대로 통과해야 함
                                              (복사 직후 `dotnet test` 하네스로 먼저 확인 권장).
  Scriptable Object/ — {소재}의 색상 팔레트, 난이도 커브 등 밸런스 수치.
```

## 라운드 생성/난이도 곡선 — `ThemeLimits`만 교체

`RoundDifficultyCurve.ThemeLimits`(색상/슬롯/여유막대 범위)만 이 소재에 맞게
새로 설정하면, 코드 수정 없이 라운드→파라미터 자동 매핑이 그대로 동작한다.
WaterSort에서 했던 것처럼 **이 소재의 범위로 다시 헤드리스 자기대전을 돌려
곡선이 우상향하는지 실측 확인**할 것(재사용 노트 3장) — WaterSort의 실측값
(색상 3.5→8.5, 슬롯 4.3→8.8 등)은 색상 9종/슬롯 4~9칸이라는 WaterSort 전용
범위에서 나온 수치라 이 소재에 그대로 적용되지 않는다.

## 복사 직후 체크리스트

1. `dotnet test` 하네스(스크래치패드에 재구성 — 재사용 노트 2장 템플릿)로
   Core/Solver 테스트가 전부 통과하는지 먼저 확인(코드를 아직 안 건드렸으니
   당연히 통과해야 하고, 안 되면 복사 과정에서 뭔가 빠진 것).
2. 이 문서와 `docs/GameDesign.md`의 `{...}` 자리표시자를 채운다.
3. Unity Product Name/패키지 식별자 등은 `docs/template/README.md` 체크리스트
   참고.
4. 그 다음부터 UI 작업 시작 — 여기서부터는 WaterSort와 완전히 다른 코드가
   쌓이기 시작한다.

## 아직 정하지 않은 것

- `docs/GameDesign.md`의 확정 필요 정책 항목 — WaterSort와 같은 목록으로
  시작하되, {소재} 특화 항목(이동 연출 등)은 이 프로젝트에서 새로 확인해야 함.
- 광고 SDK 선택, 강제 업데이트/개인정보처리방침용 허브 저장소
  (`{계정}.github.io`) 폴더 준비 — 광고를 넣기로 정하는 시점에
  `캐주얼_게임_재사용_시스템_모음.md` 2장 / `개인정보처리방침_재사용_가이드.md`
  참고해 진행(WaterSort와 같은 허브 저장소를 쓴다면 그 안에 이 게임 이름의
  폴더만 새로 추가하면 됨 — 1장 참고).
