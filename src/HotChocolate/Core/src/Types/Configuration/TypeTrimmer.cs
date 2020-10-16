using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeTrimmer
    {
        private readonly HashSet<TypeSystemObjectBase> _touched =
            new HashSet<TypeSystemObjectBase>();
        private readonly TypeRegistry _discoveredTypes;

        public TypeTrimmer(TypeRegistry discoveredTypes)
        {
            _discoveredTypes = discoveredTypes;
        }

        public IReadOnlyCollection<TypeSystemObjectBase> Types => _touched;

        public void VisitRoot(ObjectType rootType)
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
        }

        private void VisitEnum(EnumType type)
        {
            VisitDirectives(type);

            foreach (IEnumValue value in type.Values)
            {
                VisitDirectives(value);
            }
        }

        private void VisitObject(ObjectType type)
        {
            VisitDirectives(type);

            foreach (InterfaceType interfaceType in type.Interfaces)
            {
                VisitInterface(interfaceType, true);
            }

            foreach (ObjectField field in type.Fields)
            {
                VisitDirectives(field);
                Visit((TypeSystemObjectBase)field.Type.NamedType());

                foreach (Argument argument in field.Arguments)
                {
                    VisitDirectives(argument);
                    Visit((TypeSystemObjectBase)argument.Type.NamedType());
                }
            }
        }

        private void VisitUnion(UnionType type)
        {
            VisitDirectives(type);

            foreach (ObjectType objectType in type.Types.Values)
            {
                Visit(objectType);
            }
        }

        private void VisitInterface(InterfaceType type, bool implements = false)
        {
            VisitDirectives(type);

            foreach (InterfaceField field in type.Fields)
            {
                VisitDirectives(field);
                Visit((TypeSystemObjectBase)field.Type.NamedType());

                foreach (Argument argument in field.Arguments)
                {
                    VisitDirectives(argument);
                    Visit((TypeSystemObjectBase)argument.Type.NamedType());
                }
            }

            foreach (ObjectType objectType in
                _discoveredTypes.Types.Select(t => t.Type).OfType<ObjectType>())
            {
                if (objectType.IsImplementing(type))
                {
                    Visit(objectType);
                }
            }
        }

        private void VisitInput(InputObjectType type)
        {
            VisitDirectives(type);

            foreach (InputField field in type.Fields)
            {
                VisitDirectives(field);
                Visit((TypeSystemObjectBase)field.Type.NamedType());
            }
        }

        private void VisitDirective(DirectiveType type)
        {
            foreach (Argument argument in type.Arguments)
            {
                VisitDirectives(argument);
                Visit((TypeSystemObjectBase)argument.Type.NamedType());
            }
        }

        private void VisitDirectives(IHasDirectives hasDirectives)
        {
            foreach (DirectiveType type in hasDirectives.Directives.Select(t => t.Type))
            {
                Visit(type);
            }
        }
    }
}
