using System.Diagnostics;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

public class CSharpGeneratorClient
{
    private readonly string _codeGenServer;

    public CSharpGeneratorClient(string codeGenServer)
    {
        _codeGenServer = codeGenServer;
    }

    public GeneratorResponse Execute(GeneratorRequest request, Action<string?>? log = null)
    {
        var sink = RequestFormatter.Format(request);

        var childProcess = Process.Start(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_codeGenServer}\" \"{sink}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = log is not null
            });

        if (childProcess is null)
        {
            throw new Exception("Unable to generate client!");
        }

        if (log is not null)
        {
            childProcess.OutputDataReceived += (_, args) => log(args.Data);
            childProcess.BeginOutputReadLine();
        }

        childProcess.WaitForExit();

        if (childProcess.ExitCode != 0)
        {
            throw new Exception("An error happened while generating the code.");
        }

        return ResponseParser.Parse(sink);
    }
}
