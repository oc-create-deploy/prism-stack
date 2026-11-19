"""Drive normal native touch input; QA snapshots are read-only observations."""
import json, os, pathlib, subprocess, time
out = pathlib.Path('screenshots'); out.mkdir(exist_ok=True)
udid = os.environ['UDID']; bundle = os.environ['APP_BUNDLE_ID']
container = pathlib.Path(subprocess.check_output(['xcrun','simctl','get_app_container',udid,bundle,'data'],text=True).strip())
seq = 0

def state():
    deadline = time.time()+20
    while time.time()<deadline:
        for p in container.rglob('qa-state.json'):
            try:
                s=json.loads(p.read_text())
                assert not s['errors'],s['errors']
                return s
            except (json.JSONDecodeError,FileNotFoundError): pass
        time.sleep(.5)
    raise AssertionError('Read-only native snapshot missing')

def capture(label):
    (out/(label+'.json')).write_text(json.dumps(state(),indent=2))
    subprocess.run(['xcrun','simctl','io',udid,'screenshot',str(out/(label+'.png'))],check=True)

def point(item):
    assert 0<item['x']<1 and 0<item['y']<1,item
    return f"{item['x']*100:.3f}%, {item['y']*100:.3f}%"

def action(body):
    global seq
    seq+=1
    p=out/f'input-{seq:03}.yaml'
    p.write_text(f'appId: {bundle}\n---\n'+body+'\n')
    subprocess.run([os.path.expanduser('~/.maestro/bin/maestro'),'--device',udid,'test',str(p)],check=True,timeout=90)
    time.sleep(1)

def tap(text):
    choices=[i for i in state()['items'] if i['kind']=='button' and i['interactable'] and text.casefold() in (i.get('text','')+' '+i['name']).casefold()]
    assert choices,('Button missing',text)
    action('- tapOn:\n    point: "'+point(choices[0])+'"')

capture('01-menu')
tap('HOW TO PLAY');capture('02-help');tap('PLAY');capture('03-board')
placements=0;clears=0
for turn in range(60):
    s=state();cells={tuple(map(int,i['name'].split(','))):i for i in s['items'] if i['kind']=='cell'}
    assert len(cells)==64,('Grid not observed',len(cells))
    occupied={rc for rc,i in cells.items() if i['group']==1}
    choices=[]
    for piece in [i for i in s['items'] if i['kind']=='piece']:
        shape={(int(v['x']),int(v['y'])) for v in piece['shape']}
        assert shape,'Empty piece'
        for row,col in cells:
            placed={(row-dy,col+dx) for dx,dy in shape}
            if not placed.issubset(cells) or placed & occupied: continue
            filled=occupied|placed
            rows={r for r in range(8) if all((r,c) in filled for c in range(8))}
            cols={c for c in range(8) if all((r,c) in filled for r in range(8))}
            expected={rc for rc in filled if rc[0] not in rows and rc[1] not in cols}
            choices.append((len(rows)+len(cols),sum(r*8+c for r,c in placed),piece,cells[row,col],expected))
    if not choices:
        capture('06-no-fit');break
    # Favor real clears and pack toward one corner, without modifying the model.
    count,_,piece,target,expected=max(choices,key=lambda c:(c[0],c[1]))
    action('- swipe:\n    start: "'+point(piece)+'"\n    end: "'+point(target)+'"\n    duration: 1200')
    actual={tuple(map(int,i['name'].split(','))) for i in state()['items'] if i['kind']=='cell' and i['group']==1}
    assert actual==expected,('Touch placement/clear mismatch',turn,sorted(expected),sorted(actual))
    placements+=1;clears+=count
    if turn in (0,5,11) or count:capture(f'04-placement-{turn:02}')
    if placements>=12 and clears:break
assert placements>=3,('Not enough native placements',placements)
assert clears>=1,('No native line clear proved',placements)
capture('05-played')
values={v['key']:v['value'] for v in state()['values']}
score=int(values.get('GameController.points','0'))
assert score>0,('Clears did not award points',values)
subprocess.run(['xcrun','simctl','terminate',udid,bundle],check=True)
subprocess.run(['xcrun','simctl','launch',udid,bundle,'--playabilityQA'],check=True)
time.sleep(3)
menu=state()
assert menu['scene']=='Menu',('Cold launch did not return to menu',menu['scene'])
texts=[i.get('text','') for i in menu['items'] if i['kind']=='text']
best=[int(t.rsplit(' ',1)[-1]) for t in texts if t.startswith('BEST SCORE  ')]
assert best and max(best)>=score,('High score not persisted across native relaunch',score,texts)
capture('07-highscore-relaunch')
tap('PLAY');time.sleep(1)
assert int(next(v['value'] for v in state()['values'] if v['key']=='GameController.points'))==0,'New game score did not reset'
capture('08-new-round')
(out/'result.json').write_text(json.dumps({'placements':placements,'clearedLines':clears,'coreTouchPass':True,'savedHighScorePass':True,'newRoundPass':True,'lossRetryAndSimultaneousClear':'pending further review'},indent=2))
