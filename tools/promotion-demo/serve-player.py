from http.server import SimpleHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit
from paths import KIT, OUTPUT
import re
root = OUTPUT
if not (root / 'excel-to-tasks.mp4').is_file():
    raise SystemExit('Encode the MP4 before starting the player.')
for filename in ('watch.html', 'transcript.md'):
    (root / filename).write_bytes((KIT / 'video' / filename).read_bytes())
class Handler(SimpleHTTPRequestHandler):
    def __init__(self,*args,**kwargs):super().__init__(*args,directory=str(root),**kwargs)
    def translate_path(self, request_path):
        route = urlsplit(request_path).path
        if route == '/recording-result.json':
            return str(OUTPUT.parent / 'saved-result.json')
        if route in ('/README.md', '/work-notes.xlsx', '/rehearsal-2026-09-27.md'):
            return str(KIT / route.lstrip('/'))
        return super().translate_path(request_path)

    def send_head(self):
        self.remaining=None
        path=Path(self.translate_path(self.path))
        request=self.headers.get('Range')
        if not request or not path.is_file():return super().send_head()
        match=re.fullmatch(r'bytes=(\d+)-(\d*)',request)
        if not match:return super().send_head()
        size=path.stat().st_size
        start=int(match[1]);end=min(int(match[2]) if match[2] else size-1,size-1)
        if start>end:
            self.send_response(416);self.send_header('Content-Range',f'bytes */{size}');self.end_headers();return None
        f=path.open('rb');f.seek(start);self.remaining=end-start+1
        self.send_response(206)
        self.send_header('Content-Type',self.guess_type(str(path)))
        self.send_header('Content-Length',str(self.remaining))
        self.send_header('Content-Range',f'bytes {start}-{end}/{size}')
        self.send_header('Accept-Ranges','bytes');self.end_headers()
        return f
    def copyfile(self,source,outputfile):
        if self.remaining is None:return super().copyfile(source,outputfile)
        remaining=self.remaining
        while remaining:
            data=source.read(min(65536,remaining))
            if not data:break
            outputfile.write(data);remaining-=len(data)
with ThreadingHTTPServer(('127.0.0.1', 0), Handler) as server:
    print(f'http://127.0.0.1:{server.server_port}/watch.html', flush=True)
    server.serve_forever()
