using System.Diagnostics;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

internal sealed class CSharpGeneratorClient
{
    private readonly string _codeGenServer;

    public CSharpGeneratorClient(string codeGenServer)
    {
        _codeGenServer = codeGenServer;
    }

    public GeneratorResponse Execute(GeneratorRequest request)
    {
        try
        {
            var fileSink = RequestFormatter.Format(request);

            var childProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{_codeGenServer}\" \"{fileSink}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

            if (childProcess is null)
            {
                return CreateErrorResponse("Unable to generate client!");
            }

            childProcess.WaitForExit();

            if (childProcess.ExitCode != 0)
            {
                return CreateErrorResponse("An error happened while generating the code.");
            }

            return ResponseFormatter.Take(fileSink);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex);
        }
    }

    private static GeneratorResponse CreateErrorResponse(Exception exception)
        => CreateErrorResponse(exception.Message + Environment.NewLine + exception.StackTrace);

    private static GeneratorResponse CreateErrorResponse(string message)
        => new GeneratorResponse(
            Array.Empty<GeneratorDocument>(),
            new[] { new GeneratorError("SSG0005", "Generator Error", message) });
}
