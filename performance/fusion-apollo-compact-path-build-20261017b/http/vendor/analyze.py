"""Summarize retained native samples and matching process observations, without reruns."""
import argparse, bisect, collections, datetime, gzip, hashlib, json, math, statistics
from pathlib import Path

def seconds(value):return datetime.datetime.fromisoformat(value.replace('Z','+00:00')).timestamp()
def quantile(sorted_values,p):
    rank=(len(sorted_values)-1)*p;i=int(rank);j=min(i+1,len(sorted_values)-1)
    return sorted_values[i]+(sorted_values[j]-sorted_values[i])*(rank-i)
def load_counters(path):
    text=path.read_text()
    try:return json.loads(text)['Events'],True
    except json.JSONDecodeError:
        # SIGINT on non-TTY dotnet-counters leaves complete events and a trailing comma.
        return json.loads(text.rstrip().rstrip(',')+']}')['Events'],False

def analyze(case, rows, counters, counter_complete):
    trace_bounds={'cpu-load':('2026-10-17T15:58:52.9190000Z','2026-10-17T15:59:23.0077164Z'),'alloc-load':('2026-10-17T15:59:43.2800000Z','2026-10-17T15:59:58.4007500Z'),'entities9-alloc-load':('2026-10-17T16:00:33.9710000Z','2026-10-17T16:00:49.0509961Z')}
    trace_start,trace_end=map(seconds,trace_bounds.get(case.name,('1970-01-01T00:00:00Z','1970-01-01T00:00:00Z')))
    trace_counts=collections.Counter()
    counts=collections.Counter();sums=collections.Counter();setup=0;latencies=[];status=collections.Counter();request_first=math.inf;request_last=0;iteration_first=math.inf;iteration_last=0
    with gzip.open(case/'samples.json.gz','rt') as f:
        for line in f:
            r=json.loads(line)
            if r.get('type')!='Point':continue
            d=r['data'];m=r['metric'];tags=d.get('tags',{})
            if tags.get('group')=='::setup':
                if m=='http_reqs':setup+=d['value']
                continue
            if not tags.get('scenario'):continue
            if m in ['http_reqs','iterations','success_rate','http_req_failed','http_req_duration','dropped_iterations']:
                counts[m]+=1;sums[m]+=d['value']
                if trace_start<=seconds(d['time'])<=trace_end:trace_counts[m]+=d['value'] if m!='http_req_duration' else 1
            if m=='http_req_duration':
                end=seconds(d['time']);request_first=min(request_first,end-d['value']/1000);request_last=max(request_last,end)
                latencies.append((d['value'],d['time']));status[tags.get('status','missing')]+=1
            elif m=='iteration_duration':
                end=seconds(d['time']);iteration_first=min(iteration_first,end-d['value']/1000);iteration_last=max(iteration_last,end)
    assert latencies,case
    assert sums['http_reqs']==counts['http_req_duration']==sums['iterations']==counts['success_rate'],(case,counts,sums)
    assert sums['success_rate']==counts['success_rate'] and sums['http_req_failed']==0 and set(status)=={'200'},(case,sums,status)
    start=request_first;end=request_last;duration=end-start
    points={}
    for r in rows:
        for name,p in r['processes'].items():points.setdefault(name,[]).append((r['time'],p))
    # Seed fast-starting generator processes from their immediately recorded launch identity.
    for line in (case.parent/'process-identities.jsonl').read_text().splitlines():
        identity=json.loads(line);name=identity['name'];t=seconds(identity['utc'])
        if name in points and t<points[name][0][0]:points[name].insert(0,(t,identity))
    def cpu(name,t):
        values=points[name];times=[v[0] for v in values];i=bisect.bisect_left(times,t)
        assert 0<i<len(times),(case,name,t,times[0],times[-1])
        a,b=values[i-1],values[i]
        return a[1]['cpu_seconds']+(b[1]['cpu_seconds']-a[1]['cpu_seconds'])*(t-a[0])/(b[0]-a[0])
    processes={}
    for name,cores in [('gateway',4),('Accounts',8),('Inventory',8),('Products',8),('Reviews',8),(case.name,2),('counters',2)]:
        matching=[p for t,p in points.get(name,[]) if start<=t<=end]
        if not matching:continue
        used=cpu(name,end)-cpu(name,start)
        per_second=[cpu(name,start+i+1)-cpu(name,start+i) for i in range(int(duration))]
        processes[name]={'cpu_seconds':used,'core_equivalents':used/duration,'fraction_assigned_cpu':used/duration/cores,'max_rss_bytes':max(v['rss_bytes'] for v in matching),'min_rss_bytes':min(v['rss_bytes'] for v in matching),'max_threads':max(v['threads'] for v in matching),'start_ticks_unique':sorted(set(v['start_ticks'] for v in matching)),'cpu_core_equivalents_1s_max':max(per_second),'cpu_core_equivalents_1s_p95':quantile(sorted(per_second),.95)}
    sorted_latencies=sorted(latencies);values=[v for v,_ in sorted_latencies]
    tails={}
    for p in [.99,.999]:
        rank=(len(values)-1)*p;idx=int(rank)
        cutoff=quantile(values,p)
        clusters=collections.Counter(t[:19] for v,t in latencies if v>=cutoff)
        tails[str(p)]={'fractional_zero_based_rank':rank,'neighboring_samples':sorted_latencies[max(0,idx-3):idx+5],'largest_1s_clusters':clusters.most_common(10),'at_or_above_threshold':sum(clusters.values())}
    rate_groups=collections.defaultdict(list);rollup=collections.defaultdict(list)
    for c in counters:
        t=seconds(c['timestamp'])
        if start<=t<=end:
            rollup[(c['name'],c.get('tags',''),c['counterType'])].append(c)
            if 'cpu.time' in c['name']:rate_groups[c.get('tags','')].append(c)
    counter_rollup=[]
    for (name,tags,kind),values_c in sorted(rollup.items()):
        v=[c['value'] for c in values_c]
        counter_rollup.append({'name':name,'tags':tags,'type':kind,'observations':len(v),'first_timestamp':values_c[0]['timestamp'],'last_timestamp':values_c[-1]['timestamp'],'min':min(v),'max':max(v),'sum_interval_values_excluding_first':sum(v[1:]) if kind=='Rate' else None})
    cpu_checks=[]
    for tag,group in rate_groups.items():
        assert all(c['counterType']=='Rate' and '(s / 1 sec)' in c['name'] for c in group)
        first=seconds(group[0]['timestamp']);last=seconds(group[-1]['timestamp'])
        cpu_checks.append({'tags':tag,'start_epoch':first,'end_epoch':last,'intervals':len(group)-1,'runtime_cpu_seconds':sum(c['value'] for c in group[1:]),'os_total_cpu_seconds_matching_callback_window':cpu('gateway',last)-cpu('gateway',first)})
    runtime=sum(c['runtime_cpu_seconds'] for c in cpu_checks);os_cpu=statistics.mean(c['os_total_cpu_seconds_matching_callback_window'] for c in cpu_checks)
    iterations_cpu=cpu('gateway',iteration_last)-cpu('gateway',iteration_first)
    summary=json.loads((case/'k6_summary.json').read_text())
    assert summary['metrics']['success_rate']['values']['passes']==sums['success_rate']
    assert summary['metrics']['http_reqs']['values']['count']==sums['http_reqs']+setup
    assert summary['metrics']['iterations']['values']['count']==sums['iterations']
    trace_report=None
    if case.name in trace_bounds:
        assert start<trace_start<trace_end<end,(case,start,trace_start,trace_end,end)
        trace_report={'start':trace_bounds[case.name][0],'end':trace_bounds[case.name][1],'recorded_duration_seconds':trace_end-trace_start,'events_lost_reported':0,'native_sums_in_trace_window':dict(trace_counts),'gateway_cpu_seconds':cpu('gateway',trace_end)-cpu('gateway',trace_start),'traffic_contains_trace':True,'metadata_origin':'TraceAnalysis targetPID18692 metadata, independently parsed by execution-discovery worker'}
    return {'trace_window':trace_report,'case':str(case),'scenario':json.loads((case/'command.json').read_text()),'window_start_epoch':start,'window_end_epoch':end,'window_seconds':duration,'window_rule':'Earliest non-setup HTTP completion minus http_req_duration to last non-setup HTTP completion. Duration excludes blocked/connect/TLS;100ms process samples interpolated,10ms CPU ticks.','counts':dict(counts),'metric_sums':dict(sums),'setup_requests_excluded':setup,'status_counts':dict(status),'successes':sums['success_rate'],'throughput_success_per_second':sums['success_rate']/duration,'latency_ms':{'mean':statistics.mean(values),'median':quantile(values,.5),'p95':quantile(values,.95),'p99':quantile(values,.99),'p999':quantile(values,.999),'max':max(values)},'tails':tails,'processes':processes,'sources_combined_cores':sum(processes[n]['core_equivalents'] for n in ['Accounts','Inventory','Products','Reviews']),'gateway_cpu_ms_per_success':processes['gateway']['cpu_seconds']*1000/sums['success_rate'],'iteration_window_sensitivity':{'start_epoch':iteration_first,'end_epoch':iteration_last,'gateway_cpu_seconds':iterations_cpu,'difference_from_http_window_seconds':iterations_cpu-processes['gateway']['cpu_seconds']},'counter_json_container_complete':counter_complete,'counter_rollup':counter_rollup,'runtime_cpu_checks':cpu_checks,'runtime_vs_os_matching_callback_relative_difference':runtime/os_cpu-1,'notes':['Counter rate intervals use callback boundaries, which differ slightly from internal interval endpoints. This check is not a phase uncertainty bound.','Exact allocation-byte totals are not inferred from unvalidated process allocation counters.','Original TestQuery oracle checks response structure once per VU; focused controls compare complete captured data once per VU. Every iteration checks HTTP status and absence of GraphQL errors.']}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('results',type=Path);args=parser.parse_args();base=args.results
    reports=[]
    for session in sorted(base.glob('baseline-*'))+[base/'profiles']:
        if not session.is_dir():continue
        rows=[json.loads(l) for l in (session/'process-samples.jsonl').read_text().splitlines()]
        counters,complete=load_counters(session/'counters.json')
        for case in sorted(session.iterdir()):
            if not case.is_dir() or not (case/'samples.json.gz').exists() or not (case/'k6_summary.json').exists():continue
            if json.loads((case/'completion.json').read_text())['returncode']!=0:continue
            report=analyze(case,rows,counters,complete);reports.append(report)
            (case/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
            print(session.name,case.name,round(report['gateway_cpu_ms_per_success'],4),round(report['throughput_success_per_second'],1),flush=True)
    (base/'analysis.json').write_text(json.dumps(reports,indent=2)+'\n')
    lines=['| Session | Workload | Successes | req/s | CPU ms/request | median ms | p95 ms | p99 ms | p99.9 ms | Gateway cores/4 | Sources cores/8 | Generator cores/2 |','|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|']
    for r in reports:
        p=Path(r['case']);l=r['latency_ms'];g=r['processes'][p.name]['core_equivalents'];lines.append(f"| {p.parent.name} | {p.name} | {r['successes']:.0f} | {r['throughput_success_per_second']:.2f} | {r['gateway_cpu_ms_per_success']:.4f} | {l['median']:.3f} | {l['p95']:.3f} | {l['p99']:.3f} | {l['p999']:.3f} | {r['processes']['gateway']['core_equivalents']:.3f} | {r['sources_combined_cores']:.3f} | {g:.3f} |")
    (base/'analysis-table.md').write_text('\n'.join(lines)+'\n')
if __name__=='__main__':main()
