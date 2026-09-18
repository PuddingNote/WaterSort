# 강제 업데이트 시스템 (Force Update Gate)

> 구조·원칙은 `캐주얼_게임_재사용_시스템_모음.md` 1장을 그대로 가져왔다(Dice
> Battle에서 실측/운영해 본 설계). 이 문서는 그중 **이 프로젝트(WaterSort)에
> 실제로 적용한 구체 값**과 운영 시 지켜야 할 순서만 담는다 — "왜 이런 구조인지"는
> 위 재사용 문서를 참고.

## 구조

```
GameBootstrap.Boot()
  └ VersionCheckService.CheckAsync()   부팅 직후 원격 JSON 조회 → 버전 비교(fail-open)
       └ UpdateRequiredView.Show()      필요할 때만 뜨는 닫을 수 없는 차단 창 [QUIT] [UPDATE]

AppVersion (Core)                       "0.10.0" > "0.9.9"를 보장하는 순수 버전 문자열 비교
```

- `AppVersion.IsOlderThan` — [AppVersion.cs](../WaterSort/Assets/Scripts/Core/AppVersion.cs).
  문자열을 그대로 비교하지 않고 `.`으로 나눠 자리별로 정수 비교한다(그렇지 않으면
  "0.9.9" > "0.10.0"으로 잘못 판정됨).
- `VersionCheckService.CheckAsync` — [VersionCheckService.cs](../WaterSort/Assets/Scripts/Managers/VersionCheckService.cs).
  현재 버전은 `Application.version`(= Player Settings의 bundleVersion, 즉 실제 배포
  버전)을 그대로 쓴다.
- `UpdateRequiredView` — [UpdateRequiredView.cs](../WaterSort/Assets/Scripts/UI/UpdateRequiredView.cs).
  `ConfirmDialog`와 같은 규격(딤 배경 + 다이얼로그 패널)이지만, 버튼을 눌러도
  절대 안 닫힌다. `IsActive` 정적 플래그로 `TitleScreen`/`GameView`의 뒤로가기
  (Escape) 처리를 완전히 무시시켜서, 뒤로가기로 그 화면 자신의 다이얼로그가 차단
  창 뒤에서 열리는 경합을 막는다.

## 원격 파일

```
https://puddingnote.github.io/watersort/version.json
```

`개인정보처리방침_재사용_가이드.md` 2장의 허브 저장소 방식 그대로 —
`privacy-policy.html`과 같은 자리(`PuddingNote.github.io/watersort/`, 폴더명은
소문자로 통일하기로 확정, 2026-09-18)에 둔다.

**⚠️ 2026-09-18 기준 이 저장소/폴더/파일이 아직 실제로 만들어지지 않았다.**
지금은 이 URL로 요청해도 그냥 실패(fail-open)해서 아무도 이 창을 보지 않는
상태다 — 안전하지만 강제 업데이트 기능 자체가 아직 켜져 있지 않다는 뜻이기도
하다. 실제로 동작하게 하려면:
1. `PuddingNote.github.io` 저장소를 만들고(또는 이미 있다면) Pages를 켠다.
2. 그 안에 `watersort/version.json`을 만들어 아래 형식으로 채운다(폴더명 소문자).
3. `docs/privacy-policy.html`도 같은 김에 이 허브 저장소로 옮기는 걸 고려한다
   (아직 게임 저장소 안에 있음 — `Architecture.md` "정책/약관" 절 참고).

`version.json` 형식:
```json
{
  "minVersion": "0.0.0",
  "storeUrl": "https://play.google.com/store/apps/details?id=com.onepixel.watersort",
  "message": "A new version is available.\nPlease update to keep playing."
}
```
- `storeUrl`/`message`를 비워도(파일에서 빼도) `VersionCheckService`가 각각
  `Application.identifier` 기반 스토어 URL과 기본 영문 메시지로 채운다 — 실제로는
  운영자가 원하는 문구로 바꿔 넣는 걸 권장(메시지는 앱이 아니라 이 JSON이 소스이므로
  언어 제한 없음).

## fail-open (반드시 지킬 것)

**확인에 실패하면 무조건 통과시킨다** — 오프라인, 타임아웃(5초), 404, JSON 문법
오류, `minVersion` 파싱 실패 전부 "그냥 통과". `VersionCheckService.CheckAsync`는
예외를 절대 밖으로 던지지 않는다. 오타 하나로 전체 유저가 게임을 못 켜는 사고가,
일부가 구버전을 쓰는 것보다 훨씬 나쁘다.

## minVersion을 올릴지 판단하는 기준표

| 상황 | 올린다 |
|---|:---:|
| 핵심 규칙·밸런스 변경 | ✅ |
| 저장 데이터 형식 변경 | ✅ |
| 크래시·치명적 버그 수정 | ✅ |
| 필수 SDK 도입/교체(광고 등) | ✅ |
| UI 다듬기, 문구 수정, 연출 개선 | ❌ |
| 새 기능 추가(구버전도 정상 동작) | ❌ (게임의 핵심 축이라 전원이 써야 하면 예외 — 이유를 기록) |

## 운영 순서 (틀리면 위험한 부분)

```
1. 새 버전 빌드 → Play 콘솔 업로드
2. Play 콘솔에서 "배포 완료" 상태 확인          ← 반드시 여기까지 기다린다
3. version.json의 minVersion을 올린다
4. 푸시(허브 저장소)
```
**minVersion을 먼저 올리면**: 사용자는 게임도 못 하고, 스토어에 가도 아직 새
버전이 없어 받을 것도 없는 상태가 된다. **단계적 출시 중에는 minVersion을 절대
올리지 않는다.**

## 사고 복구

전원이 차단됐다면 → `minVersion`을 `"0.0.0"`으로 되돌리고 푸시만 하면 된다.
앱 재빌드/재심사 불필요, 캐시 반영까지 1~5분.
