using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;

namespace Zeus.Definitions
{
    internal class SchemaSyntaxVisitor
        : SyntaxNodeWalker
    {
        private Dictionary<string, ObjectTypeDefinition> _objectTypes 
            = new Dictionary<string, ObjectTypeDefinition>(StringComparer.Ordinal);
        private Dictionary<string, InputObjectTypeDefinition> _inputTypes 
            = new Dictionary<string, InputObjectTypeDefinition>(StringComparer.Ordinal);

        public IReadOnlyCollection<ObjectTypeDefinition> ObjectTypeDefinitions => _objectTypes.Values;
        public IReadOnlyCollection<InputObjectTypeDefinition> InputObjectTypeDefinitions => _inputTypes.Values;

        public ObjectTypeDefinition QueryTypeDefinition { get; private set; }
        public ObjectTypeDefinition MutationTypeDefinition { get; private set; }

        public bool HasQueryTypeDefinition => QueryTypeDefinition != null;

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition node)
        {
            string name = node.Name.Value;
            ObjectTypeDefinition objectTypeDefinition = new ObjectTypeDefinition(
                name, GetFieldDefinitions(node.Fields));

            if (_objectTypes.TryGetValue(name, out var current))
            {
                objectTypeDefinition = current.Merge(objectTypeDefinition);
            }

            if (WellKnownTypes.IsQuery(name))
            {
                QueryTypeDefinition = objectTypeDefinition;
            }

            if (WellKnownTypes.IsMutation(name))
            {
                MutationTypeDefinition = objectTypeDefinition;
            }

            _objectTypes[name] = objectTypeDefinition;
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition node)
        {
            string name = node.Name.Value;
            InputObjectTypeDefinition inputObjectTypeDefinition = new InputObjectTypeDefinition(
                name, GetInputFieldDefinitions(node.Fields));

            if (_inputTypes.TryGetValue(name, out var current))
            {
                inputObjectTypeDefinition = current.Merge(inputObjectTypeDefinition);
            }

            _inputTypes[name] = inputObjectTypeDefinition;
        }

        private IEnumerable<FieldDefinition> GetFieldDefinitions(IEnumerable<GraphQLFieldDefinition> fieldDefinitions)
        {
            foreach (GraphQLFieldDefinition fieldDefinition in fieldDefinitions)
            {
                yield return new FieldDefinition(fieldDefinition.Name.Value,
                    CreateTypeDefinition(fieldDefinition.Type),
                    GetInputFieldDefinitions(fieldDefinition.Arguments));
            }
        }

        private IEnumerable<InputFieldDefinition> GetInputFieldDefinitions(IEnumerable<GraphQLInputValueDefinition> inputValueDefinitions)
        {
            foreach (GraphQLInputValueDefinition inputValueDefinition in inputValueDefinitions)
            {
                yield return new InputFieldDefinition(inputValueDefinition.Name.Value,
                    CreateTypeDefinition(inputValueDefinition.Type));
            }
        }

        private TypeDefinition CreateTypeDefinition(GraphQLType type)
        {
            if (type is GraphQLNamedType n)
            {
                return new TypeDefinition(n.Name.Value, true,
                    ResolveTypeKind(n.Name.Value));
            }

            TypeDefinition declaration = null;
            Stack<GraphQLType> typeStack = CreateTypeStack(type);
            while (typeStack.Any())
            {
                GraphQLType current = typeStack.Pop();
                if (current is GraphQLNamedType namedType)
                {
                    declaration = new TypeDefinition(namedType.Name.Value,
                        IsNullable(typeStack), ResolveTypeKind(namedType.Name.Value));
                }
                else if (current is GraphQLListType list)
                {
                    declaration = new TypeDefinition("List",
                        IsNullable(typeStack), TypeKind.List,
                        declaration);
                }
            }

            return declaration;
        }

        private Stack<GraphQLType> CreateTypeStack(GraphQLType type)
        {
            Stack<GraphQLType> typeStack = new Stack<GraphQLType>();
            GraphQLType current = type;

            while (current != null)
            {
                typeStack.Push(current);

                if (current is GraphQLListType list)
                {
                    current = list.Type;
                }
                else if (current is GraphQLNonNullType nullable)
                {
                    current = nullable.Type;
                }
                else
                {
                    current = null;
                }
            }

            return typeStack;
        }

        private bool IsNullable(Stack<GraphQLType> typeStack)
        {
            if (typeStack.Any() && typeStack.Peek() is GraphQLNonNullType)
            {
                typeStack.Pop();
                return false;
            }
            return true;
        }

        private static TypeKind ResolveTypeKind(string name)
        {
            return ScalarTypes.Contains(name) ? TypeKind.Scalar : TypeKind.Object;
        }
    }
}