# Tools/Scripts — 세션 간 재사용 스크립트 (2026-07-17 인수인계)

- `mcp.py` — PixelLab MCP(HTTP JSON-RPC) 직접 호출 헬퍼. 키는 ~/.claude.json의 pixellab Bearer에서 읽음
- `gen_b2_batch1.py` / `poll_b2.py` / `poll_download.py` — PixelLab 생성 요청·폴링·다운로드 패턴 (본 생성 시 이 패턴 재사용)
- `compose_b2.py` — 룩 검증 씬 합성 (PIL, 정수 배율 원칙)
- `key_bg.py` — PixelLab 320급 캔버스의 베이크된 배경 제거 (코너 플러드필 키잉) — 본 생성 지형에 필수 후처리
- `test_nanobanana.py` — Gemini 이미지(나노바나나) 키 검증+참조 생성 테스트. 키: ~/.claude/secrets/gemini-api-key.txt (현재 결제 미연결로 429)
- `board_post_example.py` — 승인 보드(127.0.0.1:7777/mcp)에 이미지 포함 대형 목업을 JSON-RPC로 직접 게시하는 템플릿 (컨텍스트 절약 패턴)

스타일 앵커 문구(본 생성 전 프롬프트 접두 필수)는 `Docs/아트 디렉션 v2` 문서와 메모리 m6-handover 참조.
