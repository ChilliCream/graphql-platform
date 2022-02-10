using System.Diagnostics;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

public class CSharpGeneratorClient
{
    private readonly string _codeGenServer;

    public CSharpGeneratorClient(string codeGenServer)
    {
        _codeGenServer = codeGenServer;
    }

    public GeneratorResponse Execute(GeneratorRequest request)
    {
        var sink = RequestFormatter.Format(request);

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
            throw new Exception("Unable to generate client!");
        }

        childProcess.WaitForExit();

        if (childProcess.ExitCode != 0)
        {
            throw new Exception("An error happened while generating the code.");
        }

        return ResponseParser.Parse(sink);
    }
}
