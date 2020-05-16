Remove-Item BenchmarkDotNet.Artifacts -Recurse -Force -Confirm:$false
Remove-Item Execution.Benchmarks/bin -Recurse -Force -Confirm:$false
Remove-Item Execution.Benchmarks/obj -Recurse -Force -Confirm:$false

dotnet run --project Execution.Benchmarks/ -c release

Remove-Item Execution.Benchmarks/bin -Recurse -Force -Confirm:$false
Remove-Item Execution.Benchmarks/obj -Recurse -Force -Confirm:$false
