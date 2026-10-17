C12 result: ABANDON, primary and independent reviews agree.

The actual production client microbenchmark shows reliable native allocation savings with repeated or unresolved elapsed costs. No VU or equivalent microbenchmark retry is justified. Elapsed time is not measured process CPU.

All five native launches completed with 20 native MemoryDiagnoser records and 240 actual observations. No observations were trimmed. Runtime was .NET 10.0.12, default server GC adaptation; only the exact baseline/tested candidate Engine DLL/PDB differed.

| Case | B1 ns | C12a ns | B2 ns | C12b ns | B3 ns | Native bytes B1/C12a/B2/C12b/B3 |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| ordinary | 1632.803 | 2129.046 | 1783.078 | 1633.439 | 1523.980 | 3470 / 3404 / 3470 / 3406 / 3470 |
| async-ordinary | 3815.565 | 4067.918 | 4100.716 | 4315.152 | 3171.678 | 4024 / 3880 / 4024 / 3887 / 4024 |
| async-alias2 | 4964.003 | 6047.093 | 5628.619 | 5294.835 | 5243.101 | 4536 / 4397 / 4540 / 4397 / 4536 |
| async-custom | 4125.897 | 3654.217 | 4195.227 | 3847.599 | 3871.992 | 4030 / 4032 / 4030 / 4038 / 4030 |

Async ordinary saves 137–144 B but elapsed is +6.614%/-0.800% on the first launch and +5.229%/+36.053% on the second, against both adjacent controls. Async alias2 saves 139–143 B but first-launch elapsed is +21.819%/+7.435%, then -5.930%/+0.987%. Synchronous ordinary saves 64–66 B with first-launch costs +30.392%/+19.403% and second-launch -8.392%/+7.182%.

Custom-client control elapsed moves favorably, while baseline distributions also shift. These observations preserve temporal uncertainty; no timing variation is assigned a cause and no favorable control is selected. Reliable allocation-only improvements are acceptable, but these data do not establish an acceptable complete tradeoff.

Optional calibrated allocation failed during baseline async-custom block0: before-calibration process bytes 523912 versus expected 523240, a 672-byte discrepancy. The guard stopped the campaign before candidate allocation. All earlier baseline blocks and the invalid block remain in raw evidence. This optional stop is not the reason for rejecting complete native results. No candidate controlled-allocation saving is claimed, and cross-thread thread-local byte differences are not treated as totals.

Campaign ran 18:05:32.352708–18:06:51.859632 UTC. Owner cleanup passed 18:07:03.695539 and independent cleanup 18:07:18.684492: all eight exact parent/child identities gone and lock free. The original tested engine a5593442501444e2e6964065a5ab9b51666ecd38fbc096f79e67a5a2ce7adf83 was used. All preparation, initial rejected-freeze fingerprints, source, tests, raw native exports, failed allocation stderr and cleanup evidence are retained.

The authored analyzer and result report pass whitespace checks. Native BDN exports retain their original trailing whitespace so raw evidence remains byte-for-byte unchanged.
