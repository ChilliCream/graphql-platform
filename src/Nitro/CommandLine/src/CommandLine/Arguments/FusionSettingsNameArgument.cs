namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionSettingsNameArgument : Argument<string>
{
    public const string ArgumentName = "SETTING_NAME";
    public const string CacheControlMergeBehavior = "cache-control-merge-behavior";
    public const string ExcludeByTag = "exclude-by-tag";
    public const string GlobalObjectIdentification = "global-object-identification";
    public const string NodeResolution = "node-resolution";
    public const string ShareableFieldRuntimeTypeRouting = "shareable-field-runtime-type-routing";
    public const string TagMergeBehavior = "tag-merge-behavior";

    public static readonly string[] All =
    [
        CacheControlMergeBehavior,
        ExcludeByTag,
        GlobalObjectIdentification,
        NodeResolution,
        ShareableFieldRuntimeTypeRouting,
        TagMergeBehavior
    ];

    public FusionSettingsNameArgument() : base(ArgumentName)
    {
        Description = "The name of the setting to change";
        this.AcceptOnlyFromAmong(All);
    }
}
