# -*- coding: utf-8 -*-
import base64, json, urllib.request, os
os.chdir(r"C:/Users/minjae/UnityProjects/HangeulAdventure")
LC = r"ArtDrop/Generated/lookcheck_b2"
def img64(p): return "data:image/png;base64," + base64.b64encode(open(p, "rb").read()).decode()
IMG = "width:100%;max-width:860px;border:1px solid #e2ded2;border-radius:8px;margin:6px 0;image-rendering:pixelated"
third = "width:32%;border:1px solid #e2ded2;border-radius:8px;margin:2px;image-rendering:pixelated;vertical-align:top"
html = f"""<div style="font-family:'Segoe UI','Malgun Gothic',sans-serif;color:#2c2c2a;font-size:14px;line-height:1.7">
<p style="margin:0 0 6px"><b>B안 룩 샘플 개정판</b> — 반려 코멘트 2건 대응:</p>
<ul style="margin:0 0 8px;padding-left:20px">
<li>"한 줄 조각·떨어질 것 같은 틈" → 지면을 <b>연속 지형 덩어리 2종(평지·언덕)의 겹침 배치</b>로 교체, 간극 0. 부유 발판은 공중에만</li>
<li>"석등과 발판 느낌 불일치" → 석등·장승을 <b>지형 이미지를 참조로 인페인팅 재생성</b>해 팔레트를 지형에서 직접 상속. 전 에셋이 하나의 스타일 앵커(새벽 저채도·온광 포인트) 아래 통일</li>
</ul>
<img src="{img64(f'{LC}/scene_composite_1280.png')}" style="{IMG}">
<p style="margin:8px 0 4px;color:#888;font-size:13px">재생성 프롭: 석등 · 장승 · 부유 발판</p>
<img src="{img64(f'{LC}/prop_seokdeung_b2_48x78.png')}" style="{third}"><img src="{img64(f'{LC}/prop_jangseung_b2_64x130.png')}" style="{third}"><img src="{img64(f'{LC}/platform_float_large_160x64.png')}" style="{third}">
<p style="margin:8px 0 0">확정 시 pixellab.ai에서 <b>Tier 1($12/월)</b> 결제 후 본 생성 시작 (확정된 스타일 앵커 문구를 전 에셋에 적용). 남은 미세 격차 — 부유 발판의 윗면 시점, 지형 반복 주기 — 는 본 생성에서 보정 계획이 잡혀 있습니다. 합성은 실기 1:1 스케일.</p></div>"""
payload = {"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "propose_change", "arguments": {
    "title": "B안 룩 샘플 개정판 — 통일감 반영 (결제 판단)", "html": html,
    "note": "결제 완료를 코멘트나 채팅으로 알려주시면 본 생성 시작", "project": "HangeulAdventure", "from_label": "M4 배치",
    "options": ["확정 — 결제하고 오겠음", "수정 필요 (코멘트)", "반려"]}}}
req = urllib.request.Request("http://127.0.0.1:7777/mcp",
    data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
    headers={"Content-Type": "application/json; charset=utf-8", "Accept": "application/json, text/event-stream"})
body = json.loads(urllib.request.urlopen(req, timeout=20).read())
print(json.loads(body["result"]["content"][0]["text"])["id"])
