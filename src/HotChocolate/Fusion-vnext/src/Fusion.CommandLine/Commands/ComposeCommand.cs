using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.Properties.CommandLineResources;

namespace HotChocolate.Fusion.Commands;

internal sealed class ComposeCommand : Command
{
    public ComposeCommand() : base("compose")
    {
        Description = ComposeCommand_Description;

        var workingDirectoryOption = new Option<string>("--working-directory")
        {
            Description = ComposeCommand_WorkingDirectory_Description
        };
        workingDirectoryOption.AddAlias("-w");
        workingDirectoryOption.AddValidator(result =>
        {
            var workingDirectory = result.GetValueForOption(workingDirectoryOption);

            if (!Directory.Exists(workingDirectory))
            {
                result.ErrorMessage =
                    string.Format(
                        ComposeCommand_Error_WorkingDirectoryDoesNotExist,
                        workingDirectory);
            }
        });
        workingDirectoryOption.SetDefaultValueFactory(Directory.GetCurrentDirectory);
        workingDirectoryOption.LegalFilePathsOnly();

        var sourceSchemaFileOption = new Option<List<string>>("--source-schema-file")
        {
            Description = ComposeCommand_SourceSchemaFile_Description
        };
        sourceSchemaFileOption.AddAlias("-s");
        sourceSchemaFileOption.LegalFilePathsOnly();

        var compositeSchemaFileOption = new Option<string>("--composite-schema-file")
        {
            Description = ComposeCommand_CompositeSchemaFile_Description
        };
        compositeSchemaFileOption.AddAlias("-c");
        compositeSchemaFileOption.LegalFilePathsOnly();

        AddOption(workingDirectoryOption);
        AddOption(sourceSchemaFileOption);
        AddOption(compositeSchemaFileOption);

        this.SetHandler(async context =>
        {
            var workingDirectory = context.ParseResult.GetValueForOption(workingDirectoryOption)!;
            var sourceSchemaFiles = context.ParseResult.GetValueForOption(sourceSchemaFileOption)!;
            var compositeSchemaFile =
                context.ParseResult.GetValueForOption(compositeSchemaFileOption);

            context.ExitCode = await ExecuteAsync(
                context.Console,
                workingDirectory,
                sourceSchemaFiles,
                compositeSchemaFile,
                context.GetCancellationToken());
        });
    }

    private static async Task<int> ExecuteAsync(
        IConsole console,
        string workingDirectory,
        List<string> sourceSchemaFiles,
        string? compositeSchemaFile,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> sourceSchemas;

        try
        {
            sourceSchemas = await ReadSourceSchemasAsync(
                sourceSchemaFiles,
                workingDirectory,
                cancellationToken);
        }
        catch (Exception e)
        {
            console.Error.WriteLine(e.Message);

            return 1;
        }

        var compositionLog = new CompositionLog();
        var schemaComposer = new SchemaComposer(sourceSchemas, compositionLog);

        var result = schemaComposer.Compose();

        WriteCompositionLog(
            compositionLog,
            writer: result.IsSuccess ? console.Out : console.Error,
            writeAsGraphQLComments: result.IsSuccess && compositeSchemaFile is null);

        if (result.IsFailure)
        {
            foreach (var error in result.Errors)
            {
                console.Error.WriteLine(error.Message);
            }

            return 1;
        }

        // If a composite schema file was not specified, print the result to the console.
        if (compositeSchemaFile is null)
        {
            console.Out.WriteLine(result.Value.ToString());
        }
        else
        {
            var compositeSchemaPath = Path.Combine(workingDirectory, compositeSchemaFile);
            var directoryPath = Path.GetDirectoryName(compositeSchemaPath);

            if (directoryPath is not null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await File.WriteAllTextAsync(
                compositeSchemaPath,
                result.Value + Environment.NewLine,
                cancellationToken);

            console.Out.WriteLine(
                string.Format(ComposeCommand_CompositeSchemaFile_Written, compositeSchemaPath));
        }

        return 0;
    }

    private static void WriteCompositionLog(
        CompositionLog compositionLog,
        IStandardStreamWriter writer,
        bool writeAsGraphQLComments)
    {
        Console.OutputEncoding = Encoding.UTF8;

        foreach (var entry in compositionLog)
        {
            var emoji = entry.Severity switch
            {
                LogSeverity.Error => "❌",
                LogSeverity.Info => "ℹ️",
                LogSeverity.Warning => "⚠️",
                _ => throw new InvalidOperationException()
            };

            var abbreviatedSeverity = entry.Severity switch
            {
                LogSeverity.Error => ComposeCommand_AbbreviatedSeverity_Error,
                LogSeverity.Info => ComposeCommand_AbbreviatedSeverity_Info,
                LogSeverity.Warning => ComposeCommand_AbbreviatedSeverity_Warning,
                _ => throw new InvalidOperationException()
            };

            var message = $"{emoji} [{abbreviatedSeverity}] {entry.Message} ({entry.Code})";

            if (writeAsGraphQLComments)
            {
                message = $"# {message}";
            }

            writer.WriteLine(message);
        }

        if (!compositionLog.IsEmpty)
        {
            writer.WriteLine();
        }
    }

    private static async Task<IEnumerable<string>> ReadSourceSchemasAsync(
        List<string> sourceSchemaFiles,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        ImmutableSortedSet<string> sourceSchemaFilePaths;

        // If no source schema files were specified, scan the working directory for *.graphqls
        // files.
        if (sourceSchemaFiles.Count == 0)
        {
            sourceSchemaFilePaths =
                new DirectoryInfo(workingDirectory)
                    .GetFiles("*.graphqls")
                    .Select(i => i.FullName)
                    .ToImmutableSortedSet();

            if (sourceSchemaFilePaths.Count == 0)
            {
                throw new Exception(ComposeCommand_Error_NoSourceSchemaFilesFound);
            }
        }
        else
        {
            sourceSchemaFilePaths
                = sourceSchemaFiles.Select(f => Path.Combine(workingDirectory, f))
                    .ToImmutableSortedSet();
        }

        foreach (var sourceSchemaFilePath in sourceSchemaFilePaths)
        {
            if (!File.Exists(sourceSchemaFilePath))
            {
                throw new Exception(
                    string.Format(
                        ComposeCommand_Error_SourceSchemaFileDoesNotExist,
                        sourceSchemaFilePath));
            }
        }

        return await Task.WhenAll(
            sourceSchemaFilePaths.Select(
                async f => await File.ReadAllTextAsync(f, cancellationToken)));
    }
}
