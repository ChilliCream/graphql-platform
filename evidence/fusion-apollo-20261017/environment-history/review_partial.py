"""Independent read-only native sample/accounting audit of the retained partial baseline."""
import bisect,collections,datetime,gzip,hashlib,json,pathlib
ROOT=pathlib.Path('/workspaces/platform-1')
SOURCE=ROOT/'perf/fusion-apollo-baseline-20261017b/baseline-r1-delay0'
OUT=pathlib.Path(__file__).resolve().parent

def seconds(value):
    return datetime.datetime.fromisoformat(value.replace('Z','+00:00')).timestamp()

def percentile(values,p):
    n=(len(values)-1)*p;lo=int(n);hi=min(lo+1,len(values)-1)
    return values[lo]+(values[hi]-values[lo])*(n-lo)

counts=collections.Counter();latencies=[];first=float('inf');last=0;bad_status=0
setup=0;successes=0;failures=0;request_completions=[]
with gzip.open(SOURCE/'TestQuery/samples.json.gz','rt') as stream:
    for line in stream:
        row=json.loads(line)
        if row.get('type')!='Point':continue
        data=row['data'];metric=row['metric'];tags=data.get('tags',{})
        if tags.get('group')=='::setup':
            if metric=='http_reqs':setup+=data['value']
            continue
        if tags.get('scenario')!='default':continue
        if metric in ['http_reqs','iterations','success_rate','http_req_failed','http_req_duration','dropped_iterations']:counts[metric]+=1
        if metric=='success_rate':
            successes+=data['value'];failures+=1-data['value']
        if metric=='http_req_duration':
            end=seconds(data['time']);start=end-data['value']/1000
            first=min(first,start);last=max(last,end);latencies.append(data['value']);request_completions.append(end)
            bad_status+=tags.get('status')!='200'
latencies.sort()
rows=[json.loads(line) for line in (SOURCE/'process-samples.jsonl').read_text().splitlines()]
def interpolate(name,t):
    points=[(v['time'],v['processes'][name]['cpu_seconds']) for v in rows if name in v['processes']]
    times=[v[0] for v in points];i=bisect.bisect_left(times,t)
    assert 0<i<len(points),(name,t,times[0],times[-1])
    a,b=points[i-1],points[i];return a[1]+(b[1]-a[1])*(t-a[0])/(b[0]-a[0])
window=last-first;proc={}
for name,cores in [('gateway',4),('Accounts',8),('Inventory',8),('Products',8),('Reviews',8),('TestQuery',2),('counters',2)]:
    matching=[r['processes'][name] for r in rows if first<=r['time']<=last and name in r['processes']]
    if not matching:continue
    cpu=interpolate(name,last)-interpolate(name,first)
    proc[name]={'cpu_seconds':cpu,'core_equivalents':cpu/window,'fraction_assigned_cpu':cpu/window/cores,'max_rss_bytes':max(v['rss_bytes'] for v in matching),'min_rss_bytes':min(v['rss_bytes'] for v in matching),'max_threads':max(v['threads'] for v in matching),'start_ticks_unique':sorted(set(v['start_ticks'] for v in matching))}
counter_text=(SOURCE/'counters.json').read_text(); counter_container_complete=True
try: counter_root=json.loads(counter_text)
except json.JSONDecodeError:
    counter_container_complete=False
    counter_root=json.loads(counter_text.rstrip().rstrip(',')+']}')
counters=counter_root['Events'];names=sorted(set(v['name'] for v in counters))
cpu_metrics=[v for v in counters if 'cpu.time' in v['name'] and first<=seconds(v['timestamp'])<=last]
bytag=collections.defaultdict(list)
for v in cpu_metrics:bytag[v.get('tags','')].append(v)
checks=[]
# dotnet-counters presents this counter as interval deltas (Rate), not cumulative CPU.
for tag,values in bytag.items():
    a,b=values[0],values[-1];start=seconds(a['timestamp']);end=seconds(b['timestamp'])
    assert all(v['counterType']=='Rate' and '(s / 1 sec)' in v['name'] for v in values)
    runtime=sum(v['value'] for v in values[1:]);os_delta=interpolate('gateway',end)-interpolate('gateway',start)
    checks.append({'tags':tag,'runtime_unit_name':a['name'],'first_boundary':a,'last_boundary':b,'included_rate_samples':len(values)-1,'runtime_interval_delta_sum_seconds':runtime,'matching_os_total_cpu_seconds':os_delta})
combined_rate=sum(v['runtime_interval_delta_sum_seconds'] for v in checks)
combined_os=sum(v['matching_os_total_cpu_seconds'] for v in checks)/len(checks)
report={'source':str(SOURCE),'window_start_epoch':first,'window_end_epoch':last,'window_seconds':window,'window_rule':'Minimum scenario-default completion minus http_req_duration to maximum scenario-default completion; setup excluded. Duration excludes blocked/connect/TLS; matching process CPU interpolated from100ms samples with10ms ticks.','native_counts':dict(counts),'setup_http_requests':setup,'successes':successes,'failures':failures,'non200_latency_samples':bad_status,'throughput_success_per_second':successes/window,'latency_ms':{str(p):percentile(latencies,p) for p in [.5,.95,.99,.999]},'processes':proc,'source_combined_cores':sum(proc[n]['core_equivalents'] for n in ['Accounts','Inventory','Products','Reviews']),'gateway_cpu_ms_per_success':proc['gateway']['cpu_seconds']*1000/successes,'counter_container_complete':counter_container_complete,'runtime_cpu_checks':checks,'combined_runtime_cpu_seconds':combined_rate,'matching_os_cpu_seconds':combined_os,'runtime_vs_os_relative_difference':combined_rate/combined_os-1,'runtime_metric_names':names,'limitations':['This is a retained initial baseline observation, not a candidate comparison.','CPU tick/interpolation checks validate accounting only, not full-phase uncertainty.','Assigned cores share virtualized host resources; no hardware counters collected.','The four subgraphs share8CPUs, their fractions must be assessed together.']}
(OUT/'PARTIAL-BASELINE-REVIEW.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='runtime_metric_names'},indent=2))
