"""Serve the Unity build locally; no third-party packages required."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import sys
import webbrowser

root = Path(__file__).resolve().parent / "Builds" / "WebGL"
if not (root / "index.html").exists():
    raise SystemExit("WebGL build missing: " + str(root))
SimpleHTTPRequestHandler.extensions_map[".wasm"] = "application/wasm"
url = "http://127.0.0.1:8765/"
try:
    server = ThreadingHTTPServer(("127.0.0.1", 8765), partial(SimpleHTTPRequestHandler, directory=str(root)))
except OSError as error:
    raise SystemExit("Cannot start local server on port 8765: " + str(error))
print("Calculator: " + url, flush=True)
print("Keep this window open. Ctrl+C stops the server.", flush=True)
if "--no-browser" not in sys.argv:
    webbrowser.open(url)
try:
    server.serve_forever()
except KeyboardInterrupt:
    pass
finally:
    server.server_close()
