using HotChocolate.Fusion.Options;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class TagMergeBehaviorOption : Option<DirectiveMergeBehavior?>
{
    public const string OptionName = "--tag-merge-behavior";

    public TagMergeBehaviorOption()
        : base(OptionName)
    {
        Description = "Choose how @tag directives are merged";
        AcceptOnlyFromAmong(DirectiveMergeBehaviorParser.Values);
        CustomParser = result => DirectiveMergeBehaviorParser.Parse(result.Tokens.Single().Value);
    }
}
