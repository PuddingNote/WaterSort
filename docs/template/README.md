# 새 소재로 프로젝트 복사할 때 — 여기서 시작

이 폴더는 WaterSort 저장소를 통째로 복사해 다른 소재(너트-볼트, 레고 블록 등)의
색깔 정렬 게임을 새로 만들 때 쓰는 자리표시자 문서다. Core/Solver 코드는 이미
`ColorSort.*`로 소재 무관하게 짜여 있어 손댈 게 없고, **여기 두 파일만 실제
`docs/GameDesign.md` / `docs/Architecture.md`로 교체하면서 `{...}` 자리표시자를
채우면 된다.**

## 순서

1. 저장소를 통째로 복사한다(폴더명 포함, `WaterSort` → 새 게임 이름).
2. 이 폴더(`docs/template/`)의 `GameDesign.md`, `Architecture.md`를
   `docs/GameDesign.md`, `docs/Architecture.md`에 덮어쓰고, 이 `template/`
   폴더는 지운다(더 복사할 일 없으면).
3. 두 파일 안의 `{게임 이름}`, `{소재}`, `{유닛 명칭}` 등 자리표시자를 채운다.
4. Unity 쪽 설정(코드 아님, 그때그때 확인):
   - `ProjectSettings` → Product Name / Company Name / Application(Bundle)
     Identifier를 새 게임에 맞게 변경(패키지 정체성 — 두 게임이 같은 식별자로
     스토어에 올라갈 수 없다).
   - 앱 아이콘, 스플래시 등 브랜딩 에셋 교체.
   - `WaterSort/.gitignore`의 `*.keystore`/`*.jks` 규칙은 그대로 유지(새 게임도
     서명 키는 절대 커밋 금지).
5. git 이력: 이 저장소의 커밋 로그엔 "WaterSort"라는 이름이 그대로 남아있다.
   새로 `git init`해서 이력 없이 시작할지, 이어갈지는 사용자 판단(운영 판단
   영역 — 새_캐주얼_게임_시작_키트.md/재사용 노트 9장 원칙대로 Claude가 먼저
   나서서 정하지 않는다).
6. 개인정보처리방침/강제 업데이트용 허브 저장소(`{계정}.github.io`)에 새 게임
   이름의 폴더를 만드는 건 광고 SDK를 넣기로 정하는 시점에 한다
   (`개인정보처리방침_재사용_가이드.md` 참고 — 지금 당장은 불필요).

## 그대로 두면 되는 것 (손댈 필요 없음)

- `Assets/Scripts/Core/`, `Assets/Scripts/Solver/`, `Assets/Tests/EditMode/`의
  모든 코드 — asmdef 프리픽스(`ColorSort.*`)부터 로직까지 소재 무관.
- `Assets/Editor/`, `Assets/Scripts/Managers/`, `Assets/Scripts/UI/`의 asmdef
  골격(아직 내용 없음 — 새 소재의 UI/연출은 여기서부터 새로 채워나가면 됨).
- `WaterSort/.gitignore`, 저장소 루트 구조(`docs/` 밖에 Unity 프로젝트 폴더).

## 다음에 실제로 할 일

여기까지 끝났으면, 그 다음은 원래 WaterSort에서 했던 순서와 동일하게 새
소재의 UI(코드 기반 UiFactory/UiTheme 패턴)부터 시작하면 된다 — 그 시점에
`docs/GameDesign.md`의 "확정이 필요한 정책" 목록을 하나씩 사용자에게 확인해
나간다.
