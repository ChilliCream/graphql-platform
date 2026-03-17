using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class AuthorizeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public AuthorizeMutableDirectiveDefinition(
        MutableScalarTypeDefinition stringType,
        MutableEnumTypeDefinition applyPolicyType)
        : base(WellKnownDirectiveNames.Authorize)
    {
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.Policy, stringType));
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Roles,
                new ListType(new NonNullType(stringType))));
        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.Apply, new NonNullType(applyPolicyType))
            {
                DefaultValue = new StringValueNode("BEFORE_RESOLVER")
            });
        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object;
    }

    public static AuthorizeMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
                SpecScalarNames.String.Name,
                out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        if (!schema.Types.TryGetType<MutableEnumTypeDefinition>(
            WellKnownTypeNames.ApplyPolicy,
            out var applyPolicyType))
        {
            applyPolicyType = ApplyPolicyMutableEnumTypeDefinition.Create();
        }

        return new AuthorizeMutableDirectiveDefinition(stringType, applyPolicyType);
    }
}
