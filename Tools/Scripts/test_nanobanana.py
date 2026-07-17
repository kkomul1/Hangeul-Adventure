# -*- coding: utf-8 -*-
# 나노바나나(Gemini 이미지) 키 검증 + 참조 기반 생성 테스트
import base64, json, urllib.request, os, sys

KEY = open(os.path.expanduser("~/.claude/secrets/gemini-api-key.txt")).read().strip()
BASE = "https://generativelanguage.googleapis.com/v1beta"

def call(url, payload=None):
    req = urllib.request.Request(url,
        data=json.dumps(payload).encode() if payload else None,
        headers={"Content-Type": "application/json", "x-goog-api-key": KEY})
    return json.loads(urllib.request.urlopen(req, timeout=120).read())

# 1) 키 유효성: 모델 목록
try:
    models = call(f"{BASE}/models?pageSize=50")
    names = [m["name"] for m in models.get("models", [])]
    img_models = [n for n in names if "image" in n]
    print("키 유효 ✓ / 이미지 모델:", img_models[:5])
except Exception as e:
    print("키 검증 실패:", e); sys.exit(1)

# 2) 참조 기반 생성 테스트
art = base64.b64encode(open(r"C:/Users/minjae/UnityProjects/HangeulAdventure/Assets/Resources/Art/title_art.png", "rb").read()).decode()
model = "models/gemini-2.5-flash-image" if "models/gemini-2.5-flash-image" in names else (img_models[0] if img_models else None)
if not model:
    print("이미지 생성 모델 없음"); sys.exit(1)
print("사용 모델:", model)

payload = {
    "contents": [{"parts": [
        {"inline_data": {"mime_type": "image/png", "data": art}},
        {"text": "Using the exact same painting style, palette and mood as this reference illustration, create a new 16:9 scene: a modern Korean teenage boy in a hoodie standing bewildered in the middle of a Joseon-era village street at dusk, villagers with lanterns looking at him curiously. Same brushwork, same warm dusk lighting, same level of detail."}
    ]}],
    "generationConfig": {"responseModalities": ["IMAGE"]}
}
res = call(f"{BASE}/{model}:generateContent", payload)
for part in res.get("candidates", [{}])[0].get("content", {}).get("parts", []):
    if "inlineData" in part:
        out = r"C:/Users/minjae/UnityProjects/HangeulAdventure/ArtDrop/nanobanana_test_cut.png"
        open(out, "wb").write(base64.b64decode(part["inlineData"]["data"]))
        print("생성 성공 →", out, os.path.getsize(out) // 1024, "KB")
        break
else:
    print("이미지 파트 없음. 응답:", json.dumps(res)[:400])
