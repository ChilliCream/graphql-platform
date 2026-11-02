namespace HotChocolate.Serialization;

internal sealed class GraphQLSpecVersionProfile
{
    private static readonly HashSet<string> s_executableDirectiveLocations =
    [
        "QUERY",
        "MUTATION",
        "SUBSCRIPTION",
        "FIELD",
        "FRAGMENT_DEFINITION",
        "FRAGMENT_SPREAD",
        "INLINE_FRAGMENT",
        "VARIABLE_DEFINITION"
    ];

    private static readonly HashSet<string> s_typeSystemDirectiveLocations =
    [
        "SCHEMA",
        "SCALAR",
        "OBJECT",
        "FIELD_DEFINITION",
        "ARGUMENT_DEFINITION",
        "INTERFACE",
        "UNION",
        "ENUM",
        "ENUM_VALUE",
        "INPUT_OBJECT",
        "INPUT_FIELD_DEFINITION"
    ];

    private static readonly HashSet<string> s_skipAndIncludeLocations =
    ["FIELD", "FRAGMENT_SPREAD", "INLINE_FRAGMENT"];

    private static readonly HashSet<string> s_october2021DeprecatedLocations =
    ["FIELD_DEFINITION", "ENUM_VALUE"];

    private static readonly HashSet<string> s_september2025DeprecatedLocations =
    ["FIELD_DEFINITION", "ENUM_VALUE", "ARGUMENT_DEFINITION", "INPUT_FIELD_DEFINITION"];

    private static readonly HashSet<string> s_specifiedByLocations = ["SCALAR"];
    private static readonly HashSet<string> s_oneOfLocations = ["INPUT_OBJECT"];

    private static readonly GraphQLSpecVersionProfile s_october2021 = new(
        s_typeSystemDirectiveLocations,
        s_executableDirectiveLocations,
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["skip"] = s_skipAndIncludeLocations,
            ["include"] = s_skipAndIncludeLocations,
            ["deprecated"] = s_october2021DeprecatedLocations,
            ["specifiedBy"] = s_specifiedByLocations
        });

    private static readonly GraphQLSpecVersionProfile s_september2025 = new(
        s_typeSystemDirectiveLocations,
        s_executableDirectiveLocations,
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["skip"] = s_skipAndIncludeLocations,
            ["include"] = s_skipAndIncludeLocations,
            ["deprecated"] = s_september2025DeprecatedLocations,
            ["specifiedBy"] = s_specifiedByLocations,
            ["oneOf"] = s_oneOfLocations
        });

    private readonly HashSet<string> _directiveLocations;

    private GraphQLSpecVersionProfile(
        IReadOnlySet<string> typeSystemDirectiveLocations,
        IReadOnlySet<string> executableDirectiveLocations,
        IReadOnlyDictionary<string, IReadOnlySet<string>> specDirectiveLocations)
    {
        TypeSystemDirectiveLocations = typeSystemDirectiveLocations;
        ExecutableDirectiveLocations = executableDirectiveLocations;
        SpecDirectiveLocations = specDirectiveLocations;
        _directiveLocations = new HashSet<string>(typeSystemDirectiveLocations, StringComparer.Ordinal);
        _directiveLocations.UnionWith(executableDirectiveLocations);
    }

    public IReadOnlySet<string> TypeSystemDirectiveLocations { get; }

    public IReadOnlySet<string> ExecutableDirectiveLocations { get; }

    public IReadOnlyDictionary<string, IReadOnlySet<string>> SpecDirectiveLocations { get; }

    public bool IsDirectiveLocationAllowed(string location)
        => _directiveLocations.Contains(location);

    public static GraphQLSpecVersionProfile For(GraphQLSpecVersion version)
        => version switch
        {
            GraphQLSpecVersion.October2021 => s_october2021,
            GraphQLSpecVersion.September2025 => s_september2025,
            _ => throw ThrowHelper.GraphQLSpecVersions_UnknownVersion(version)
        };
}
