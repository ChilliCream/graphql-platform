using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;

namespace Zeus.Definitions
{
    public static class ASTTypeExtensions
    {
        public static TypeDefinition CreateTypeInfo(this GraphQLType type)
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

        private static Stack<GraphQLType> CreateTypeStack(GraphQLType type)
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

        private static bool IsNullable(Stack<GraphQLType> typeStack)
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