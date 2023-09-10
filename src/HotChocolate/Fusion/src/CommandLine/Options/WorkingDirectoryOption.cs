using System.CommandLine;

namespace HotChocolate.Fusion.CommandLine.Options;

internal sealed class WorkingDirectoryOption : Option<DirectoryInfo>
{
    public WorkingDirectoryOption() : base("--working-directory")
    {
        SetDefaultValueFactory(() => new DirectoryInfo(Environment.CurrentDirectory));
        AddAlias("-w");
    }
}
