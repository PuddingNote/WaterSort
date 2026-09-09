# 스프라이트 준비 목록

> UI 코드(`UiFactory`/`UiTheme`)는 스프라이트가 없어도 단색 사각형으로 대체되게
> 짜여 있다 — 그리는 순서와 무관하게 코드 작업이 막히지 않는다. 이 문서는
> "결국 필요한 것"을 화면별로 정리한 것이고, 완성되는 대로 하나씩 채워 넣으면
> 그 즉시 코드에서 반영된다(`TODO(sprite): ...` 주석이 박힌 자리부터 먼저 채우면
> 됨). 파일 위치는 `Assets/Sprites/` 아래(테마 프리젠테이션이라 UI 계층 소관).

**공통 규격**: PNG(투명 배경), 2배 해상도로 제작해 Unity에서 다운스케일 권장.
버튼/아이콘류는 9-slice(Sliced) 대응 가능하게 모서리를 둥글리고 안전 여백을
둘 것. 기준 해상도는 1080×1920(세로).

---

## 0. 고정 시스템 그림 (테마 무관 — UiSkin이 아니라 UiTheme가 직접 로드)

폰트(`ONE Mobile POP SDF`)와 마찬가지로, 병/물처럼 소재별로 갈아 끼우는 게
아니라 이 UI 시스템 자체가 항상 쓰는 그림들. `Assets/Resources/Sprites/`에
정해진 파일명으로 두기만 하면 코드 수정 없이 반영된다(Inspector 연결 불필요).

| 파일명 | 용도 |
|---|---|
| `circle.png` | 로딩 스피너 기본 대체 그림(`UiSkin.Loading Spinner`가 비어 있을 때만 씀) — 흰색 원형 실루엣 하나로 충분(`Image.Type.Filled`+`Radial360`으로 코드가 파이 조각처럼 채워서 돌림) |

로딩 스피너 자체는 `UiSkin.Loading Spinner`에 Inspector로 연결 가능하다
(2026-09-07) — 도트가 점점 흐려지는 것처럼 "잔상"이 그림 자체에 이미
들어있는 디자인을 권장한다(정사각형, 도트 배치가 캔버스 가운데 정렬).
`HintLoadingOverlay`가 그 그림을 그대로 회전만 시켜서(별도 애니메이션 프레임
불필요) 로딩 중 느낌을 낸다. 비워두면 위 `circle.png` 대체 그림으로 자동
전환된다.

## 1. 공통 아이콘 (전 화면에서 재사용)

| 이름 | 용도 | 권장 크기 | 형태 | 우선순위 |
|---|---|---|---|---|
| `icon_settings_gear` | 설정 버튼(톱니바퀴) | 64×64 | 단색 실루엣(틴트로 색 입힘) | 중간 — 타이틀 화면에서만 씀(게임 플레이 화면엔 설정 버튼 자체가 없음, 2026-09-07 확정). `UiSkin.Settings Icon`에 Inspector로 바로 연결 가능(2026-08-25) |
| `icon_back_arrow` | 뒤로가기 | 48×48 | 실루엣 | 높음 — `UiSkin.Back Icon`에 Inspector로 바로 연결 가능(2026-08-25) |
| `icon_close_x` | 팝업 닫기 | 48×48 | 실루엣 | 중간 |
| `icon_undo` | 실행취소 버튼 | 64×64 | 실루엣(반시계 화살표) | **높음** — `UiSkin.Undo Icon`에 Inspector로 바로 연결 가능(2026-08-25) |
| `icon_reset` | 초기화 버튼 | 64×64 | 실루엣(새로고침 화살표) | **높음** — `UiSkin.Reset Icon`에 Inspector로 바로 연결 가능(2026-08-25) |
| `icon_hint_bulb` | 힌트 버튼 | 64×64 | 실루엣(전구) | **높음** — `UiSkin.Hint Icon`에 Inspector로 바로 연결 가능(2026-08-25) |
| `icon_add_container` | 막대/병 추가 버튼 | 64×64 | 실루엣(+ 또는 새 병 아이콘) | **높음** — `UiSkin.Add Container Icon`에 Inspector로 바로 연결 가능(2026-08-25) |
| `icon_play_ad` | 보상형 광고 시청 버튼 표시 | 40×40 | 실루엣(▶ 또는 광고 마크) | 낮음(광고 SDK 붙일 때) |
| `icon_coin` | 재화(코인) 표시 | 40×40 | 컬러 아이콘 | 낮음(경제 시스템 붙일 때) |
| `icon_sound_on` / `icon_sound_off` | 사운드 토글 | 56×56 | 실루엣 2종(on/off) | 중간(설정 화면) |
| `icon_lock` | 잠긴 기능 표시(선택) | 40×40 | 실루엣 | 낮음 |

