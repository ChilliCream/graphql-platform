"""Bounded baseline/fidelity runner. Must be invoked under the exclusive shared lock."""
import argparse, concurrent.futures, contextlib, datetime as dt, hashlib, json, os, signal, socket, subprocess, threading, time, urllib.request
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=Path('/workspaces/platform-1')
BASE=ROOT/'perf/fusion-apollo-baseline-20261017b'
FROZEN=BASE/'frozen'
K6=ROOT/'perf/fusion-apollo-baseline-20261017b-tools/k6-v2.3.0-linux-arm64/k6'
TOOLS=ROOT/'perf/fusion-apollo-20261017-43a8dca2/environment-history/tools'
ENDPOINT='http://127.0.0.1:5220/graphql'
TICKS=os.sysconf('SC_CLK_TCK')
PAGE=os.sysconf('SC_PAGE_SIZE')
def stamp(): return dt.datetime.now(dt.timezone.utc).isoformat()
def dump(path,value): path.write_text(json.dumps(value,indent=2)+'\n')
def post(port,payload,headers=None):
    req=urllib.request.Request(f'http://127.0.0.1:{port}/graphql',data=json.dumps(payload).encode(),headers={'Content-Type':'application/json',**(headers or {})})
    with urllib.request.urlopen(req,timeout=30) as r: return json.load(r)
def proc(pid):
    fields=Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split()
    return {'pid':pid,'start_ticks':int(fields[19]),'cpu_seconds':(int(fields[11])+int(fields[12]))/TICKS,'rss_bytes':int(fields[21])*PAGE,'threads':int(fields[17])}
