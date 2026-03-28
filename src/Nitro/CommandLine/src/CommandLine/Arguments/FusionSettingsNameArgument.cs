namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionSettingsNameArgument : Argument<string>
{
    public const string CacheControlMergeBehavior = "cache-control-merge-behavior";
    public const string ExcludeByTag = "exclude-by-tag";
    public const string GlobalObjectIdentification = "global-object-identification";
    public const string TagMergeBehavior = "tag-merge-behavior";

    public static readonly string[] All =
    [
        CacheControlMergeBehavior,
        ExcludeByTag,
        GlobalObjectIdentification,
        TagMergeBehavior
    ];

    public FusionSettingsNameArgument() : base("SETTING_NAME")
    {
        this.FromAmong(All);
    }
}
