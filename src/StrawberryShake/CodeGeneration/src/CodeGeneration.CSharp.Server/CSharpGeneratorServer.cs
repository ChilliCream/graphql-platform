using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

public static partial class CSharpGeneratorServer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<int> RunAsync(string requestFile)
    {
        try
        {
            var buffer = await File.ReadAllBytesAsync(requestFile);
            var request = JsonSerializer.Deserialize<GeneratorRequest>(buffer, _options)!;
            File.Delete(requestFile);

            var response = await GenerateAsync(request);
            buffer = JsonSerializer.SerializeToUtf8Bytes(response, _options);
            await File.WriteAllBytesAsync(requestFile, buffer);
            return 0;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