class Session:
    def __init__(self,out,delay,capture=False):
        self.out=out;out.mkdir(parents=True);self.delay=delay;self.capture=capture;self.owned=[];self.stop=threading.Event();self.live={};self.lock=threading.Lock()
    def launch(self,name,args,cwd,env,affinity):
        log=open(self.out/(name+'.log'),'w')
        launch_env={**os.environ,**env}
        p=subprocess.Popen(['taskset','-c',affinity,*map(str,args)],cwd=cwd,env=launch_env,stdout=log,stderr=subprocess.STDOUT)
        identity=proc(p.pid);identity.update(name=name,argv=list(map(str,args)),utc=stamp(),cwd=str(cwd),affinity=affinity,environment={k:v for k,v in launch_env.items() if k.startswith(('DOTNET_', 'COMPlus_', 'ASPNETCORE_', 'BENCH_', 'BENCHMARK_', 'Logging__'))})
        with self.lock: self.owned.append((p,identity,log));self.live[name]=p.pid
        with open(self.out/'process-identities.jsonl','a') as f:f.write(json.dumps(identity)+'\n')
        return p
    def monitor(self):
        with open(self.out/'process-samples.jsonl','w',buffering=1) as f:
            while not self.stop.is_set():
                row={'time':time.time(),'monotonic':time.monotonic(),'processes':{}}
                with self.lock: live=dict(self.live)
                for name,pid in live.items():
                    try:row['processes'][name]=proc(pid)
                    except (FileNotFoundError,ProcessLookupError):pass
                row['system_cpu']=Path('/proc/stat').read_text().splitlines()[0]
                row['loadavg']=Path('/proc/loadavg').read_text().strip()
                f.write(json.dumps(row)+'\n');self.stop.wait(.1)
    def __enter__(self):
        try:
            return self.start()
        except BaseException:
            self.close()
            raise
    def start(self):
        for port in range(5220,5225):
            with socket.socket() as s:
                assert s.connect_ex(('127.0.0.1',port))!=0,f'Port {port} already owned; refusing to run'
        self.thread=threading.Thread(target=self.monitor,daemon=True);self.thread.start()
        env={'ASPNETCORE_ENVIRONMENT':'Production','DOTNET_ROOT':'/usr/share/dotnet','DOTNET_ROLL_FORWARD':'Disable','BENCHMARK_SUBGRAPH_DELAY_MS':str(self.delay)}
        for name,port in [('Accounts',5221),('Inventory',5222),('Products',5223),('Reviews',5224)]:
            source_env={**env,'DOTNET_ROLL_FORWARD':'LatestPatch','ASPNETCORE_URLS':f'http://localhost:{port}'}
            self.launch(name,['dotnet',FROZEN/name/f'eShop.{name}.dll'],FROZEN/name,source_env,'4-7,12-15')
        gateway_env={**env,'ASPNETCORE_URLS':'http://localhost:5220','BENCH_CAPTURE_PATH':str(self.out/'wire.jsonl') if self.capture else ''}
        if not self.capture:gateway_env.pop('BENCH_CAPTURE_PATH');gateway_env['BENCH_CAPTURE_PATH_DISABLED']='1'
        # Avoid inheriting diagnostic knobs from an external shell.
        gateway_env['BENCH_DIAGNOSTIC_RUN']='0'
        if not self.capture:os.environ.pop('BENCH_CAPTURE_PATH',None)
        self.gateway=self.launch('gateway',['dotnet',FROZEN/'gateway/eShop.Gateway.dll'],FROZEN/'gateway',gateway_env,'2-3,10-11')
        deadline=time.monotonic()+60
        while time.monotonic()<deadline:
            try:
                result=post(5220,{'query':'{ __typename }'})
                if result.get('data',{}).get('__typename')=='Query':break
            except Exception:pass
            if self.gateway.poll() is not None:raise RuntimeError('Gateway exited during startup')
            time.sleep(.2)
        else:raise TimeoutError('Gateway startup')
        for source_port in range(5221,5225):
            deadline=time.monotonic()+30
            while time.monotonic()<deadline:
                with socket.socket() as probe:
                    if probe.connect_ex(('127.0.0.1',source_port))==0:break
                time.sleep(.1)
            else:raise TimeoutError(f'Source port {source_port}')
        dump(self.out/'session.json',{'start':stamp(),'source_delay_ms':self.delay,'capture':self.capture,'gateway':proc(self.gateway.pid)})
        (self.out/'gateway-maps.txt').write_text(Path(f'/proc/{self.gateway.pid}/maps').read_text())
        for name,pid in self.live.items():
            (self.out/(name+'-status.txt')).write_text(Path(f'/proc/{pid}/status').read_text())
        if not self.capture:
            self.counter=self.launch('counters',[TOOLS/'dotnet-counters','collect','-p',self.gateway.pid,'--format','json','-o',self.out/'counters.json','--counters','System.Runtime','--refresh-interval','1'],HERE,{},'0,8')
        return self
    def close(self):
        self.stop.set()
        if hasattr(self,'thread'):self.thread.join(2)
        failures=[]
        for p,identity,log in reversed(self.owned):
            if p.poll() is None:
                try:
                    if proc(p.pid)['start_ticks']!=identity['start_ticks']:raise RuntimeError('PID identity changed')
                    p.send_signal(signal.SIGINT if identity['name'] in ['counters','trace'] else signal.SIGTERM)
                    try:p.wait(timeout=8)
                    except subprocess.TimeoutExpired:p.kill();p.wait(timeout=3)
                except ProcessLookupError:pass
            log.close()
            if p.poll() is None:failures.append(p.pid)
        dump(self.out/'cleanup.json',{'end':stamp(),'remaining_owned_pids':failures,'returncodes':{i['name']:p.returncode for p,i,l in self.owned}})
        assert not failures
    def __exit__(self,*args):self.close()
    def k6(self,name,workload='TestQuery',duration=30,mode='constant',vus=50,raw=True):
        out=self.out/name;out.mkdir()
        args=[K6,'run','--quiet','--address','127.0.0.1:0']
        if raw:args+=['--out',f'json={out}/samples.json.gz']
        for k,v in {'MODE':mode,'BENCH_OVER_TIME':f'{duration}s','BENCH_VUS':vus,'GATEWAY_ENDPOINT':ENDPOINT,'SUMMARY_PATH':out,'WORKLOAD_PATH':HERE/f'workloads/{workload}.json','ORACLE_PATH':BASE/f'fidelity/{workload}-oracle.json'}.items():args+=['-e',f'{k}={v}']
        args+=[HERE/'fixture/k6/k6.js' if workload=='TestQuery' else HERE/'focused.js']
        dump(out/'command.json',{'argv':list(map(str,args)),'start':stamp(),'workload':workload,'duration':duration,'mode':mode,'vus':vus})
        p=self.launch(name,args,HERE,{},'1,9')
        try:rc=p.wait(timeout=duration+120)
        except subprocess.TimeoutExpired:p.terminate();raise
        with self.lock:self.live.pop(name,None)
        dump(out/'completion.json',{'end':stamp(),'returncode':rc})
        assert rc==0,f'k6 {name} failed {rc}'
        summary=json.loads((out/'k6_summary.json').read_text())
        successes=summary.get('metrics',{}).get('success_rate',{}).get('values',{})
        assert successes.get('rate')==1,f'Non-success responses: {successes}'
        return out

