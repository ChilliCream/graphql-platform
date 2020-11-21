using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeTrimmer
    {
        private readonly HashSet<TypeSystemObjectBase> _touched = new();
        private readonly List<ObjectType> _rootTypes = new();
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
                if (directiveType.IsExecutableDirective)
                {
                    _touched.Add(directiveType);
                }
            }

            foreach (ObjectType rootType in _rootTypes)
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

            foreach (IEnumValue value in type.Values)
            {
                VisitDirectives(value);
            }
        }

        private void VisitObject(ObjectType type)
        {
            VisitDirectives(type);

            foreach (InterfaceType interfaceType in type.Implements)
            {
                VisitInterface(interfaceType);
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

        private void VisitInterface(InterfaceType type)
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

            foreach (IComplexOutputType complexType in
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
