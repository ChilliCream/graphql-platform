using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using HotChocolate.Client.Core.Syntax;
using HotChocolate.Client.Core.Utilities;

namespace HotChocolate.Client.Core.Serializers
{
    public class QuerySerializer
    {
        private static readonly ConcurrentDictionary<Type, Tuple<string, MethodInfo>[]> typeCache = new ConcurrentDictionary<Type, Tuple<string, MethodInfo>[]>();

        private readonly int indentation;
        private readonly string comma = ",";
        private readonly string colon = ":";

        private int currentIndent;

        public QuerySerializer(int indentation = 0)
        {
            this.indentation = indentation;

            if (indentation > 0)
            {
                comma = ", ";
                colon = ": ";
            }
        }

        public string Serialize(OperationDefinition operation)
        {
            StringBuilder builder = new StringBuilder();

            switch (operation.Type)
            {
                case OperationType.Query:
                    builder.Append("query");
                    break;
                case OperationType.Mutation:
                    builder.Append("mutation");
                    break;
                case OperationType.Subscription:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (operation.Name != null)
            {
                builder.Append(' ').Append(operation.Name);
            }

            if (operation.VariableDefinitions.Count > 0)
            {
                builder.Append('(');

                var first = true;
                foreach (var v in operation.VariableDefinitions)
                {
                    if (!first) builder.Append(comma);
                    builder.Append('$').Append(v.Name).Append(colon).Append(v.Type);
                    first = false;
                }

                builder.Append(')');
            }

            SerializeSelections(operation, builder);

            foreach (var fragment in operation.FragmentDefinitions.Values)
            {
                builder.Append(Environment.NewLine);
                SerializeFragment(fragment, builder);
            }

            return builder.ToString();
        }

        private void SerializeFragment(FragmentDefinition fragment, StringBuilder builder)
        {
            builder.Append("fragment ");
            builder.Append(fragment.Name);
            builder.Append(" on ");
            builder.Append(fragment.TypeCondition);
            SerializeSelections(fragment, builder);
        }

        private void Serialize(FieldSelection field, StringBuilder builder)
        {
            if (field.Alias != null)
            {
                builder.Append(field.Alias);
                builder.Append(": ");
            }

            builder.Append(field.Name);

            if (field.Arguments?.Any() == true)
            {
                builder.Append('(');

                var first = true;
                foreach (var arg in field.Arguments)
                {
                    if (!first) builder.Append(comma);
                    builder.Append(arg.Name).Append(colon);
                    SerializeValue(builder, arg.Value);
                    first = false;
                }

                builder.Append(')');
            }

            if (field.Selections.Any() == true)
            {
                SerializeSelections(field, builder);
            }
        }

        private void Serialize(InlineFragment fragment, StringBuilder builder)
        {
            builder.Append("... on ");
            builder.Append(fragment.TypeCondition);

            if (fragment.Selections.Any() == true)
            {
                SerializeSelections(fragment, builder);
            }
        }

        private void Serialize(FragmentSpread fragmentSpread, StringBuilder builder)
        {
            builder.Append("...");
            builder.Append(fragmentSpread.Name);
        }

        private void SerializeSelections(ISelectionSet selectionSet, StringBuilder builder)
        {
            OpenBrace(builder);

            bool first = true;

            if (selectionSet.Selections != null)
            {
                foreach (var s in selectionSet.Selections)
                {
                    if (!first) Separator(builder);

                    var field = s as FieldSelection;
                    var fragment = s as InlineFragment;
                    var fragmentSpread = s as FragmentSpread;

                    if (field != null)
                    {
                        Serialize(field, builder);
                    }
                    else if (fragment != null)
                    {
                        Serialize(fragment, builder);
                    }
                    else if (fragmentSpread != null)
                    {
                        Serialize(fragmentSpread, builder);
                    }

                    first = false;
                }
            }

            CloseBrace(builder);
        }

        private void SerializeValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
            }
            else if (value is string s)
            {
                builder.Append(JsonConvert.ToString(s, '"'));
            }
            else if (value is Enum)
            {
                builder.Append(value.ToString().PascalCaseToSnakeCase());
            }
            else if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
            }
            else if (value is int || value is float)
            {
                builder.Append(value);
            }
            else if (value is ID id)
            {
                builder.Append(JsonConvert.ToString(id.Value, '"'));
            }
            else if (value is IEnumerable)
            {
                builder.Append("[");

                var i = 0;
                var valueEnumerator = ((IEnumerable)value).GetEnumerator();
                while (valueEnumerator.MoveNext())
                {
                    if (i != 0)
                    {
                        builder.Append(",");
                    }

                    SerializeValue(builder, valueEnumerator.Current);

                    i++;
                }

                builder.Append("]");
            }
            else if (value is VariableDefinition v)
            {
                builder.Append('$').Append(v.Name);
            }
            else
            {
                var objectType = value.GetType();

                Tuple<string, MethodInfo>[] properties;
                if (!typeCache.TryGetValue(objectType, out properties))
                {
                    properties = objectType.GetRuntimeProperties()
                        .Where(info => info.GetMethod.IsPublic)
                        .Select(info => new Tuple<string, MethodInfo>(info.Name.LowerFirstCharacter(), info.GetMethod))
                        .ToArray();

                    typeCache.TryAdd(objectType, properties);
                }
                else
                {
                    //Cache Hit
                }

                for (var index = 0; index < properties.Length; index++)
                {
                    var property = properties[index];

                    if (index == 0)
                    {
                        OpenBrace(builder);
                    }
                    else
                    {
                        builder.Append(",");
                    }

                    builder.Append(property.Item1.LowerFirstCharacter()).Append(colon);
                    SerializeValue(builder, property.Item2.Invoke(value, null));

                    if (index + 1 == properties.Length)
                    {
                        CloseBrace(builder);
                    }
                }
            }
        }

        private void OpenBrace(StringBuilder builder)
        {
            if (indentation == 0)
            {
                builder.Append('{');
            }
            else
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append("{\r\n");
                currentIndent += indentation;
                Indent(builder);
            }
        }

        private void CloseBrace(StringBuilder builder)
        {
            if (indentation == 0)
            {
                builder.Append('}');
            }
            else
            {
                currentIndent -= indentation;
                builder.Append("\r\n");
                Indent(builder);
                builder.Append('}');
            }
        }

        private void Separator(StringBuilder builder)
        {
            if (indentation == 0)
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append("\r\n");
                Indent(builder);
            }
        }

        private void Indent(StringBuilder builder)
        {
            for (var i = 0; i < currentIndent; ++i)
            {
                builder.Append(' ');
            }
        }
    }
}
