# PixelLab MCP JSON-RPC helper (Streamable HTTP)
import json, sys, urllib.request, os

URL = "https://api.pixellab.ai/mcp"
TOKEN = "5c6adfcc-8231-47c5-9ab8-76dd44e46ac7"
SESSION_FILE = os.path.join(os.path.dirname(__file__), "mcp_session_id.txt")

def _post(payload, session_id=None, timeout=300):
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
        "Authorization": f"Bearer {TOKEN}",
    }
    if session_id:
        headers["Mcp-Session-Id"] = session_id
    req = urllib.request.Request(URL, data=json.dumps(payload).encode(), headers=headers, method="POST")
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        sid = resp.headers.get("Mcp-Session-Id") or resp.headers.get("mcp-session-id")
        ctype = resp.headers.get("Content-Type", "")
        body = resp.read().decode("utf-8", "replace")
    if "text/event-stream" in ctype:
        # parse SSE: take last data: line that is valid JSON
        result = None
        for line in body.splitlines():
            if line.startswith("data:"):
                chunk = line[5:].strip()
                if chunk:
                    try:
                        result = json.loads(chunk)
                    except json.JSONDecodeError:
                        pass
        return result, sid
    if body.strip():
        return json.loads(body), sid
    return None, sid

def init_session():
    payload = {"jsonrpc": "2.0", "id": 0, "method": "initialize", "params": {
        "protocolVersion": "2025-03-26",
        "capabilities": {},
        "clientInfo": {"name": "hangeul-adventure-cli", "version": "1.0"}}}
    result, sid = _post(payload)
    if sid:
        with open(SESSION_FILE, "w") as f:
            f.write(sid)
    # send initialized notification
    _post({"jsonrpc": "2.0", "method": "notifications/initialized"}, session_id=sid)
    return result, sid

def get_sid():
    if os.path.exists(SESSION_FILE):
        return open(SESSION_FILE).read().strip()
    return None

_id_counter = [100]
def rpc(method, params=None):
    _id_counter[0] += 1
    payload = {"jsonrpc": "2.0", "id": _id_counter[0], "method": method}
    if params is not None:
        payload["params"] = params
    result, _ = _post(payload, session_id=get_sid())
    return result

def call_tool(name, arguments=None):
    return rpc("tools/call", {"name": name, "arguments": arguments or {}})

def tool_text(res):
    """Extract text content from a tools/call result."""
    try:
        parts = res["result"]["content"]
        return "\n".join(p.get("text", "") for p in parts if p.get("type") == "text")
    except (KeyError, TypeError):
        return json.dumps(res, ensure_ascii=False)[:2000]

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "init"
    if cmd == "init":
        result, sid = init_session()
        print("session:", sid)
        print(json.dumps(result, ensure_ascii=False)[:800])
    elif cmd == "tools":
        res = rpc("tools/list")
        tools = res["result"]["tools"]
        for t in tools:
            print(t["name"], "-", t.get("description", "")[:100].replace("\n", " "))
    elif cmd == "schema":
        res = rpc("tools/list")
        for t in res["result"]["tools"]:
            if t["name"] == sys.argv[2]:
                print(json.dumps(t, ensure_ascii=False, indent=1))
    elif cmd == "call":
        name = sys.argv[2]
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        res = call_tool(name, args)
        print(tool_text(res))
