"""One local Maestro session; loopback bridge carries touch commands, never model writes."""
import atexit, http.server, json, os, pathlib, queue, subprocess, threading, time, re, urllib.request

class TouchBridge:
    def __init__(self, out, udid, bundle):
        self.pending=queue.Queue();self.previous=None;self.proc=None;self.out=out;self.bundle=bundle;self.native_base=None
        bridge=self
        class Handler(http.server.BaseHTTPRequestHandler):
            def log_message(self,*args): pass
            def do_GET(self):
                if self.path!='/next':self.send_error(404);return
                if bridge.previous is not None:bridge.previous.set()
                command,event=bridge.pending.get(timeout=900)
                bridge.previous=event
                body=json.dumps(command).encode();self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(body)));self.end_headers();self.wfile.write(body)
        self.server=http.server.ThreadingHTTPServer(('127.0.0.1',0),Handler)
        threading.Thread(target=self.server.serve_forever,daemon=True).start()
        port=self.server.server_address[1]
        (out/'next-input.js').write_text(f'output.command = JSON.parse(http.get("http://127.0.0.1:{port}/next").body);')
        flow='''appId: BUNDLE
---
- runScript: next-input.js
- repeat:
    times: 150
    while:
      true: ${output.command.kind != 'done'}
    commands:
      - runFlow:
          when:
            true: ${output.command.kind == 'tap'}
          commands:
            - tapOn:
                point: ${output.command.point}
      - runScript: next-input.js
'''.replace('BUNDLE',bundle)
        (out/'persistent-input.yaml').write_text(flow)
        self.log=(out/'persistent-input.log').open('w')
        self.command=[os.path.expanduser('~/.maestro/bin/maestro'),'--device',udid,'test',str(out/'persistent-input.yaml')]
        atexit.register(self.close)
    def action(self, body):
        import re
        values=re.findall(r'"([^"]+)"',body)
        if body.startswith('- swipe:'):
            self.swipe(values[0],values[1]);return
        command={'kind':'tap','point':values[0]}
        event=threading.Event();self.pending.put((command,event))
        if self.proc is None:self.proc=subprocess.Popen(self.command,stdout=self.log,stderr=subprocess.STDOUT)
        deadline=time.time()+360
        while not event.wait(1):
            if self.proc.poll() is not None:raise AssertionError('Persistent native driver exited: '+(self.out/'persistent-input.log').read_text()[-3000:])
            if time.time()>deadline:raise TimeoutError('Native input exceeded 360 seconds; inspect persistent-input.log')
    def native_request(self, base, endpoint, payload):
        req=urllib.request.Request(base+'/'+endpoint,data=json.dumps(payload).encode(),headers={'Content-Type':'application/json'},method='POST')
        with urllib.request.urlopen(req,timeout=60) as response:return response.read()
    def swipe(self, start, end):
        # Maestro 2.12.1 parses swipe coordinates before JS evaluation. Use its
        # already-running XCUITest driver's native swipe API, not a new driver.
        if self.native_base is None:
            logs=sorted(pathlib.Path.home().glob('.maestro/tests/*/maestro.log'),key=lambda p:p.stat().st_mtime,reverse=True)
            assert logs,'Native driver log missing'
            ports=list(dict.fromkeys(re.findall(r'127\.0\.0\.1:(\d+)',logs[0].read_text())))
            for port in reversed(ports):
                base='http://127.0.0.1:'+port
                try:
                    info=json.loads(self.native_request(base,'deviceInfo',{}))
                    if info.get('widthPoints') and info.get('heightPoints'):
                        self.native_base=base;self.info=info;break
                except Exception:pass
            assert self.native_base,'Running XCUITest driver endpoint not found'
        def xy(point):
            x,y=[float(v.strip().replace('%','')) for v in point.split(',')]
            return x*self.info['widthPoints']/100,y*self.info['heightPoints']/100
        sx,sy=xy(start);ex,ey=xy(end)
        payload={'appId':self.bundle,'startX':sx,'startY':sy,'endX':ex,'endY':ey,'duration':3.0}
        result=self.native_request(self.native_base,'swipe',payload)
        with (self.out/'native-swipes.jsonl').open('a') as f:f.write(json.dumps({'touch':payload,'response':result.decode(errors='replace')[:500]})+'\n')
    def close(self):
        if self.proc is None:return
        self.pending.put(({'kind':'done'},threading.Event()))
        try:self.proc.wait(timeout=30)
        except subprocess.TimeoutExpired:self.proc.terminate()
        self.server.shutdown();self.log.close();self.proc=None
