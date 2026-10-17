"""Review retained plain baseline data; no processes are launched or workloads repeated.

Reuses the timestamp and interpolation method established in review_partial.py.
Invoke under flock -s -o while the exclusive measurement slot is free.
"""
import bisect,collections,datetime,gzip,hashlib,json,pathlib,statistics
ROOT=pathlib.Path('/workspaces/platform-1')
BASE=ROOT/'perf/fusion-apollo-baseline-20261017b'
OUT=pathlib.Path(__file__).resolve().parent/'full-plain-review'
OUT.mkdir(exist_ok=True)

def seconds(value):
    return datetime.datetime.fromisoformat(value.replace('Z','+00:00')).timestamp()

def quantile(values,p):
    n=(len(values)-1)*p;lo=int(n);hi=min(lo+1,len(values)-1)
    return values[lo]+(values[hi]-values[lo])*(n-lo)

def read_counters(path):
    text=path.read_text()
    try:return json.loads(text)['Events'],True
    except json.JSONDecodeError:
        # Non-TTY SIGINT leaves complete event objects followed by one comma.
        assert text.rstrip().endswith('},'),path
        return json.loads(text.rstrip().rstrip(',')+']}')['Events'],False

def native(path):
    counts=collections.Counter();values=collections.Counter();setup=0;lat=[];bad=0;first=float('inf');last=0;scenarios=set()
    with gzip.open(path,'rt') as stream:
        for line in stream:
            row=json.loads(line)
            if row.get('type')!='Point':continue
            d=row['data'];metric=row['metric'];tags=d.get('tags',{})
            if tags.get('group')=='::setup':
                if metric=='http_reqs':setup+=d['value']
                continue
            scenario=tags.get('scenario')
            if not scenario:continue
            scenarios.add(scenario)
            if metric in ['http_reqs','iterations','success_rate','http_req_failed','http_req_duration','dropped_iterations']:
                counts[metric]+=1;values[metric]+=d['value']
            if metric=='http_req_duration':
                end=seconds(d['time']);start=end-d['value']/1000
                first=min(first,start);last=max(last,end);lat.append((end,d['value']))
                bad+=tags.get('status')!='200'
    return {'counts':dict(counts),'values':dict(values),'setup_requests':setup,'latencies':lat,'first':first,'last':last,'non200':bad,'scenarios':sorted(scenarios)}

