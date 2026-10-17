"""Offline profile conversion. Run only after the load and services have stopped."""
import json, os, pathlib, subprocess
ROOT=pathlib.Path(__file__).resolve().parent
TOOLS=pathlib.Path(os.environ['FUSION_PERF_TOOLS'])
ANALYZER=ROOT/'trace-analyzer/bin/Release/net10.0/TraceAnalyzer'
ENV=dict(os.environ,DOTNET_EnableDiagnostics='0')
def run(args,output):
    with open(output,'w') as stream:
        completed=subprocess.run([str(a) for a in args],stdout=stream,stderr=subprocess.PIPE,env=ENV)
    output.with_suffix(output.suffix+'.stderr').write_bytes(completed.stderr)
    if completed.returncode:
        raise RuntimeError((args,completed.returncode,completed.stderr.decode()[-2000:]))
directories=sorted({p.parent for p in ROOT.rglob('cpu.nettrace')})
for directory in sorted(directories):
    if not directory.is_dir():
        continue
    if (directory/'cpu-events.json').exists() and (directory/'allocation-reduced.json').exists():
        continue
    if (directory/'cpu.nettrace').exists():
        run([TOOLS/'dotnet-trace','report',directory/'cpu.nettrace','topN','-n','60'],directory/'cpu-self.txt')
        run([TOOLS/'dotnet-trace','report',directory/'cpu.nettrace','topN','-n','60','--inclusive'],directory/'cpu-inclusive.txt')
        run(['python3',ROOT/'agg_speedscope.py',directory/'cpu.speedscope.json'],directory/'cpu-reduced.json')
        run([ANALYZER,directory/'cpu.nettrace'],directory/'cpu-events.json')
    if (directory/'alloc.nettrace').exists():
        run([ANALYZER,directory/'alloc.nettrace'],directory/'allocation-reduced.json')
    print(directory.name,flush=True)
for path in sorted(list((ROOT/'heaps').rglob('*.gcdump'))+list((ROOT/'history').glob('*/heap.gcdump'))):
    if path.with_suffix('.report.txt').exists():
        continue
    run([TOOLS/'dotnet-gcdump','report',path],path.with_suffix('.report.txt'))
    print(path.name,flush=True)
