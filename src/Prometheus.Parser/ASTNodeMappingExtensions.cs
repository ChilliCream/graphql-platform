using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    internal static class ASTNodeMappingExtensions
    {
        public static IEnumerable<Argument> Map(this IEnumerable<GraphQLArgument> arguments)
        {
            if (arguments != null)
            {
                foreach (GraphQLArgument argument in arguments)
                {
                    yield return new Argument(argument.Name.Value, argument.Value.Map());
                }
            }
        }

        public static IEnumerable<Directive> Map(this IEnumerable<GraphQLDirective> directives)
        {
            if (directives != null)
            {
                foreach (GraphQLDirective directive in directives)
                {
                    yield return new Directive(directive.Name.Value, directive.Arguments.Map());
                }
            }
        }

        public static IEnumerable<VariableDefinition> Map(this IEnumerable<GraphQLVariableDefinition> variableDefinitions)
        {
            if (variableDefinitions != null)
            {
                foreach (GraphQLVariableDefinition variableDefinition in variableDefinitions)
                {
                    if (variableDefinition.DefaultValue == null)
                    {
                        yield return new VariableDefinition(
                            variableDefinition.Variable.Name.Value,
                            Map(variableDefinition.Type));
                    }
                    else
                    {
                        // TODO: Default Value => object
                        yield return new VariableDefinition(
                            variableDefinition.Variable.Name.Value,
                            Map(variableDefinition.Type));
                    }
                }
            }
        }

        public static Abstractions.OperationType Map(this GraphQLParser.AST.OperationType operationType)
        {
            switch (operationType)
            {
                case GraphQLParser.AST.OperationType.Query:
                    return Abstractions.OperationType.Query;
                case GraphQLParser.AST.OperationType.Mutation:
                    return Abstractions.OperationType.Mutation;
                default:
                    throw new NotSupportedException("The specified operation type is not supported.");
            }
        }

        public static IType Map(this GraphQLType type)
        {
            Stack<GraphQLType> typeStack = DisassembleSyntaxTypeNode(type);
            return BuildType(typeStack);
        }

        public static IValue Map(this GraphQLValue value)
        {
            if (ReferenceEquals(value, null))
            {
                return NullValue.Instance;
            }
            if (value is GraphQLScalarValue sv)
            {
                return CreateScalarValue(sv);
            }
            else if (value is GraphQLListValue lv)
            {
                return CreateListValue(lv);
            }
            else if (value is GraphQLObjectValue ov)
            {
                return CreateObjectValue(ov);
            }
            else if (value is GraphQLVariable v)
            {
                return new Variable(v.Name.Value);
            }
            else
            {
                throw new InvalidOperationException("Unknown grahql type.");
            }
        }

        private static IValue CreateScalarValue(GraphQLScalarValue scalarValue)
        {
            switch (scalarValue.Kind)
            {
                case ASTNodeKind.StringValue:
                    return new StringValue(scalarValue.Value);
                case ASTNodeKind.IntValue:
                    return new IntegerValue(int.Parse(scalarValue.Value));
                case ASTNodeKind.FloatValue:
                    return new FloatValue(decimal.Parse(scalarValue.Value));
                case ASTNodeKind.BooleanValue:
                    return new BooleanValue(bool.Parse(scalarValue.Value));
                case ASTNodeKind.EnumValue:
                    return new EnumValue(scalarValue.Value);
                case ASTNodeKind.NullValue:
                    return NullValue.Instance;
                default:
                    throw new InvalidOperationException("This is not a scalar type.");
            }
        }

        private static IValue CreateListValue(GraphQLListValue listValue)
        {
            List<IValue> values = new List<IValue>();
            foreach (GraphQLValue value in listValue.Values)
            {
                values.Add(Map(value));
            }
            return new ListValue(values);
        }

        private static IValue CreateObjectValue(GraphQLObjectValue objectValue)
        {
            Dictionary<string, IValue> fields = new Dictionary<string, IValue>();
            foreach (GraphQLObjectField field in objectValue.Fields)
            {
                fields[field.Name.Value] = Map(field.Value);
            }
            return new InputObjectValue(fields);
        }


        private static Stack<GraphQLType> DisassembleSyntaxTypeNode(GraphQLType type)
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

        private static IType BuildType(Stack<GraphQLType> typeStack)
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
    }
}