import json, os, pathlib, subprocess, sys, threading, time, urllib.request
from lifecycle import ROOT, request
from workloads import CASES

TOOLS=pathlib.Path(os.environ['FUSION_PERF_TOOLS'])
K6=TOOLS/'k6'
PIDS=json.loads((ROOT/'pids.json').read_text())
HZ=os.sysconf('SC_CLK_TCK')
PAGESIZE=os.sysconf('SC_PAGE_SIZE')

def process(pid):
    try:
        raw=pathlib.Path(f'/proc/{pid}/stat').read_text()
        comm=raw.split('(',1)[1].rsplit(')',1)[0]
        fields=raw.rsplit(')',1)[1].split()
        return dict(pid=pid,comm=comm,cpu=(int(fields[11])+int(fields[12]))/HZ,rss=int(fields[21])*PAGESIZE,threads=int(fields[17]),minorFaults=int(fields[7]),majorFaults=int(fields[9]))
    except (FileNotFoundError,ProcessLookupError,PermissionError):
        return None

def snapshot():
    with request('http://127.0.0.1:5320/perf/state') as r:
        gateway=json.load(r)
    return dict(monotonic=time.monotonic(),utc=time.time(),gateway=gateway,processes={k:process(p) for k,p in PIDS.items()})

def monitor(path,stop):
    with open(path,'w',buffering=1) as f:
        while not stop.is_set():
            procs=[process(int(p.name)) for p in pathlib.Path('/proc').iterdir() if p.name.isdigit()]
            row=dict(gateway=snapshot()['gateway'],time=time.time(),monotonic=time.monotonic(),processes=[p for p in procs if p],cpu=pathlib.Path('/proc/stat').read_text().split('intr ')[0],load=pathlib.Path('/proc/loadavg').read_text(),memory=pathlib.Path('/proc/meminfo').read_text())
            f.write(json.dumps(row)+'\n')
            stop.wait(1)

def run(case,directory,duration=60,vus=None,diagnostic=None,rate=None):
    directory.mkdir(parents=True,exist_ok=True)
    env=dict(os.environ, REQUEST=str(ROOT/'requests'/f'{case["name"]}.json'), VUS=str(vus or case['vus']),DURATION=f'{duration}s',SUMMARY=str(directory/'summary.json'),DEFER='1' if case['kind']=='defer' else '0')
    if rate:
        env['RATE']=str(rate)
        env['TIME_UNIT']=case.get('timeUnit','1s')
    cmd=['taskset','-c','1,9',str(K6),'run','--quiet',str(ROOT/'load.js')]
    (directory/'command.json').write_text(json.dumps(dict(command=cmd,environment={k:env[k] for k in ['REQUEST','VUS','DURATION','SUMMARY','DEFER','RATE','TIME_UNIT'] if k in env}),indent=2))
    before=snapshot()
    stop=threading.Event()
    observer=threading.Thread(target=monitor,args=(directory/'processes.jsonl',stop),daemon=True)
    observer.start()
    with open(directory/'k6.log','w') as log:
        load=subprocess.Popen(cmd,env=env,stdout=log,stderr=subprocess.STDOUT)
        (directory/'loadgen.pid').write_text(str(load.pid))
        try:
            if diagnostic:
                # Tool runs are sequential and occur after a fresh 60-second warmup.
                time.sleep(5)
                tools=TOOLS
                pid=str(PIDS['Gateway'])
                commands=[
                    ('counters',[str(tools/'dotnet-counters'),'collect','-p',pid,'--format','json','-o',str(directory/'counters.json'),'--duration','00:00:00:20','--counters','System.Runtime,Microsoft.AspNetCore.Hosting,System.Net.Http']),
                    ('cpu',[str(tools/'dotnet-trace'),'collect','-p',pid,'--profile','dotnet-sampled-thread-time','--providers','Microsoft-Windows-DotNETRuntime:0x4000:5','--duration','00:00:00:30','--format','speedscope','-o',str(directory/'cpu.nettrace')]),
                    ('alloc',[str(tools/'dotnet-trace'),'collect','-p',pid,'--profile','gc-verbose','--duration','00:00:00:15','-o',str(directory/'alloc.nettrace')]),
                ]
                for label,command in commands:
                    print(time.strftime('%H:%M:%S'),case['name'],label,flush=True)
                    state=snapshot()
                    with open(directory/(label+'.log'),'w') as f:
                        result=subprocess.run(command,stdout=f,stderr=subprocess.STDOUT)
                    (directory/(label+'-interval.json')).write_text(json.dumps(dict(before=state,after=snapshot(),command=command,exitCode=result.returncode),indent=2))
                    if result.returncode:
                        raise RuntimeError(f'{label} failed')
            _,raw_status,usage=os.wait4(load.pid,0)
            status=os.waitstatus_to_exitcode(raw_status)
            load.returncode=status
            (directory/'loadgen-usage.json').write_text(json.dumps(dict(userCpu=usage.ru_utime,systemCpu=usage.ru_stime,maxRssKiB=usage.ru_maxrss,voluntarySwitches=usage.ru_nvcsw,involuntarySwitches=usage.ru_nivcsw),indent=2))
        finally:
            if load.poll() is None:
                load.terminate()
                load.wait()
            stop.set()
            observer.join()
    after=snapshot()
    (directory/'boundaries.json').write_text(json.dumps(dict(before=before,after=after,exitCode=status),indent=2))
    if status:
        raise RuntimeError(f'Load failed, inspect {directory}')
    samples=[json.loads(line)['gateway'] for line in (directory/'processes.jsonl').read_text().splitlines()]
    decreases=[dict(before=a['allocated'],after=b['allocated'],utc=b['utc']) for a,b in zip(samples,samples[1:]) if b['allocated']<a['allocated']]
    (directory/'counter-decreases.json').write_text(json.dumps(decreases,indent=2))
    if os.environ.get('FUSION_PERF_REQUIRE_MONOTONIC')=='1':
        assert not decreases,(directory,decreases)
    metrics=json.loads((directory/'summary.json').read_text())['metrics']
    assert metrics['success']['values']['rate']==1,directory
    if rate:
        assert metrics.get('dropped_iterations',{}).get('values',{}).get('count',0)==0,directory
    if case['kind']!='defer':
        assert metrics['body_bytes']['values']['min']==metrics['body_bytes']['values']['max'],directory
    for line in (directory/'k6.log').read_text().splitlines():
        if 'ACCOUNTING ' in line:
            # k6 emits console output as a JSON-escaped string in its text logger.
            value=line.split('ACCOUNTING ',1)[1].split('" source=',1)[0]
            if value.endswith('"'):
                value=value[:-1]
            value=value.replace('\\"','"')
            accounting=json.loads(value)
            (directory/'accounting.json').write_text(json.dumps(accounting,indent=2))
            break
    else:
        raise RuntimeError('Accounting not found')
    delta=(accounting['after']['timestamp']-accounting['before']['timestamp'])/accounting['before']['frequency']
    count=metrics['completed']['values']['count']
    cpu=accounting['after']['cpuMs']-accounting['before']['cpuMs']
    alloc=accounting['after']['allocated']-accounting['before']['allocated']
    print(time.strftime('%H:%M:%S'),str(directory.relative_to(ROOT)),f'{count/delta:.2f} RPS, {cpu/count:.3f} CPU ms/req, {alloc/count:.0f} B/req',flush=True)

