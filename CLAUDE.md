# HangeulAdventure

한글 자모를 이동/회전/합성/분해해 글자와 단어를 만드는 그리드 퍼즐 어드벤처. Steam 출시 목표. Unity 6 (6000.5.1f1), URP, 2D.

## 필독 문서
- **`Docs/게임기획초안.md`** — 게임 기획의 기준 문서. 게임플레이·기획 관련 작업 전에 반드시 읽을 것. 핵심: 합성 규칙은 실제 한글 조합 원리(모음 방향성 포함), MVP 범위는 20장 참조.

## 개발 방향
- MVP 최우선: 2x2~3x3 보드, 기본 자모, 이동/합성/분해, 이동력, 스테이지 30개. 검증 질문은 "ㄱ과 ㅏ를 움직여 가를 만드는 게 재미있는가".
- 퍼즐 규칙 엔진(합성/분해 판정, 보드 상태)은 MonoBehaviour와 분리된 순수 C#으로 작성 — EditMode 테스트와 솔버가 재사용해야 함.
- 스테이지는 코드가 아닌 데이터(JSON/ScriptableObject)로 정의.

## 서브에이전트 (.claude/agents/)
- `game-designer` — 기획, 밸런싱, 진행 구조
- `puzzle-level-designer` — 스테이지 제작 + 솔버 기반 풀이 가능성 검증
- `asset-designer` — 아트 디렉션, 에셋 스펙 (옛한글 폰트 이슈 담당)
- `unity-code-reviewer` — Unity C# 리뷰 (코드 작성 후 사용)
- `qa-playtester` — 규칙 엔진 테스트, 엣지 케이스

## Unity MCP
- `.mcp.json`에 unityMCP 등록됨 (MCP for Unity v10). **Unity 에디터가 이 프로젝트를 열고 있어야 도구가 작동함** — 연결 실패 시 에디터 실행 여부부터 확인.
- 에디터 쪽 창: `Window → MCP for Unity → Toggle MCP Window`

## 지침
- 답변은 한국어로 작성한다.
