using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;

namespace Zeus.Types
{
    internal class SchemaSyntaxVisitor
        : SyntaxNodeWalker
    {
        private List<ObjectDeclaration> _objectTypes = new List<ObjectDeclaration>();
        private List<InputDeclaration> _inputTypes = new List<InputDeclaration>();

        public IReadOnlyCollection<ObjectDeclaration> ObjectTypes => _objectTypes;
        public IReadOnlyCollection<InputDeclaration> InputTypes => _inputTypes;

        public bool HasQueryType { get; private set; }

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition)
        {
            _objectTypes.Add(new ObjectDeclaration(
                objectTypeDefinition.Name.Value,
                DeserializeFieldDefinitions(objectTypeDefinition.Fields)));
            
            if (!HasQueryType)
            {
                HasQueryType = WellKnownTypes.IsQuery(objectTypeDefinition.Name.Value);
            }
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition inputObjectTypeDefinition)
        {
            _inputTypes.Add(new InputDeclaration(
                inputObjectTypeDefinition.Name.Value,
                DeserializeInputFields(inputObjectTypeDefinition.Fields)));
        }

        private IEnumerable<FieldDeclaration> DeserializeFieldDefinitions(IEnumerable<GraphQLFieldDefinition> fieldDefinitions)
        {
            foreach (GraphQLFieldDefinition fieldDefinition in fieldDefinitions)
            {
                yield return new FieldDeclaration(fieldDefinition.Name.Value,
                    DeserializeType(fieldDefinition.Type),
                    DeserializeInputFields(fieldDefinition.Arguments));
            }
        }

        private IEnumerable<InputFieldDeclaration> DeserializeInputFields(IEnumerable<GraphQLInputValueDefinition> inputValueDefinitions)
        {
            foreach (GraphQLInputValueDefinition inputValueDefinition in inputValueDefinitions)
            {
                yield return new InputFieldDeclaration(inputValueDefinition.Name.Value,
                    DeserializeType(inputValueDefinition.Type));
            }
        }

        private TypeDeclaration DeserializeType(GraphQLType type)
        {
            if (type is GraphQLNamedType n)
            {
                return new TypeDeclaration(n.Name.Value, true,
                    ResolveTypeKind(n.Name.Value));
            }

            TypeDeclaration declaration = null;
            Stack<GraphQLType> typeStack = CreateTypeStack(type);
            while (typeStack.Any())
            {
                GraphQLType current = typeStack.Pop();
                if (current is GraphQLNamedType namedType)
                {
                    declaration = new TypeDeclaration(namedType.Name.Value,
                        IsNullable(typeStack), ResolveTypeKind(namedType.Name.Value));
                }
                else if (current is GraphQLListType list)
                {
                    declaration = new TypeDeclaration("List",
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
            return BuiltInTypes.Contains(name) ? TypeKind.Scalar : TypeKind.Object;
        }
    }
}

