namespace ChilliCream.Nitro.CommandLine;

internal sealed class WorkingDirectoryOption : Option<string>
{
    public WorkingDirectoryOption() : base("--working-directory")
    {
        Description = "Set the working directory for the command";
        Aliases.Add("-w");
        Validators.Add(result =>
        {
            var workingDirectory = result.GetValue(this);

            if (workingDirectory is not null && !Directory.Exists(workingDirectory))
            {
                result.AddError(
                    string.Format(
                        "❌ Working directory '{0}' does not exist.",
                        workingDirectory));
            }
        });
        this.LegalFilePathsOnly();
    }
}
