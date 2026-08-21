# Scriptable Object

밸런스 수치(난이도 곡선, 색상 팔레트, 라운드 생성 파라미터 범위 등)를 코드가 아니라
Inspector로 튜닝하기 위한 ScriptableObject 에셋을 담는 폴더.

예정 후보 (기획서 기준, 실제 확정 순서대로 추가):
- 색상 팔레트 정의 (색상 ID ↔ HEX, 테마별로 교체되는 프리젠테이션 데이터)
- 난이도 커브 / DifficultyScore 가중치
- 라운드 구간별 파라미터 테이블 (`containerCount`, `slotCount`, `colorCount` 등 범위)
