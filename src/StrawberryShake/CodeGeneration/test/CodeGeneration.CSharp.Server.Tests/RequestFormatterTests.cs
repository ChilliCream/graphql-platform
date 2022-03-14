using System.Collections.Generic;
using System.IO;
using Microsoft.VisualBasic;
using Xunit;
using static Xunit.Assert;

namespace StrawberryShake.CodeGeneration.CSharp;

public class RequestFormatterTests
{
    [Fact]
    public void Format_And_Parse_Minimal()
    {
        string? fileName = null;

        try
        {
            // arrange
            var configFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var documentFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // act
            var request = new GeneratorRequest(configFileName, new List<string> {documentFileName});
            fileName = RequestFormatter.Format(request);
            GeneratorRequest parsedRequest = RequestFormatter.Parse(fileName);

            // assert
            Equal(request.ConfigFileName, parsedRequest.ConfigFileName);
            Equal(request.RootDirectory, parsedRequest.RootDirectory);
            Equal(request.DocumentFileNames, parsedRequest.DocumentFileNames);
            Equal(request.DefaultNamespace, parsedRequest.DefaultNamespace);
            Equal(request.PersistedQueryDirectory, parsedRequest.PersistedQueryDirectory);
            Equal(request.Option, parsedRequest.Option);
        }
        finally
        {
            if (fileName is not null && File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Format_And_Take_Minimal()
    {
        string? fileName = null;

        try
        {
            // arrange
            var configFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var documentFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // act
            var request = new GeneratorRequest(configFileName, new List<string> {documentFileName});
            fileName = RequestFormatter.Format(request);
            GeneratorRequest parsedRequest = RequestFormatter.Take(fileName);

            // assert
            Equal(request.ConfigFileName, parsedRequest.ConfigFileName);
            Equal(request.RootDirectory, parsedRequest.RootDirectory);
            Equal(request.DocumentFileNames, parsedRequest.DocumentFileNames);
            Equal(request.DefaultNamespace, parsedRequest.DefaultNamespace);
            Equal(request.PersistedQueryDirectory, parsedRequest.PersistedQueryDirectory);
            Equal(request.Option, parsedRequest.Option);
            False(File.Exists(fileName));
        }
        finally
        {
            if (fileName is not null && File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Format_And_Parse_All()
    {
        string? fileName = null;

        try
        {
            // arrange
            var configFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var documentFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // act
            var request = new GeneratorRequest(
                configFileName,
                new List<string> {documentFileName},
                Path.Combine(Path.GetTempPath(), "Root"),
                "Foo.Bar",
                Path.Combine(Path.GetTempPath(), "Queries"),
                RequestOptions.GenerateRazorComponent);
            fileName = RequestFormatter.Format(request);
            GeneratorRequest parsedRequest = RequestFormatter.Parse(fileName);

            // assert
            Equal(request.ConfigFileName, parsedRequest.ConfigFileName);
            Equal(request.RootDirectory, parsedRequest.RootDirectory);
            Equal(request.DocumentFileNames, parsedRequest.DocumentFileNames);
            Equal(request.DefaultNamespace, parsedRequest.DefaultNamespace);
            Equal(request.PersistedQueryDirectory, parsedRequest.PersistedQueryDirectory);
            Equal(request.Option, parsedRequest.Option);
        }
        finally
        {
            if (fileName is not null && File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
