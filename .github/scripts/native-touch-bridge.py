"""One local Maestro session; loopback bridge carries touch commands, never model writes."""
import atexit, http.server, json, os, pathlib, queue, subprocess, threading, time

class TouchBridge:
    def __init__(self, out, udid, bundle):
        self.pending=queue.Queue();self.previous=None;self.proc=None;self.out=out
        bridge=self
        class Handler(http.server.BaseHTTPRequestHandler):
            def log_message(self,*args): pass
            def do_GET(self):
                if self.path!='/next':self.send_error(404);return
                if bridge.previous is not None:bridge.previous.set()
                command,event=bridge.pending.get(timeout=180)
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
      - runFlow:
          when:
            true: ${output.command.kind == 'swipe'}
          commands:
            - swipe:
                start: ${output.command.start}
                end: ${output.command.end}
                duration: 1200
      - runScript: next-input.js
'''.replace('BUNDLE',bundle)
        (out/'persistent-input.yaml').write_text(flow)
        self.log=(out/'persistent-input.log').open('w')
        self.command=[os.path.expanduser('~/.maestro/bin/maestro'),'--device',udid,'test',str(out/'persistent-input.yaml')]
        atexit.register(self.close)
    def action(self, body):
        import re
        values=re.findall(r'"([^"]+)"',body)
        command={'kind':'tap','point':values[0]} if body.startswith('- tapOn:') else {'kind':'swipe','start':values[0],'end':values[1]}
        event=threading.Event();self.pending.put((command,event))
        if self.proc is None:self.proc=subprocess.Popen(self.command,stdout=self.log,stderr=subprocess.STDOUT)
        deadline=time.time()+360
        while not event.wait(1):
            if self.proc.poll() is not None:raise AssertionError('Persistent native driver exited: '+(self.out/'persistent-input.log').read_text()[-3000:])
            if time.time()>deadline:raise TimeoutError('Native input exceeded 360 seconds; inspect persistent-input.log')
    def close(self):
        if self.proc is None:return
        self.pending.put(({'kind':'done'},threading.Event()))
        try:self.proc.wait(timeout=30)
        except subprocess.TimeoutExpired:self.proc.terminate()
        self.server.shutdown();self.log.close();self.proc=None