def main():
    mode=sys.argv[1]
    if mode=='pilot':
        for case in CASES:
            run(case,ROOT/'pilot'/case['name'],10)
    elif mode=='measure':
        names=sys.argv[2:]
        for case in CASES:
            if names and case['name'] not in names:
                continue
            directory=ROOT/'measured'/case['name']
            print(time.strftime('%H:%M:%S'),case['name'],'WARMUP',flush=True)
            run(case,directory/'warmup-1',30)
            run(case,directory/'warmup-2',30)
            for rep in range(1,4):
                run(case,directory/f'rep-{rep}',60)
    elif mode=='paired':
        cases=[c for c in CASES if c['kind']=='variables']
        for case in cases:
            for i in [1,2]:
                run(case,ROOT/'paired'/case['name']/f'warmup-{i}',30,rate=100)
        for rep in range(1,4):
            for case in cases:
                run(case,ROOT/'paired'/case['name']/f'rep-{rep}',60,rate=100)
    elif mode=='confirm':
        for name in ['variables-50','list-20000-lookup']:
            case=next(c for c in CASES if c['name']==name)
            directory=ROOT/'confirmation'/name
            run(case,directory/'warmup-1',30)
            run(case,directory/'warmup-2',30)
            for rep in range(1,4):
                run(case,directory/f'rep-{rep}',60)
    elif mode=='curve':
        for name,vus in [('nested',16),('nested',128),('list-20000-lookup',4),('list-20000-lookup',16)]:
            case=next(c for c in CASES if c['name']==name)
            directory=ROOT/'curve'/f'{name}-c{vus}'
            run(case,directory/'warmup',45,vus)
            for rep in range(1,4):
                run(case,directory/f'rep-{rep}',60,vus)
    elif mode=='profile':
        names=sys.argv[2:] or ['nested','variables-200','list-20000-plain','list-20000-lookup']
        for name in names:
            case=next(c for c in CASES if c['name']==name)
            directory=ROOT/'profiles'/name
            run(case,directory/'warmup',60)
            run(case,directory,95,diagnostic=True)
    elif mode=='profile-pressure':
        case=next(c for c in CASES if c['name']=='nested')
        directory=ROOT/'profiles/nested-c128'
        run(case,directory/'warmup',60,128)
        run(case,directory,95,128,diagnostic=True)

if __name__=='__main__':
    main()
