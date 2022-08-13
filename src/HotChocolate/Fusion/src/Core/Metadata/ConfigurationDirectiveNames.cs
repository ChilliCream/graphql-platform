using static HotChocolate.Fusion.Metadata.ConfigurationDirectiveNames;

namespace HotChocolate.Fusion.Metadata;

internal static class ConfigurationDirectiveNames
{
    public const string VariableDirective = "variable";
    public const string FetchDirective = "fetch";
    public const string BindDirective = "bind";
    public const string HttpDirective = "httpClient";
    public const string FusionDirective = "fusion";
    public const string NameArg = "name";
    public const string SelectArg = "select";
    public const string TypeArg = "type";
    public const string FromArg = "from";
    public const string ToArg = "to";
    public const string AsArg = "as";
    public const string ArgumentArg = "argument";
    public const string BaseAddressArg = "baseAddress";
}
