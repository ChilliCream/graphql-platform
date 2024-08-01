using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

internal sealed class FromJsonSchemaDirective : ISchemaDirective
{
    public string Name => "fromJson";

    public void ApplyConfiguration(
        IDescriptorContext context,
        DirectiveNode directiveNode,
        IDefinition definition,
        Stack<IDefinition> path)
    {
        if (definition is ObjectFieldDefinition fieldDef)
        {
            fieldDef.Configurations.Add(
                new CompleteConfiguration<ObjectFieldDefinition>(
                    (ctx, def) =>
                    {
                        var propertyName = GetPropertyName(directiveNode);
                        propertyName ??= def.Name;
                        var type = ctx.GetType<IType>(def.Type!);
                        var namedType = type.NamedType();

                        if (type.IsListType())
                        {
                            JsonObjectTypeExtensions.InferListResolver(def);
                            return;
                        }

                        if (namedType is ScalarType scalarType)
                        {
                            JsonObjectTypeExtensions.InferResolver(
                                ctx.Type, def, scalarType, propertyName);
                            return;
                        }

                        throw ThrowHelper.CannotInferTypeFromJsonObj(ctx.Type.Name);
                    },
                    fieldDef,
                    ApplyConfigurationOn.BeforeCompletion));
        }
    }

    private static string? GetPropertyName(DirectiveNode directive)
    {
        if (directive.Arguments.Count == 0)
        {
            return null;
        }

        if (directive.Arguments.Count == 1)
        {
            var argument = directive.Arguments[0];
            if (argument.Name.Value.EqualsOrdinal("name") &&
                argument.Value is StringValueNode { Value: { Length: > 0, } name, })
            {
                return name;
            }
        }

        return null;
    }
}
