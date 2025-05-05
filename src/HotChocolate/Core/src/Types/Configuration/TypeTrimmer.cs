using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class TypeTrimmer
{
    private readonly HashSet<ITypeSystemMember> _touched = [];
    private readonly List<IObjectTypeDefinition> _rootTypes = [];
    private readonly List<ITypeSystemMember> _discoveredTypes;

    public TypeTrimmer(IEnumerable<ITypeSystemMember> discoveredTypes)
    {
        ArgumentNullException.ThrowIfNull(discoveredTypes);
        _discoveredTypes = [.. discoveredTypes];
    }

    public void AddOperationType(IObjectTypeDefinition? operationType)
    {
        if (operationType is not null)
        {
            _rootTypes.Add(operationType);
        }
    }

    public IReadOnlyCollection<ITypeSystemMember> Trim()
    {
        foreach (var type in _discoveredTypes)
        {
            if (type is not DirectiveType directiveDef)
            {
                continue;
            }

            if (directiveDef.IsExecutableDirective
                || directiveDef.Name.EqualsOrdinal(WellKnownDirectives.Deprecated)
                || directiveDef.Name.EqualsOrdinal(SpecifiedByDirectiveType.Names.SpecifiedBy))
            {
                _touched.Add(directiveDef);
                VisitDirective(directiveDef);
            }
        }

        foreach (var rootType in _rootTypes)
        {
            VisitRoot(rootType);
        }

        return _touched;
    }

    private void VisitRoot(IObjectTypeDefinition rootType)
    {
        Visit(rootType);
    }

    private void Visit(ITypeSystemMember type)
    {
        if (_touched.Add(type))
        {
            switch (type)
            {
                case IScalarTypeDefinition s:
                    VisitScalar(s);
                    break;

                case IEnumTypeDefinition e:
                    VisitEnum(e);
                    break;

                case IObjectTypeDefinition o:
                    VisitObject(o);
                    break;

                case IUnionTypeDefinition u:
                    VisitUnion(u);
                    break;

                case IInterfaceTypeDefinition i:
                    VisitInterface(i);
                    break;

                case IDirectiveDefinition d:
                    VisitDirective(d);
                    break;

                case IInputObjectTypeDefinition i:
                    VisitInput(i);
                    break;
            }
        }
    }

    private void VisitScalar(IScalarTypeDefinition type)
    {
        VisitDirectives(type);
    }

    private void VisitEnum(IEnumTypeDefinition type)
    {
        VisitDirectives(type);

        foreach (var value in type.Values)
        {
            VisitDirectives(value);
        }
    }

    private void VisitObject(IObjectTypeDefinition type)
    {
        VisitDirectives(type);

        foreach (var interfaceType in type.Implements)
        {
            Visit(interfaceType);
        }

        foreach (var field in type.Fields)
        {
            VisitDirectives(field);
            Visit(field.Type.NamedType());

            foreach (var argument in field.Arguments)
            {
                VisitDirectives(argument);
                Visit(argument.Type.NamedType());
            }
        }
    }

    private void VisitUnion(IUnionTypeDefinition type)
    {
        VisitDirectives(type);

        foreach (var objectType in type.Types)
        {
            Visit(objectType);
        }
    }

    private void VisitInterface(IInterfaceTypeDefinition type)
    {
        VisitDirectives(type);

        foreach (var field in type.Fields)
        {
            VisitDirectives(field);
            Visit(field.Type.NamedType());

            foreach (var argument in field.Arguments)
            {
                VisitDirectives(argument);
                Visit(argument.Type.NamedType());
            }
        }

        foreach (var complexType in _discoveredTypes.OfType<IComplexTypeDefinition>())
        {
            if (complexType.IsImplementing(type))
            {
                Visit(complexType);
            }
        }
    }

    private void VisitInput(IInputObjectTypeDefinition type)
    {
        VisitDirectives(type);

        foreach (var field in type.Fields)
        {
            VisitDirectives(field);
            Visit(field.Type.NamedType());
        }
    }

    private void VisitDirective(IDirectiveDefinition directive)
    {
        foreach (var argument in directive.Arguments)
        {
            VisitDirectives(argument);
            Visit(argument.Type.NamedType());
        }
    }

    private void VisitDirectives(IDirectivesProvider directivesProvider)
    {
        foreach (var directive in directivesProvider.Directives)
        {
            Visit(directive.Definition);
        }
    }
}
