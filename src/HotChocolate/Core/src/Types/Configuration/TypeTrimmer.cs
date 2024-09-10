using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class TypeTrimmer
{
    private readonly HashSet<TypeSystemObjectBase> _touched = [];
    private readonly List<ObjectType> _rootTypes = [];
    private readonly List<TypeSystemObjectBase> _discoveredTypes;

    public TypeTrimmer(IEnumerable<TypeSystemObjectBase> discoveredTypes)
    {
        if (discoveredTypes is null)
        {
            throw new ArgumentNullException(nameof(discoveredTypes));
        }

        _discoveredTypes = discoveredTypes.ToList();
    }

    public void AddOperationType(ObjectType? operationType)
    {
        if (operationType is not null)
        {
            _rootTypes.Add(operationType);
        }
    }

    public IReadOnlyCollection<TypeSystemObjectBase> Trim()
    {
        foreach (var directiveType in _discoveredTypes.OfType<DirectiveType>())
        {
            if (directiveType.IsExecutableDirective ||
                directiveType.Name.EqualsOrdinal(WellKnownDirectives.Deprecated) ||
                directiveType.Name.EqualsOrdinal(SpecifiedByDirectiveType.Names.SpecifiedBy))
            {
                _touched.Add(directiveType);
                VisitDirective(directiveType);
            }
        }

        foreach (var rootType in _rootTypes)
        {
            VisitRoot(rootType);
        }

        return _touched;
    }

    private void VisitRoot(ObjectType rootType)
    {
        Visit(rootType);
    }

    private void Visit(TypeSystemObjectBase type)
    {
        if (_touched.Add(type))
        {
            switch (type)
            {
                case ScalarType s:
                    VisitScalar(s);
                    break;

                case EnumType e:
                    VisitEnum(e);
                    break;

                case ObjectType o:
                    VisitObject(o);
                    break;

                case UnionType u:
                    VisitUnion(u);
                    break;

                case InterfaceType i:
                    VisitInterface(i);
                    break;

                case DirectiveType d:
                    VisitDirective(d);
                    break;

                case InputObjectType i:
                    VisitInput(i);
                    break;
            }
        }
    }

    private void VisitScalar(ScalarType type)
    {
        VisitDirectives(type);
    }

    private void VisitEnum(EnumType type)
    {
        VisitDirectives(type);

        foreach (var value in type.Values)
        {
            VisitDirectives(value);
        }
    }

    private void VisitObject(ObjectType type)
    {
        VisitDirectives(type);

        foreach (var interfaceType in type.Implements)
        {
            Visit(interfaceType);
        }

        foreach (var field in type.Fields)
        {
            VisitDirectives(field);
            Visit((TypeSystemObjectBase)field.Type.NamedType());

            foreach (var argument in field.Arguments)
            {
                VisitDirectives(argument);
                Visit((TypeSystemObjectBase)argument.Type.NamedType());
            }
        }
    }

    private void VisitUnion(UnionType type)
    {
        VisitDirectives(type);

        foreach (var objectType in type.Types.Values)
        {
            Visit(objectType);
        }
    }

    private void VisitInterface(InterfaceType type)
    {
        VisitDirectives(type);

        foreach (var field in type.Fields)
        {
            VisitDirectives(field);
            Visit((TypeSystemObjectBase)field.Type.NamedType());

            foreach (var argument in field.Arguments)
            {
                VisitDirectives(argument);
                Visit((TypeSystemObjectBase)argument.Type.NamedType());
            }
        }

        foreach (var complexType in
            _discoveredTypes.OfType<IComplexOutputType>())
        {
            if (complexType.IsImplementing(type))
            {
                Visit((TypeSystemObjectBase)complexType);
            }
        }
    }

    private void VisitInput(InputObjectType type)
    {
        VisitDirectives(type);

        foreach (var field in type.Fields)
        {
            VisitDirectives(field);
            Visit((TypeSystemObjectBase)field.Type.NamedType());
        }
    }

    private void VisitDirective(DirectiveType type)
    {
        foreach (var argument in type.Arguments)
        {
            VisitDirectives(argument);
            Visit((TypeSystemObjectBase)argument.Type.NamedType());
        }
    }

    private void VisitDirectives(IHasDirectives hasDirectives)
    {
        foreach (var type in hasDirectives.Directives.Select(t => t.Type))
        {
            Visit(type);
        }
    }
}
