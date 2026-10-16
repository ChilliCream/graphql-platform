#!/usr/bin/env python3
import sys,json,re,collections
from pathlib import Path
p=Path(sys.argv[1]); data=json.loads(p.read_text());frames=[f['name'] for f in data['shared']['frames']]
selfs=collections.Counter();incl=collections.Counter();locks=collections.Counter();groups=collections.Counter();chains=collections.Counter();busy=idle=suspended=native=wakeup=0.0
idle_re=re.compile(r'LowLevelLifoSemaphore|WaitHandle.Wait|Monitor.Wait|GC.RunFinalizers|FinalizerThread|FileSystemWatcher.*Read|Interop[+]Sys.Read')
group_patterns={
 'cost_tree_building':r'CostAnalysis.ConditionTreeBuilder[.]',
 'case_normalize':r'ExactCasesTraversal[+]CaseCursor.Normalize',
 'http_request_envelopes':r'DefaultGraphQLHttpClient.CreateRequestMessage|HttpRequestMessage[.][.]ctor',
 'cost_analysis':r'HotChocolate.CostAnalysis[.]',
 'source_property_lookup':r'SourceResultDocument.TryGetNamedPropertyValueCore',
 'value_completion':r'ValueCompletion[.]',
 'variable_coercion':r'OperationVariableCoercionMiddleware.*TryCoerceVariables|VariableCoercionHelper|JsonVariableCoercion',
 'json_property_lookup':r'JsonDocument.TryGetNamedPropertyValue',
 'request_json_parsing':r'Utf8GraphQLRequestParser.ParseRequest',
 'variable_sets':r'CreateVariableValueSets|BuildVariableValueSets|SerializeForwardedVariables|WriteForwardedVariables',
 'wide_masks':r'IsIncludedWide|BuildIncludeFlags|EvaluateIncludeConditions|ConditionFlags',
 'source_json_parsing':r'SourceResultDocument.*Parse|SourceResultDocumentBuilder',
 'result_mapping':r'ResultDataMapper|CompositeResultDocument.*(Insert|Set|Build|Create)|FetchResultStore.*(Add|Update|Set)',
 'lookup_batching':r'HttpSourceSchemaClient.*Alias|BuildAlias|CreateAlias|OperationBatchExecutionNode|VariableDedupTable',
 'defer_orchestration':r'ExecuteDeferred|DeferredWork|DeferredResult|DeferredExecution|IncrementalDelivery|DeliveryWork|DeferredFragment',
 'excluded_path_pool':r'PathSegment(Local)?Pool',
 'excluded_response_pipe':r'ChunkedPipeBufferWriter|HttpResponsePipeWriter|HttpResponseStream',
}
compiled_groups={k:re.compile(v) for k,v in group_patterns.items()}
lock_re=re.compile(r'SpinLock.*ContinueTryEnter|Monitor.*Enter_Slowpath|SpinWait.*SpinOnce|Lock.*TryEnterSlow')
def accept(stack,weight):
 global busy,idle,suspended,native,wakeup
 names=[frames[i] for i in stack]; full=' <- '.join(reversed(names))
 if any(lock_re.search(n) for n in names): locks[full]+=weight
 if any('PollGC' in n for n in names): suspended+=weight;return
 if any('MaybeAddWorkingWorker' in n for n in names): wakeup+=weight;return
 if names and ('ManualResetEventSlim.Wait' in full or 'ManualResetEventSlim!Wait' in full): idle+=weight;return
 if any(idle_re.search(n) for n in names): idle+=weight;return
 useful=[n for n in names if '!' in n and n not in ('CPU_TIME','UNMANAGED_CODE_TIME','?!?')]
 if not useful: native+=weight;return
 if 'SocketAsyncEngine.EventLoop' in useful[-1] or 'WorkerThread.WorkerThreadStart' in useful[-1]: idle+=weight;return
 busy+=weight;selfs[useful[-1]]+=weight
 for key,pattern in compiled_groups.items():
  if pattern.search(full): groups[key]+=weight
 chain=' <- '.join(reversed([n for n in useful if '!' in n][-8:]))
 chains[chain]+=weight
 for n in set(useful): incl[n]+=weight
for profile in data['profiles']:
 if profile['type']=='sampled':
  for stack,weight in zip(profile['samples'],profile.get('weights',[1]*len(profile['samples']))): accept(stack,weight)
 else:
  stack=[];prev=profile['startValue']
  for e in profile['events']:
   dt=e['at']-prev
   if stack and dt>0: accept(stack,dt)
   if e['type']=='O': stack.append(e['frame'])
   else: stack.pop()
   prev=e['at']
def rows(counter,n=30):return [{'frame':k,'time':v,'busy_percent':100*v/busy if busy else 0} for k,v in counter.most_common(n)]
print(json.dumps({'units':data['profiles'][0]['unit'],'busy':busy,'idle':idle,'gc_suspension':suspended,'unresolved':native,'threadpool_wakeup':wakeup,'groups':rows(groups,len(groups)),'self_chains':rows(chains,60),'self':rows(selfs,30),'hotchocolate_inclusive':rows(collections.Counter({k:v for k,v in incl.items() if 'HotChocolate.' in k}),30),'locks':rows(locks,len(locks))},indent=2))
