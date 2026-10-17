"""Keep actual leaf frames. Never reassign an unresolved native leaf to its managed caller."""
import collections,json,pathlib,re,sys
data=json.loads(pathlib.Path(sys.argv[1]).read_text())
frames=[f['name'] for f in data['shared']['frames']]
totals=collections.Counter()
selfs=collections.Counter()
inclusive=collections.Counter()
chains=collections.defaultdict(collections.Counter)
idle=re.compile(r'LowLevelLifoSemaphore|WaitHandle.Wait|Monitor.Wait|GC.RunFinalizers|FinalizerThread|FileSystemWatcher|Interop[+]Sys.Read|ManualResetEventSlim.Wait')
def consume(stack,weight):
    if not stack:
        return
    names=[frames[i] for i in stack]
    # CPU_TIME is the converter's synthetic sample marker, not an unresolved native frame.
    while names and names[-1]=='CPU_TIME':
        names.pop()
    if not names:
        return
    full=' <- '.join(reversed(names))
    leaf=names[-1]
    if 'PollGC' in full:
        category='gc_suspension'
    elif idle.search(full):
        category='idle_wait'
    elif 'MaybeAddWorkingWorker' in full:
        category='threadpool_wakeup'
    elif 'Monitor.Enter_Slowpath' in full or 'SpinLock.ContinueTryEnter' in full:
        category='synchronization_uncertain'
    elif leaf in ['UNMANAGED_CODE_TIME','?!?'] or '!' not in leaf:
        category='native_or_unresolved'
    elif any(x in leaf for x in ['SocketPal.','SocketAsyncEngine.EventLoop','WorkerThread.WorkerThreadStart']):
        category='transport_or_scheduler_boundary'
    else:
        category='managed_work_candidates'
    totals[category]+=weight
    chains[category][' <- '.join(reversed(names[-14:]))]+=weight
    selfs[leaf]+=weight
    for name in set(names):
        inclusive[name]+=weight
for profile in data['profiles']:
    if profile['type']=='sampled':
        for stack,weight in zip(profile['samples'],profile.get('weights',[1]*len(profile['samples']))):
            consume(stack,weight)
    else:
        stack=[]
        prev=profile['startValue']
        for event in profile['events']:
            weight=event['at']-prev
            if stack and weight>0:
                consume(stack,weight)
            if event['type']=='O':
                stack.append(event['frame'])
            else:
                stack.pop()
            prev=event['at']
total=sum(totals.values())
def rows(values,n):
    return [dict(frame=k,weight=v,percentAll=100*v/total) for k,v in values.most_common(n)]
print(json.dumps(dict(unit=data['profiles'][0]['unit'],total=total,categories=dict(totals),
                      self=rows(selfs,80),inclusive=rows(inclusive,120),
                      chains={k:rows(v,50) for k,v in chains.items()}),indent=2))
