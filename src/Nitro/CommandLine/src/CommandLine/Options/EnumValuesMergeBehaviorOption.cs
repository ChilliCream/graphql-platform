using HotChocolate.Fusion.Options;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class EnumValuesMergeBehaviorOption : Option<EnumValuesMergeBehavior?>
{
    public const string OptionName = "--enum-values-merge-behavior";

    public EnumValuesMergeBehaviorOption()
        : base(OptionName)
    {
        Description = "Choose how enum values are merged across source schemas";
        AcceptOnlyFromAmong(EnumValuesMergeBehaviorParser.Values);
        CustomParser = result => EnumValuesMergeBehaviorParser.Parse(result.Tokens.Single().Value);
    }
}
