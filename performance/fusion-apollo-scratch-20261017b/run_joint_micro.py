#!/usr/bin/env python3
"""Run the predeclared C1/C3 campaign, only while its caller holds the exclusive lock."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

if len(sys.argv) != 2:
    raise SystemExit('usage: run_joint_micro.py absolute_campaign_directory')
root = Path(sys.argv[1]).resolve()
frozen = root / 'frozen'
results = root / 'results'
results.mkdir(exist_ok=True)
order = ['B', 'C1', 'C3', 'B', 'C3', 'C1', 'B', 'C1', 'C3', 'B']
expected = {
    'B': '9c436477e63d9c7f5c8668dcd75fec9771f500a5e71b14a1f02dd0bcf8fe6c5b',
    'C1': '1ddc8f2f868361222ee46cee206d434c2d0b05ff9afd5ebe47a6561e483d258f',
    'C3': '359a421c3387637ab022c658266252616e0ab58e262c72ad57a47c6ca6ae50cf'
}
manifest = {arm: {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (frozen / arm).iterdir() if p.is_file()} for arm in expected}
for arm in expected:
    assert manifest[arm]['HotChocolate.Fusion.Execution.dll'] == expected[arm]
    assert manifest[arm]['HotChocolate.Fusion.Execution.Benchmarks.dll'] == 'd9d5b1c4b2075ded61a47d43da9d9956dbab38b33d35f10e4cafdc387bb5fac1'
    differences = [name for name in manifest['B'] if manifest['B'][name] != manifest[arm][name]]
    assert all(name in ('HotChocolate.Fusion.Execution.dll', 'HotChocolate.Fusion.Execution.pdb') for name in differences)
(root / 'frozen-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')

def utc():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()

campaign = {'start': utc(), 'order': order, 'affinity': '2,3,10,11', 'runs': []}
(root / 'campaign.json').write_text(json.dumps(campaign, indent=2) + '\n')
try:
    for mode, warmup, observations, requests in [('timing', 512, 20, 128), ('allocation', 64, 4, 16)]:
        for index, arm in enumerate(order):
            stem = f'{mode}-{index:02d}-{arm}'
            output = results / (stem + '.json')
            error = results / (stem + '.stderr')
            assert not output.exists(), output
            command = ['taskset', '-c', '2,3,10,11', 'dotnet', str(frozen / arm / 'HotChocolate.Fusion.Execution.Benchmarks.dll'), mode, 'all', str(warmup), str(observations), str(requests)]
            row = {'arm': arm, 'mode': mode, 'index': index, 'command': command, 'start': utc(), 'stdout': str(output), 'stderr': str(error)}
            with output.open('w') as stdout, error.open('w') as stderr:
                process = subprocess.Popen(command, stdout=stdout, stderr=stderr, start_new_session=True)
                row['pid'] = process.pid
                row['processStartTicks'] = Path(f'/proc/{process.pid}/stat').read_text().split(') ', 1)[1].split()[19]
                started = time.monotonic()
                try:
                    row['exitCode'] = process.wait(timeout=90)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, 15)
                    try:
                        process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        os.killpg(process.pid, 9)
                        process.wait()
                    raise
                finally:
                    row['end'] = utc()
                    row['elapsedSeconds'] = time.monotonic() - started
                    campaign['runs'].append(row)
                    (root / 'campaign.json').write_text(json.dumps(campaign, indent=2) + '\n')
            if row['exitCode'] != 0:
                raise RuntimeError(f'{stem}: exit {row["exitCode"]}; evidence retained')
            parsed = json.loads(output.read_text())
            assert parsed['sha256'] == expected[arm]
            assert parsed['runtime'] == '10.0.12' and parsed['serverGc'] is True
            print(f'{stem} complete {row["elapsedSeconds"]:.3f}s', flush=True)
finally:
    campaign['end'] = utc()
    (root / 'campaign.json').write_text(json.dumps(campaign, indent=2) + '\n')