## 2. 버튼/패널 배경 (9-slice) — Inspector에서 직접 연결

`bg_button_rounded`, `bg_panel_rounded`는 이제 코드를 안 건드리고 Inspector로
바로 연결할 수 있다 — `Assets/Resources/UiSkin.asset`(없으면 새로 만들기:
Project 창 우클릭 → Create → ColorSort → UI Skin, 반드시 이 경로에 저장)의
`Button Background` / `Icon Button Background`(모든 아이콘 버튼 공용 배경) /
`Dialog Background` 필드에 끌어다 놓기만 하면 다음 Play부터 바로 반영된다
(자세한 구조는 Desktop의 `캐주얼_게임_UI_레이아웃_컨벤션.md` 5장 참고). 비어
있으면 지금처럼 단색으로 자동 대체되니 순서 상관없이 진행 가능.

**아이콘 버튼 전경 그림**(배경 위에 얹히는 실제 그림)도 버튼별로 Inspector에서
따로 연결 가능 — 현재 `Settings Icon`/`Back Icon`/`Undo Icon`/`Reset Icon`/
`Hint Icon`/`Add Container Icon` 6개 필드가 전부 있다(2026-08-25, 사용자
요청). 비워두면 지금처럼 텍스트 자리표시자(SETTINGS/BACK/UNDO/RESET/HINT/ADD)로
자동 대체되니 그림 완성 순서와 무관하게 하나씩 채워 넣으면 됨. `icon_close_x`
(팝업 닫기)만 아직 Inspector 필드가 없음 — 필요해지면 같은 패턴으로 추가.

| 이름 | 용도 | 권장 크기(원본) | 형태 | 우선순위 |
|---|---|---|---|---|
| `bg_button_rounded` | 모든 버튼 공용 배경(색은 코드에서 버튼마다 다르게 틴트) → `UiSkin.ButtonBackground` | 400×120, 모서리 반경 28px | 둥근 사각형, 9-slice | **높음** |
| `bg_dialog_rounded` | 확인 다이얼로그 배경 → `UiSkin.DialogBackground` | 820×400, 모서리 반경 32px | 둥근 사각형, 9-slice | 중간 |
| `bg_button_icon_circle` | 아이콘 전용 원형 버튼 배경(설정 등) | 96×96 | 원형 | 중간 |
| `bg_badge_pill` | 잔여 횟수 뱃지 배경(힌트/병추가 옆) | 48×48 | 알약/원형 | 중간 |

`overlay_dim`(팝업 뒤 딤 처리)은 스프라이트 필요 없음 — `UiTheme.DimBackground`
반투명 검정색으로 코드가 이미 완전히 처리함.

## 3. 타이틀 화면

| 이름 | 용도 | 권장 크기 | 우선순위 |
|---|---|---|---|
| `bg_title` | 배경(어두운 네이비 + 낙서풍 옅은 패턴, GameDesign.md 5.1) | 1080×1920 | 중간 — 지금은 `UiTheme.BackgroundTop` 단색 |
| `logo_title` | 게임 로고(텍스트로 대체 가능, 있으면 브랜딩 업) | 800×300 | 낮음 |

## 4. 게임 화면 — 물병/물 (WaterSort 테마 특화)

