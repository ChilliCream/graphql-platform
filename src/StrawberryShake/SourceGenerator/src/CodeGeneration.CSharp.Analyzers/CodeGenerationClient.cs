using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

public sealed class CodeGenerationClient : IDisposable
{
    private readonly JsonRpc _connection;
    private bool _disposed;

    public Task<ServerResponse> SetConfigurationAsync(string configFileName)
        => _connection.InvokeAsync<ServerResponse>(
            "generator/SetConfiguration",
            configFileName);

    public Task<ServerResponse> SetDocumentsAsync(string[] documentFileNames)
        => _connection.InvokeAsync<ServerResponse>(
            "generator/SetDocuments",
            new object[] { documentFileNames });

    public Task<GeneratorResponse> GenerateAsync()
        => _connection.InvokeAsync<GeneratorResponse>("generator/Generate");

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }

    public static CodeGenerationClient Connect(string generatorLocation)
    {
        var childProcess = Process.Start(new ProcessStartInfo("childprocess.exe")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        });

        var jsonRpc = JsonRpc.Attach(childProcess.StandardInput.BaseStream, childProcess.StandardOutput.BaseStream);

        throw new NotImplementedException();
    }
}
