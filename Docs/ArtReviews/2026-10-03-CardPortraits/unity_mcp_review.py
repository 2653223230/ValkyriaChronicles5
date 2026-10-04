"""Local MCP transport for this review when the desktop's cached connection is unavailable."""
import json
import sys
import urllib.request

sys.stdout.reconfigure(encoding="utf-8")
headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
sequence = 0

def rpc(method, params, notification=False):
    global sequence
    sequence += 1
    body = {"jsonrpc": "2.0", "method": method, "params": params}
    if not notification:
        body["id"] = sequence
    request = urllib.request.Request("http://127.0.0.1:8080/mcp", json.dumps(body).encode(), headers)
    with urllib.request.urlopen(request, timeout=45) as response:
        if response.headers.get("mcp-session-id"):
            headers["Mcp-Session-Id"] = response.headers["mcp-session-id"]
        raw = response.read().decode()
    if not raw:
        return None
    messages = [json.loads(raw)] if raw.lstrip().startswith("{") else [
        json.loads(line[5:].strip()) for line in raw.splitlines() if line.startswith("data:")
    ]
    message = next((item for item in messages if item.get("id") == sequence), {})
    if "error" in message:
        raise RuntimeError(message["error"])
    return message.get("result")

request = json.load(sys.stdin)
rpc("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "vc5-review", "version": "1"}})
rpc("notifications/initialized", {}, True)
if request.get("instance"):
    pinned = rpc("tools/call", {"name": "set_active_instance", "arguments": {"instance": request.pop("instance")}})
    if pinned.get("isError"):
        raise RuntimeError(pinned)
result = rpc(request["method"], request.get("params", {}))
if request.get("names") and "tools" in result:
    result = [t for t in result["tools"] if t["name"] in request["names"]]
print(json.dumps(result, ensure_ascii=False))