| 이름 | 용도 | 권장 크기 | 형태 | 우선순위 |
|---|---|---|---|---|
| `bottle_outline` | 빈 병 윤곽(테두리) → `UiSkin.Bottle Background`(2026-08-31, Inspector로 바로 연결 가능 — 이미 완성된 그림으로 취급해 틴트 없이 그대로 씀) | 140×420 (슬롯 수에 따라 세로 가변 대응 필요 — 9-slice 권장) | 상단 트인 시험관형, 밝은 회색 테두리(기획서 5.3) | **최우선** — GameView 시작하려면 이것부터 필요 |
| `bottle_mask` | `bottle_outline`의 실루엣 안쪽(실제로 물이 차 있어도 되는 영역)만 불투명, 나머지(병 바깥 + 유리 테두리)는 완전 투명으로 손으로 그린 그림 → `UiSkin.Bottle Mask`(2026-09-07, Inspector로 바로 연결). Unity `Mask` 컴포넌트로 물을 이 실루엣 밖으로 못 나가게 잘라낸다 — `bottle_outline`과 반드시 같은 캔버스/정렬로 제작(가장 안전한 방법은 `bottle_outline` 원본 파일을 복사해서 그 위에 덧칠) | `bottle_outline`과 동일 캔버스 | 실루엣 내부=흰색 불투명, 그 외 전부 알파 0 | **최우선** — 없으면 물이 사각형 그대로 나옴(기능은 동작, 모양만 아쉬움) |
| `bottle_highlight` | (해결됨, 그림 불필요) 병 유리 하이라이트 — 물 세그먼트보다 위 레이어에 그리는 방식은 AI 생성 정적 이미지로 한 번 시도했다가 결과물이 별로여서 롤백했는데(2026-09-08), 2026-09-09에 그림 대신 `UiTheme.GlassHighlightSprite`가 런타임에 코드로 생성하는 세로 그라디언트로 재구현해서 붙었다(Architecture.md 참고). 이 그림 파일은 더 이상 필요 없음 — 위치/밝기는 `UiTheme`의 `GlassHighlight*` 상수로 조절 | - | - | 해결됨 |
| `water_layer` | 물 1칸(색상별 연속 구간 하나) → `UiSkin.Water Fill`(2026-08-31, Inspector로 바로 연결 가능) | 정사각형에 가까운 비율, 슬롯 크기에 맞춰 스케일 | **흰색/밝은 회색 바탕에 세로 방향 명암을 넣어서 제작**(2026-09-08 갱신 — 예전엔 "그라디언트 최소화"였는데, 단색으로 밋밋해 보인다는 피드백으로 방향 전환. 예: 왼쪽 30~40%는 밝게, 오른쪽은 어둡게 — 유리 원통에 빛이 비치는 느낌. WaterPalette 색으로 계속 틴트하는 건 그대로라, 그림에 명암을 넣어두면 색마다 자동으로 같은 명암의 하이라이트가 생김) | **최우선** |
| `water_surface_highlight` | 물 최상단 표면 하이라이트(찰랑임 느낌) | water_layer 폭과 동일, 얇게 | 반투명 곡선 | 낮음 |
| `fx_pour_stream` | 붓는 중 물줄기 연출(선택 — 없으면 병 기울임 회전만으로도 동작) | - | 파티클/스트립 | 낮음 |
| `fx_clear_sparkle` | 병 클리어 시 테두리 반짝임(기획서 3.6) | - | 파티클 또는 애니메이션 스프라이트 시트 | 중간 |

> 위 4번 항목은 전부 **테마 프리젠테이션**이라, 다른 소재로 프로젝트를 복사할 때
> 이 표만 그 소재 것으로 바꿔서 다시 채우면 된다(`docs/template/GameDesign.md`
> "{소재} 테마 특화" 표와 짝을 이룸).

## 5. 결과/클리어 화면 (아직 미착수, 화면 만들 때 다시 확인)

| 이름 | 용도 | 우선순위 |
|---|---|---|
| `banner_clear` | 라운드 클리어 축하 배너 | 낮음 |
| `bg_button_next_round` | 다음 라운드 버튼 배경 | 낮음(공용 `bg_button_rounded` 재사용 가능) |

---

## 지금 당장 GameView 착수에 필요한 최소 세트

UI 코드 진행을 막지 않으려면 아래만 먼저 있어도 충분하다(나머지는 단색
대체로 계속 개발 가능):

1. `icon_settings_gear` (타이틀 화면에 이미 자리표시자로 비어있음)
2. `icon_undo`, `icon_reset`, `icon_hint_bulb`, `icon_add_container`
3. `bottle_outline`, `water_layer`
4. `bg_button_rounded`

나머지는 화면이 실제로 필요해지는 시점(팝업, 결과 화면 등)에 다시 확인한다.
