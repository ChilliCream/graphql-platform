#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

internal sealed class DirectiveTypeInterceptor : TypeInterceptor
{
    private readonly HashSet<DirectiveType> _usedDirectives = [];

    public override void OnAfterCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        base.OnAfterCompleteMetadata(context, configuration);

        if (!((RegisteredType)context).HasErrors)
        {
            InspectType(context.Type);
        }
    }

    internal override void OnBeforeRegisterSchemaTypes(
        IDescriptorContext context,
        SchemaTypesConfiguration configuration)
    {
        List<DirectiveType>? discarded = null;

        foreach (var directiveType in configuration.DirectiveTypes!)
        {
            if (directiveType is { IsTypeSystemDirective: true, IsExecutableDirective: false } &&
                !directiveType.Name.EqualsOrdinal(WellKnownDirectives.Deprecated) &&
                !directiveType.Name.EqualsOrdinal(SpecifiedByDirectiveType.Names.SpecifiedBy) &&
                !_usedDirectives.Contains(directiveType))
            {
                (discarded ??= []).Add(directiveType);
            }
        }

        if (discarded is not null)
        {
            configuration.DirectiveTypes =
                configuration.DirectiveTypes!.Except(discarded).ToArray();
        }
    }

    private void InspectType(ITypeSystemObject obj)
    {
        switch (obj)
        {
            case IComplexOutputType objectType:
                RegisterDirectiveUsage(objectType);

                foreach (var field in objectType.Fields)
                {
                    RegisterDirectiveUsage(field);

                    if (field.Arguments.Count <= 0)
                    {
                        continue;
                    }

                    foreach (var argument in field.Arguments)
                    {
                        RegisterDirectiveUsage(argument);
                    }
                }
                break;

            case UnionType unionType:
                RegisterDirectiveUsage(unionType);
                break;

            case InputObjectType inputObjectType:
                RegisterDirectiveUsage(inputObjectType);

                foreach (var field in inputObjectType.Fields)
                {
                    RegisterDirectiveUsage(field);
                }
                break;

            case EnumType enumType:
                RegisterDirectiveUsage(enumType);

                foreach (var value in enumType.Values)
                {
                    RegisterDirectiveUsage(value);
                }

                break;

            case ScalarType scalarType:
                RegisterDirectiveUsage(scalarType);
                break;

            case DirectiveType directiveType:
                foreach (var argument in directiveType.Arguments)
                {
                    RegisterDirectiveUsage(argument);
                }
                break;

            case Schema schema:
                RegisterDirectiveUsage(schema);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(obj));
        }
    }

    private void RegisterDirectiveUsage(IHasDirectives member)
    {
        if (member.Directives.Count == 0)
        {
            return;
        }

        foreach (var directive in member.Directives)
        {
            _usedDirectives.Add(directive.Type);
        }
    }
}