def fidelity():
    out=BASE/'fidelity'
    session=Session(out,0,True)
    try:
        with session as s:
            for name,port in [('Accounts',5221),('Inventory',5222),('Products',5223),('Reviews',5224)]:
                dump(out/(name+'-service.json'),post(port,{'query':'{ _service { sdl } }'}))
            for name in ['TestQuery','ordinary','entities1','entities9']:
                payload=json.loads((HERE/f'workloads/{name}.json').read_text())
                response=post(5220,payload,{'Fusion-Operation-Plan':'1','Authorization':f'Bearer fidelity-{name}'})
                dump(out/(name+'-plan-response.json'),response)
                assert 'errors' not in response,response.get('errors')
                assert 'operationPlan' in response.get('extensions',{}).get('fusion',{}),response.keys()
                oracle={'data':response['data']};dump(out/(name+'-oracle.json'),oracle)
                if name=='ordinary':assert oracle=={'data':{'me':{'id':'1','name':'Uri Goldshtein'}}}
                elif name.startswith('entities'):assert [p['upc'] for p in oracle['data']['topProducts']]==[str(i) for i in range(1, int(name.removeprefix('entities'))+1)]
            lines=[json.loads(line) for line in (out/'wire.jsonl').read_text().splitlines()]
            assert any('_entities' in (r['body'] or '') for r in lines)
            assert any('representations' in (r['body'] or '') for r in lines)
            s.k6('original-oracle-check',duration=2,vus=2)
    finally:
        if any(p.poll() is None for p,_,_ in session.owned):session.close()

def baseline():
    for repetition in [1,2]:
        for delay in [0,4]:
            with Session(BASE/f'baseline-r{repetition}-delay{delay}',delay) as s:
                s.k6('warmup',duration=30,raw=False)
                s.k6('TestQuery',duration=30)
                if delay==0:
                    s.k6('ordinary',workload='ordinary',duration=20)
                    s.k6('entities9',workload='entities9',duration=20)
                else:s.k6('burst',duration=60,mode='ramping',vus=500)

def resume_baseline():
    # Retain valid first TestQuery run; recover only previously unmeasured cases.
    with Session(BASE/'baseline-r1-delay0-continuation',0) as s:
        s.k6('warmup',duration=30,raw=False)
        s.k6('ordinary',workload='ordinary',duration=20)
        s.k6('entities9',workload='entities9',duration=20)
    for repetition in [1,2]:
        for delay in ([4] if repetition==1 else [0,4]):
            with Session(BASE/f'baseline-r{repetition}-delay{delay}',delay) as s:
                s.k6('warmup',duration=30,raw=False)
                s.k6('TestQuery',duration=30)
                if delay==0:
                    s.k6('ordinary',workload='ordinary',duration=20)
                    s.k6('entities9',workload='entities9',duration=20)
                else:s.k6('burst',duration=60,mode='ramping',vus=500)

def focused_smoke():
    with Session(BASE/'focused-smoke',0) as s:
        s.k6('ordinary',workload='ordinary',duration=2,vus=2)
        s.k6('entities9',workload='entities9',duration=2,vus=2)

def profiles():
    with Session(BASE/'profiles',0) as s:
        s.k6('warmup',duration=30,raw=False)
        for name,profile,duration,workload in [('cpu','dotnet-sampled-thread-time',30,'TestQuery'),('alloc','gc-verbose',15,'TestQuery'),('entities9-alloc','gc-verbose',15,'entities9')]:
            trace_args=[TOOLS/'dotnet-trace','collect','-p',s.gateway.pid,'--duration',f'00:00:{duration:02d}','--profile',profile,'-o',s.out/(name+'.nettrace')]
            if name=='cpu':trace_args+=['--format','speedscope']
            if workload!='TestQuery':s.k6(name+'-warmup',workload=workload,duration=15,raw=False)
            # Start load first, then collect after setup and settling.
            with concurrent.futures.ThreadPoolExecutor(max_workers=1) as executor:
                traffic=executor.submit(s.k6,name+'-load',workload,duration+20)
                time.sleep(10)
                p=s.launch('trace',trace_args,HERE,{},'0,8')
                assert p.wait(timeout=duration+35)==0
                traffic.result(timeout=duration+140)
            with s.lock:s.live.pop('trace',None)

if __name__=='__main__':
    def terminated(signum, frame):
        raise SystemExit(128+signum)
    signal.signal(signal.SIGTERM, terminated)
    parser=argparse.ArgumentParser();parser.add_argument('phase',choices=['fidelity','focused-smoke','baseline','resume-baseline','profiles','all']);args=parser.parse_args()
    dump(BASE/('campaign-'+args.phase+'-start.json'),{'start':stamp(),'pid':os.getpid(),'phase':args.phase,'source_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest()})
    if args.phase in ['fidelity','all']:fidelity()
    if args.phase in ['baseline','all']:baseline()
    if args.phase=='focused-smoke':focused_smoke()
    if args.phase=='resume-baseline':resume_baseline()
    if args.phase in ['profiles','all']:profiles()
    dump(BASE/('campaign-'+args.phase+'-end.json'),{'end':stamp()})
