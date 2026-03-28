namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class WorkingDirectoryOption : Option<string>
{
    public WorkingDirectoryOption() : base("--working-directory")
    {
        Description = CommandLineResources.ComposeCommand_WorkingDirectory_Description;
        Aliases.Add("-w");
        Validators.Add(result =>
        {
            var workingDirectory = result.GetValue(this);

            if (!Directory.Exists(workingDirectory))
            {
                result.AddError(
                    string.Format(
                        CommandLineResources.ComposeCommand_Error_WorkingDirectoryDoesNotExist,
                        workingDirectory));
            }
        });
        DefaultValueFactory = _ => Directory.GetCurrentDirectory();
        this.LegalFilePathsOnly();
    }
}
