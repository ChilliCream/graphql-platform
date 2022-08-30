using System;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

public static partial class CSharpGeneratorServer
{
    public static async Task<int> RunAsync(string fileSink)
    {
        try
        {
            return await ProcessAsync(fileSink);
        }
        catch(Exception ex)
        {
            await File.WriteAllTextAsync(
                GeneratorSink.ErrorLogFileName,
                ex.Message + Environment.NewLine + ex.StackTrace);
            return 1;
        }
    }

    private static async Task<int> ProcessAsync(string fileSink)
    {
        try
        {
            var request = RequestFormatter.Take(fileSink);
            var response = await GenerateAsync(request);
            ResponseFormatter.Format(response, fileSink);
            return 0;
        }
        catch (Exception ex)
        {
            ResponseFormatter.Format(
                new GeneratorResponse(
                    Array.Empty<GeneratorDocument>(),
                    new[]
                    {
                        new GeneratorError(
                            "SSG0004",
                            "Generator Error",
                            ex.Message + Environment.NewLine + ex.StackTrace)
                    }),
                fileSink);
            return 1;
        }
    }
}