def review(case):
    source=case.parent;command=json.loads((case/'command.json').read_text());summary=json.loads((case/'k6_summary.json').read_text());completion=json.loads((case/'completion.json').read_text())
    key=source.name+'--'+case.name;cache=OUT/(key+'-native-cache.json')
    if cache.exists():raw=json.loads(cache.read_text())
    else:
        raw=native(case/'samples.json.gz');cache.write_text(json.dumps(raw,separators=(',',':'))+'\n')
    first,last=raw['first'],raw['last'];duration=last-first;lat=raw.pop('latencies');n=len(lat);ordered=sorted(v for t,v in lat)
    counts=raw['counts'];svalues=summary['metrics'];successes=raw['values']['success_rate']
    assert len({counts[k] for k in ['http_reqs','iterations','success_rate','http_req_duration','http_req_failed']})==1,(key,counts)
    assert successes==n==svalues['iterations']['values']['count']==svalues['success_rate']['values']['passes'],key
    assert n+raw['setup_requests']==svalues['http_reqs']['values']['count'],key
    assert raw['values']['http_req_failed']==0 and raw['non200']==0 and completion['returncode']==0,key
    rows=[json.loads(v) for v in (source/'process-samples.jsonl').read_text().splitlines()]
    points={}
    for row in rows:
        for name,p in row['processes'].items():points.setdefault(name,[]).append((row['time'],p['cpu_seconds'],p))
    def cpu(name,t):
        ps=points[name];times=[v[0] for v in ps];i=bisect.bisect_left(times,t)
        assert 0<i<len(ps),(name,t,times[0],times[-1])
        a,b=ps[i-1],ps[i];return a[1]+(b[1]-a[1])*(t-a[0])/(b[0]-a[0])
    processes={}
    for name,cores in [('gateway',4),('Accounts',8),('Inventory',8),('Products',8),('Reviews',8),(case.name,2),('counters',2)]:
        matching=[v[2] for v in points.get(name,[]) if first<=v[0]<=last]
        if not matching:continue
        delta=cpu(name,last)-cpu(name,first)
        bins=[cpu(name,min(first+i+1,last))-cpu(name,first+i) for i in range(int(duration)) if first+i+1<=last]
        processes[name]={'cpu_seconds':delta,'average_cores':delta/duration,'assigned_shared_cpu_count':cores,'max_full_1s_cores':max(bins),'p95_full_1s_cores':quantile(sorted(bins),.95),'rss_min_bytes':min(p['rss_bytes'] for p in matching),'rss_max_bytes':max(p['rss_bytes'] for p in matching),'rss_first_bytes':matching[0]['rss_bytes'],'rss_last_bytes':matching[-1]['rss_bytes'],'max_threads':max(p['threads'] for p in matching),'pid_start_identities':sorted(set((p['pid'],p['start_ticks']) for p in matching))}
    sources=['Accounts','Inventory','Products','Reviews'];combined_bins=[sum(cpu(name,first+i+1)-cpu(name,first+i) for name in sources) for i in range(int(duration)) if first+i+1<=last]
    one_second_tails=collections.Counter(int(t-first) for t,v in lat if v>=quantile(ordered,.999))
    rank=(n-1)*.999;rank_lo=int(rank);neighbors=[{'rank0':i,'latency_ms':ordered[i]} for i in range(max(0,rank_lo-5),min(n,rank_lo+7))]
    slowest=sorted(lat,key=lambda p:p[1],reverse=True)[:30]
    blocks=[]
    for i in range(int(duration//5)):
        start,end=first+i*5,first+(i+1)*5;block=sorted(v for t,v in lat if start<t<=end)
        blocks.append({'start_seconds':i*5,'end_seconds':(i+1)*5,'completed':len(block),'gateway_cpu_seconds':cpu('gateway',end)-cpu('gateway',start),'gateway_cpu_ms_per_completion':(cpu('gateway',end)-cpu('gateway',start))*1000/len(block),'latency_p50_ms':quantile(block,.5),'latency_p95_ms':quantile(block,.95),'latency_p99_ms':quantile(block,.99),'latency_p999_ms':quantile(block,.999),'tail_999_count':sum(v>=quantile(ordered,.999) for v in block)})
    events,container_complete=read_counters(source/'counters.json');groups=collections.defaultdict(list)
    for e in events:
        t=seconds(e['timestamp'])
        if first<=t<=last:groups[(e['name'],e.get('tags',''))].append(e)
    cpuchecks=[]
    for (name,tags),items in groups.items():
        if 'dotnet.process.cpu.time' not in name:continue
        assert all(v['counterType']=='Rate' and '(s / 1 sec)' in v['name'] for v in items)
        a,b=items[0],items[-1];start,end=seconds(a['timestamp']),seconds(b['timestamp'])
        cpuchecks.append({'name':name,'tags':tags,'first_boundary':a['timestamp'],'last_boundary':b['timestamp'],'included_intervals':len(items)-1,'runtime_cpu_seconds':sum(v['value'] for v in items[1:]),'os_total_cpu_same_callback_window':cpu('gateway',end)-cpu('gateway',start),'window_seconds':end-start})
    runtime=sum(c['runtime_cpu_seconds'] for c in cpuchecks);oscheck=statistics.mean(c['os_total_cpu_same_callback_window'] for c in cpuchecks)
    gc=[]
    for (name,tags),items in groups.items():
        if 'gc.pause.time' not in name and 'gc.collections' not in name:continue
        assert all(v['counterType']=='Rate' for v in items)
        gc.append({'name':name,'tags':tags,'first_boundary':items[0]['timestamp'],'last_boundary':items[-1]['timestamp'],'included_intervals':len(items)-1,'sum_interval_values_excluding_first':sum(v['value'] for v in items[1:]),'max_interval_value':max(v['value'] for v in items[1:]),'top_intervals':sorted([{'timestamp':v['timestamp'],'value':v['value']} for v in items[1:]],key=lambda v:v['value'],reverse=True)[:6]})
    snapshot={'case':key,'command':command,'counts':raw,'duration_seconds':duration,'success_requests_per_second':successes/duration,'latency_ms':{'arithmetic_mean':statistics.mean(ordered),'p50':quantile(ordered,.5),'p95':quantile(ordered,.95),'p99':quantile(ordered,.99),'p999':quantile(ordered,.999),'maximum':max(ordered)},'gateway_cpu_ms_per_success':processes['gateway']['cpu_seconds']*1000/successes,'processes':processes,'combined_subgraphs':{'average_cores':sum(processes[k]['average_cores'] for k in sources),'max_full_1s_cores':max(combined_bins),'assigned_shared_cpus':8},'counter_container_complete':container_complete,'cpu_counter_checks':cpuchecks,'runtime_cpu_check_combined_seconds':runtime,'os_cpu_check_seconds':oscheck,'runtime_cpu_relative_difference':runtime/oscheck-1,'cpu_counter_coverage_fraction':statistics.mean(c['window_seconds'] for c in cpuchecks)/duration,'gc_counter_windows':gc,'five_second_blocks':blocks,'p999_rank0':rank,'p999_neighbors':neighbors,'p999_exceedance_bins_seconds':dict(sorted(one_second_tails.items())),'top30_latencies':[{'completion_seconds_after_start':t-first,'latency_ms':v} for t,v in slowest],'limitations':['Subgraph processes share8cores; process_cpu sum is aggregate headroom, not proof against percore or nonCPU bottlenecks.','No percore utilization or perthread samples in plain phases. No saturation-capacity conclusion from closed50VU offered workload.','Runtime counters are interval deltas labelled Rate; incomplete JSON container recovered without modifying source. Sample timestamps are callback delivery, not exact meter read boundaries.','GC collection/pause sums omit partial edge intervals and do not establish exact perrequest allocation.','Timestamped native duration excludes blocked/connect/TLS; CPU brackets use earliest duration-start and final completion.100ms sample interpolation/10msCPU ticks limit exact edges.','Five-second blocks and requests within one launch are dependent; they are not independent replications.']}
    (OUT/(key+'.json')).write_text(json.dumps(snapshot,indent=2)+'\n')
    print(key,n,'cpu_ms',round(snapshot['gateway_cpu_ms_per_success'],6),'rps',round(snapshot['success_requests_per_second'],2),'cores',round(processes['gateway']['average_cores'],3),round(snapshot['combined_subgraphs']['average_cores'],3),round(processes[case.name]['average_cores'],3),'cpu_counter_diff',round(snapshot['runtime_cpu_relative_difference']*100,3),flush=True)
    return snapshot

# This reviewer spot-checks two raw second-launch cases. The baseline owner's
# analyze.py owns complete campaign parsing and reporting.
if __name__=='__main__':
    reports=[review(BASE/'baseline-r2-delay0'/'TestQuery'),review(BASE/'baseline-r2-delay4'/'burst')]
    (OUT/'spotchecks.json').write_text(json.dumps(reports,indent=2)+'\n')
    print('Reviewed two independent raw spotchecks',flush=True)
