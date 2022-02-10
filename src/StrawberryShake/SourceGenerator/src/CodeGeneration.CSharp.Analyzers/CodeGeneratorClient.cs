using System.Diagnostics;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

public class CodeGeneratorClient
{
    private readonly string _codeGenServer;

    public CodeGeneratorClient(string codeGenServer)
    {
        _codeGenServer = codeGenServer;
    }

    public GeneratorResponse Execute(GeneratorRequest request)
    {
        var watch = Stopwatch.StartNew();
        DebugLog.Log("Process->format");
        var sink = RequestFormatter.Format(request);
        DebugLog.Log($"dotnet \"{_codeGenServer}\" \"{sink}\"");

        var childProcess = Process.Start(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_codeGenServer}\" \"{sink}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

        if (childProcess is null)
        {
            DebugLog.Log("Process->none");
            throw new Exception("Unable to generate client!");
        }

        DebugLog.Log("Process->waitForExit");
        childProcess.WaitForExit();

        if (childProcess.ExitCode != 0)
        {
            DebugLog.Log("Process->error");
            throw new Exception("An error happened while generating the code.");
        }

        DebugLog.Log("Process->parse");
        var response = ResponseParser.Parse(sink);
        DebugLog.Log($"Process->{watch.Elapsed}");
        return response;
    }
}
