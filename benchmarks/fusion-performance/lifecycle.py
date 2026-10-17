import json, os, pathlib, subprocess, time, urllib.request, signal

ROOT = pathlib.Path(__file__).resolve().parent
def request(url, data=None, accept='application/graphql-response+json'):
    return urllib.request.urlopen(urllib.request.Request(url,data=data,headers={'Content-Type':'application/json','Accept':accept}),timeout=120)

def start(gateway_environment=None):
    env=dict(os.environ, ASPNETCORE_ENVIRONMENT='Production', BENCHMARK_SIMULATE_LATENCY='1')
    pids={}
    for kind,port in [('Accounts',5321),('Inventory',5322),('Products',5323),('Reviews',5324),('Gateway',5320)]:
        directory=ROOT/'gateway' if kind=='Gateway' else ROOT/f'subgraphs/eShop.{kind}'
        exe='Gateway' if kind=='Gateway' else f'eShop.{kind}'
        affinity='2,3,10,11' if kind=='Gateway' else '4-7,12-15'
        app_env=dict(env,ASPNETCORE_URLS=f'http://127.0.0.1:{port}')
        if kind=='Gateway' and gateway_environment:
            app_env.update(gateway_environment)
        p=subprocess.Popen(['taskset','-c',affinity,str(directory/f'bin/Release/net10.0/{exe}')],cwd=directory,env=app_env,stdout=open(ROOT/(kind+'.log'),'w'),stderr=subprocess.STDOUT,start_new_session=True)
        pids[kind]=p.pid
    (ROOT/'pids.json').write_text(json.dumps(pids,indent=2))
    for _ in range(120):
        try:
            with request('http://127.0.0.1:5320/graphql',b'{"query":"{__typename}"}') as r:
                if json.load(r).get('data'):
                    return pids
        except Exception:
            time.sleep(.5)
    raise RuntimeError('Readiness failed')

def stop():
    pids=json.loads((ROOT/'pids.json').read_text())
    result={}
    for name,pid in pids.items():
        try:
            state=pathlib.Path(f'/proc/{pid}/stat').read_text().split(')')[1].split()[0]
            if state=='Z':
                result[name]={'pid':pid,'alreadyExited':True,'stateAfter':'Z'}
                continue
            cmd=pathlib.Path(f'/proc/{pid}/cmdline').read_bytes().replace(b'\0',b' ').decode()
            if str(ROOT) not in cmd:
                raise RuntimeError(f'Refusing to stop unrelated PID {pid}: {cmd}')
            os.kill(pid,signal.SIGTERM)
            result[name]={'pid':pid,'command':cmd,'signal':'SIGTERM'}
        except FileNotFoundError:
            result[name]={'pid':pid,'alreadyExited':True}
    time.sleep(2)
    for name,pid in pids.items():
        try:
            os.waitpid(pid,os.WNOHANG)
        except ChildProcessError:
            pass
        if pathlib.Path(f'/proc/{pid}').exists():
            state=pathlib.Path(f'/proc/{pid}/stat').read_text().split(')')[1].split()[0]
            result[name]['stateAfter']=state
            if state != 'Z':
                os.kill(pid,signal.SIGKILL)
                result[name]['escalated']='SIGKILL'
        else:
            result[name]['stateAfter']='absent'
    (ROOT/'cleanup.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))

if __name__ == '__main__':
    import sys
    print(start()) if sys.argv[1]=='start' else stop()
