using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;
using Zeus.Abstractions;

namespace Zeus.Parser
{
    internal class SchemaSyntaxVisitor
        : SyntaxNodeWalker
    {
        private readonly List<ITypeDefinition> _typeDefinitions = new List<ITypeDefinition>();

        public IReadOnlyCollection<ITypeDefinition> TypeDefinitions => _typeDefinitions;

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition node)
        {
            _typeDefinitions.Add(new ObjectTypeDefinition(
                node.Name.Value,
                GetFieldDefinitions(node.Fields),
                node.Interfaces.Select(t => t.Name.Value)));
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition node)
        {
            _typeDefinitions.Add(new InputObjectTypeDefinition(
                node.Name.Value,
                GetInputValueDefinitions(node.Fields)));
        }

        protected override void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition node)
        {
            _typeDefinitions.Add(new InterfaceTypeDefinition(
                node.Name.Value,
                GetFieldDefinitions(node.Fields)));
        }

        protected override void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition node)
        {
            _typeDefinitions.Add(new UnionTypeDefinition(
                node.Name.Value,
                node.Types.Select(t => new NamedType(t.Name.Value))));
        }

        protected override void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition node)
        {
            _typeDefinitions.Add(new EnumTypeDefinition(
                node.Name.Value,
                node.Values.Select(t => t.ToString())));
        }

        private IEnumerable<FieldDefinition> GetFieldDefinitions(IEnumerable<GraphQLFieldDefinition> fieldDefinitions)
        {
            foreach (GraphQLFieldDefinition fieldDefinition in fieldDefinitions)
            {
                yield return new FieldDefinition(
                    fieldDefinition.Name.Value,
                    CreateType(fieldDefinition.Type),
                    GetInputValueDefinitions(fieldDefinition.Arguments));
            }
        }

        private IEnumerable<InputValueDefinition> GetInputValueDefinitions(IEnumerable<GraphQLInputValueDefinition> inputValueDefinitions)
        {
            foreach (GraphQLInputValueDefinition inputValueDefinition in inputValueDefinitions)
            {
                IValue value = null;
                if (inputValueDefinition.DefaultValue is GraphQLScalarValue sv)
                {
                    value = new ScalarValue(CreateType(sv.Kind), sv.Value);
                }

                yield return new InputValueDefinition(
                    inputValueDefinition.Name.Value,
                    CreateType(inputValueDefinition.Type),
                    value);
            }
        }

        private IType CreateType(GraphQLType type)
        {
            Stack<GraphQLType> typeStack = DisassembleSyntaxTypeNode(type);
            return BuildType(typeStack);
        }

        private NamedType CreateType(ASTNodeKind valueKind)
        {
            switch (valueKind)
            {
                case ASTNodeKind.StringValue:
                    return new NamedType(ScalarTypes.String);
                case ASTNodeKind.IntValue:
                    return new NamedType(ScalarTypes.Integer);
                case ASTNodeKind.BooleanValue:
                    return new NamedType(ScalarTypes.Boolean);
                case ASTNodeKind.FloatValue:
                    return new NamedType(ScalarTypes.Float);
                default:
                    throw new InvalidOperationException("This is not a scalar type.");
            }
        }

        private Stack<GraphQLType> DisassembleSyntaxTypeNode(GraphQLType type)
        {
            Stack<GraphQLType> typeStack = new Stack<GraphQLType>();

            GraphQLType current = type;
            while (current != null)
            {
                typeStack.Push(current);

                if (current is GraphQLNonNullType nnt)
                {
                    current = nnt.Type;
                }
                else if (current is GraphQLListType lt)
                {
                    current = lt.Type;
                }
                else
                {
                    current = null;
                }
            }

            return typeStack;
        }

        private IType BuildType(Stack<GraphQLType> typeStack)
        {
            IType type = null;
            GraphQLType current = null;

            while (typeStack.Any())
            {
                current = typeStack.Pop();
                if (current is GraphQLNamedType nt)
                {
                    type = new NamedType(nt.Name.Value);
                }
                else if (current is GraphQLNonNullType)
                {
                    type = new NonNullType(type);
                }
                else if (current is GraphQLListType)
                {
                    type = new ListType(type);
                }
                else
                {
                    throw new InvalidOperationException("The type structure is invalid.");
                }
            }

            return type;
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
    }
}