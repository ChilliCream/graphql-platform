namespace HotChocolate.Types.Mutable.Definitions;

public sealed class CacheControlMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public CacheControlMutableDirectiveDefinition(
        MutableScalarTypeDefinition intType,
        MutableScalarTypeDefinition booleanType,
        CacheControlScopeMutableEnumTypeDefinition cacheControlScopeType,
        MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.CacheControl.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(DirectiveNames.CacheControl.Arguments.MaxAge, intType));
        Arguments.Add(
            new MutableInputFieldDefinition(DirectiveNames.CacheControl.Arguments.SharedMaxAge, intType));
        Arguments.Add(
            new MutableInputFieldDefinition(DirectiveNames.CacheControl.Arguments.InheritMaxAge, booleanType));
        Arguments.Add(
            new MutableInputFieldDefinition(DirectiveNames.CacheControl.Arguments.Scope, cacheControlScopeType));
        Arguments.Add(
            new MutableInputFieldDefinition(DirectiveNames.CacheControl.Arguments.Vary, new ListType(stringType)));
        Locations =
            DirectiveLocation.Object
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.Interface
            | DirectiveLocation.Union;
    }

    public static CacheControlMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Int.Name, out var intType))
        {
            intType = BuiltIns.Int.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Boolean.Name, out var booleanType))
        {
            booleanType = BuiltIns.Boolean.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.String.Name, out var stringType))
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
