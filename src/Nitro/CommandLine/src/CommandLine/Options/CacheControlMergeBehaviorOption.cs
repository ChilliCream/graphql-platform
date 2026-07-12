using HotChocolate.Fusion.Options;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class CacheControlMergeBehaviorOption : Option<DirectiveMergeBehavior?>
{
    public const string OptionName = "--cache-control-merge-behavior";

    public CacheControlMergeBehaviorOption()
        : base(OptionName)
    {
        Description = "Choose how @cacheControl directives are merged";
        AcceptOnlyFromAmong(DirectiveMergeBehaviorParser.Values);
        CustomParser = result => DirectiveMergeBehaviorParser.Parse(result.Tokens.Single().Value);
    }
}
