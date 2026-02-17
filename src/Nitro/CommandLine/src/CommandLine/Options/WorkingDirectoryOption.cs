namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class WorkingDirectoryOption : Option<string>
{
    public WorkingDirectoryOption() : base("--working-directory")
    {
        Description = CommandLineResources.ComposeCommand_WorkingDirectory_Description;
        AddAlias("-w");
        AddValidator(result =>
        {
            var workingDirectory = result.GetValueForOption(this);

            if (!Directory.Exists(workingDirectory))
            {
                result.ErrorMessage =
                    string.Format(
                        CommandLineResources.ComposeCommand_Error_WorkingDirectoryDoesNotExist,
                        workingDirectory);
            }
        });
        SetDefaultValueFactory(Directory.GetCurrentDirectory);
        this.LegalFilePathsOnly();
    }
}
