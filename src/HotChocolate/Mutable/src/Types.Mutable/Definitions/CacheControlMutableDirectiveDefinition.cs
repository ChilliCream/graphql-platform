namespace HotChocolate.Types.Mutable.Definitions;

public sealed class CacheControlMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public CacheControlMutableDirectiveDefinition(
        MutableScalarTypeDefinition intType,
        MutableScalarTypeDefinition booleanType,
        CacheControlScopeMutableEnumTypeDefinition cacheControlScopeType,
        MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.CacheControl)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.MaxAge, intType));
        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.SharedMaxAge, intType));
        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.InheritMaxAge, booleanType));
        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.Scope, cacheControlScopeType));
        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.Vary, new ListType(stringType)));
        Locations =
            DirectiveLocation.Object
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.Interface
            | DirectiveLocation.Union;
    }

    public static CacheControlMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(BuiltIns.Int.Name, out var intType))
        {
            intType = BuiltIns.Int.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(BuiltIns.String.Name, out var booleanType))
        {
            booleanType = BuiltIns.Boolean.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(BuiltIns.String.Name, out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new CacheControlMutableDirectiveDefinition(
            intType,
            booleanType,
            CacheControlScopeMutableEnumTypeDefinition.Create(),
            stringType);
    }
}
