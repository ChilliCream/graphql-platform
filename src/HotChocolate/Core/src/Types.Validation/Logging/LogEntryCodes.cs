namespace HotChocolate.Logging;

/// <summary>
/// The log entry codes for schema validation.
/// </summary>
internal static class LogEntryCodes
{
    public const string EmptyObjectType = "HCV0001";
    public const string InvalidMemberName = "HCV0002";
    public const string InvalidArgumentDeprecation = "HCV0003";
    public const string InvalidInputFieldDeprecation = "HCV0004";
    public const string NotTransitivelyImplemented = "HCV0005";
    public const string FieldNotImplemented = "HCV0006";
    public const string ArgumentNotImplemented = "HCV0007";
    public const string InvalidArgumentType = "HCV0008";
    public const string AdditionalArgumentNotNullable = "HCV0009";
    public const string InvalidFieldType = "HCV0010";
    public const string InvalidFieldDeprecation = "HCV0011";
    public const string EmptyInterfaceType = "HCV0012";
    public const string SelfImplementation = "HCV0013";
    public const string EmptyUnionType = "HCV0014";
    public const string EmptyEnumType = "HCV0015";
    public const string EmptyInputObjectType = "HCV0016";
    public const string InvalidOneOfField = "HCV0017";
    public const string InputObjectCycle = "HCV0018";
    public const string InputObjectDefaultValueCycle = "HCV0019";
    public const string DirectiveDefinitionMissingLocation = "HCV0020";
    public const string UndefinedFieldType = "HCV0021";
    public const string UndefinedArgumentType = "HCV0022";
    public const string UndefinedArgumentDefaultEnumValue = "HCV0023";
    public const string UndefinedInputFieldDefaultEnumValue = "HCV0024";
    public const string UndefinedArgumentAssignedEnumValue = "HCV0025";
    public const string UndefinedDirective = "HCV0026";
}
