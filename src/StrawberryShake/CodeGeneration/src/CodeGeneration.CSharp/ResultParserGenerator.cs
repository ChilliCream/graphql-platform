using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultParserGenerator
        : CSharpCodeGenerator<ResultParserDescriptor>
    {
        private static readonly Dictionary<string, string> _jsonMethod =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "string", "GetString" },
                { "bool", "GetBoolean" },
                { "byte", "GetByte" },
                { "short", "GetInt16" },
                { "int", "GetInt32" },
                { "long", "GetInt64" },
                { "ushort", "GetUInt16" },
                { "uint", "GetUInt32" },
                { "ulong", "GetUInt64" },
                { "decimal", "GetDecimal" },
                { "float", "GetSingle" },
                { "double", "GetDouble" }
            };

        protected override Task WriteAsync(
            CodeWriter writer,
            ResultParserDescriptor descriptor)
        {
            ClassBuilder classBuilder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name)
                    .AddImplements($"{Types.JsonResultParserBase}<{descriptor.ResultType}>");

            AddConstructor(
                classBuilder,
                descriptor.ValueSerializers,
                CodeWriter.Indent);

            foreach (ResultParserMethodDescriptor parserMethod in descriptor.ParseMethods)
            {
                if (parserMethod.IsRoot)
                {
                    AddParseDataMethod(classBuilder, parserMethod, CodeWriter.Indent);
                }
                else
                {
                    AddParseMethod(classBuilder, parserMethod, CodeWriter.Indent);
                }
            }

            foreach (ResultParserDeserializerMethodDescriptor deserializerMethod in
                descriptor.DeserializerMethods)
            {
                AddDeserializeMethod(classBuilder, deserializerMethod, CodeWriter.Indent);
            }

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private void AddConstructor(
            ClassBuilder classBuilder,
            IReadOnlyList<ValueSerializerDescriptor> serializers,
            string indent)
        {
            foreach (ValueSerializerDescriptor serializer in serializers)
            {
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetType(Types.IValueSerializer)
                        .SetName(serializer.FieldName));
            }

            classBuilder.AddConstructor(
                ConstructorBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.IValueSerializerCollection)
                        .SetName("serializerResolver"))
                    .AddCode(CreateConstructorBody(serializers, indent)));
        }

        private CodeBlockBuilder CreateConstructorBody(
            IReadOnlyList<ValueSerializerDescriptor> serializers,
            string indent)
        {
            var body = new StringBuilder();

            body.AppendLine("if (serializerResolver is null)");
            body.AppendLine("{");
            body.AppendLine(
                $"{indent}throw new {Types.ArgumentNullException}" +
                "(nameof(serializerResolver));");
            body.AppendLine("}");
            body.AppendLine();

            for (int i = 0; i < serializers.Count; i++)
            {
                if (i > 0)
                {
                    body.AppendLine();
                }
                body.Append(
                    $"{serializers[i].FieldName} = serializerResolver." +
                    $"Get(\"{serializers[i].Name}\");");
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AddParseDataMethod(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Protected)
                    .SetInheritance(Inheritance.Override)
                    .SetReturnType(
                        $"{methodDescriptor.ResultType}?",
                        IsNullable(methodDescriptor.ResultType.Components))
                    .SetReturnType(
                        $"{methodDescriptor.ResultType}",
                        IsNullable(methodDescriptor.ResultType.Components))
                    .SetName("ParseData")
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.JsonElement)
                        .SetName("data"))
                    .AddCode(CreateParseDataMethodBody(
                        methodDescriptor, indent)));
        }

        private static CodeBlockBuilder CreateParseDataMethodBody(
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            var body = new StringBuilder();

            body.Append($"return ");

            AppendNewObject(
                body,
                methodDescriptor.PossibleTypes[0].Name,
                "data",
                methodDescriptor.PossibleTypes[0].Fields,
                indent,
                string.Empty);

            body.Append(";");

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AddParseMethod(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetInheritance(Inheritance.Override)
                    .SetReturnType(
                        $"{methodDescriptor.ResultType}?",
                        IsNullable(methodDescriptor.ResultType.Components))
                    .SetReturnType(
                        $"{methodDescriptor.ResultType}",
                        !IsNullable(methodDescriptor.ResultType.Components))
                    .SetName(methodDescriptor.Name)
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.JsonElement)
                        .SetName("parent"))
                    .AddCode(CreateParseMethodBody(methodDescriptor, indent)));
        }

        private CodeBlockBuilder CreateParseMethodBody(
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            var body = new StringBuilder();

            if (methodDescriptor.ResultType.Components[0].IsList
                && methodDescriptor.ResultType.Components[1].IsList)
            {
                AppendNestedList(body, methodDescriptor, indent);
            }
            else if (methodDescriptor.ResultType.Components[0].IsList)
            {
                AppendList(body, methodDescriptor, indent);
            }
            else
            {
                AppendObject(body, methodDescriptor, indent);
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AddDeserializeMethod(
            ClassBuilder classBuilder,
            ResultParserDeserializerMethodDescriptor methodDescriptor,
            string indent)
        {
            ImmutableQueue<ResultTypeComponentDescriptor> runtimeTypeComponents =
                ImmutableQueue.CreateRange<ResultTypeComponentDescriptor>(
                    methodDescriptor.RuntimeTypeComponents);

            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetInheritance(Inheritance.Override)
                    .SetReturnType(
                        $"{methodDescriptor.RuntimeType}?",
                        IsNullable(methodDescriptor.RuntimeTypeComponents))
                    .SetReturnType(
                        $"{methodDescriptor.RuntimeType}",
                        !IsNullable(methodDescriptor.RuntimeTypeComponents))
                    .SetName(methodDescriptor.Name)
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.JsonElement)
                        .SetName("obj"))
                    .AddParameter(ParameterBuilder.New()
                        .SetType("string")
                        .SetName("field"))
                    .AddCode(CreateDeserializeMethodBody(
                        methodDescriptor, runtimeTypeComponents, indent)));
        }

        private CodeBlockBuilder CreateDeserializeMethodBody(
            ResultParserDeserializerMethodDescriptor methodDescriptor,
            ImmutableQueue<ResultTypeComponentDescriptor> runtimeTypeComponents,
            string indent)
        {
            var body = new StringBuilder();

            ImmutableQueue<ResultTypeComponentDescriptor> next =
                runtimeTypeComponents.Dequeue(out ResultTypeComponentDescriptor type);

            if (type.IsList && next.Peek().IsList)
            {
            }
            else if (type.IsList)
            {
                AppendDeserializeLeafList(body, methodDescriptor, type, next, indent);
            }
            else
            {
                AppendDeserializeLeaf(body, methodDescriptor, type, indent);
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AppendNestedList(
            StringBuilder body,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            AppendNullHandling(
                body,
                "parent",
                "obj",
                IsNullable(methodDescriptor.ResultType.Components[0]),
                indent,
                string.Empty);

            body.AppendLine();
            body.AppendLine();

            AppendList(
                body,
                new ListInfo(
                    "obj",
                    "i",
                    "count",
                    "element",
                    "result",
                    BuildTypeName(methodDescriptor.ResultType.Components)),
                IsNullable(methodDescriptor.ResultType.Components[1]),
                listIndent => AppendList(
                    body,
                    new ListInfo(
                        "element",
                        "j",
                        "innerCount",
                        "innerElement",
                        "innerResult",
                        BuildTypeName(methodDescriptor.ResultType.Components, 1)),
                    IsNullable(methodDescriptor.ResultType.Components[2]),
                    elementIndent => AppendSetElement(
                        body,
                        "innerResult",
                        "j",
                        methodDescriptor,
                        "innerElement",
                        indent,
                        elementIndent),
                    indent, listIndent),
                indent, string.Empty);

            body.AppendLine();
            body.AppendLine();
            body.AppendLine("return result;");
        }

        private void AppendList(
            StringBuilder body,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            AppendNullHandling(
                body,
                "parent",
                "obj",
                IsNullable(methodDescriptor.ResultType.Components[0]),
                indent,
                string.Empty);

            body.AppendLine();
            body.AppendLine();

            AppendList(
                body,
                new ListInfo(
                    "obj",
                    "i",
                    "count",
                    "element",
                    "result",
                    BuildTypeName(methodDescriptor.ResultType.Components)),
                IsNullable(methodDescriptor.ResultType.Components[1]),
                initialIndent => AppendSetElement(
                    body,
                    "result",
                    "i",
                    methodDescriptor,
                    "element",
                    indent,
                    initialIndent),
                indent, string.Empty);

            body.AppendLine();
            body.AppendLine();
            body.AppendLine("return result;");
        }

        private void AppendObject(
            StringBuilder body,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            AppendNullHandling(
                body,
                "parent",
                "obj",
                IsNullable(methodDescriptor.ResultType.Components[0]),
                indent,
                string.Empty);

            body.AppendLine();
            body.AppendLine();

            AppendTypeCase(
                body,
                methodDescriptor,
                (ii, type) =>
                {
                    body.Append($"{ii}return ");

                    AppendNewObject(
                        body,
                        type.Name,
                        "obj",
                        type.Fields,
                        indent,
                        ii);

                    body.Append(";");
                },
                indent,
                string.Empty);
        }

        private void AppendTypeCase(
            StringBuilder body,
            ResultParserMethodDescriptor methodDescriptor,
            Action<string, ResultTypeDescriptor> appendNewObject,
            string indent,
            string initialIndent)
        {
            if (methodDescriptor.PossibleTypes.Count == 1)
            {
                appendNewObject(initialIndent, methodDescriptor.PossibleTypes[0]);
            }
            else
            {
                body.AppendLine($"{initialIndent}switch(obj.GetProperty(\"__typeName\").GetString())");
                body.AppendLine($"{initialIndent}{{");

                foreach (ResultTypeDescriptor type in methodDescriptor.PossibleTypes)
                {
                    body.AppendLine($"{initialIndent}{indent}case \"{type.GraphQLTypeName}\":");
                    appendNewObject($"{initialIndent}{indent}{indent}", type);
                    body.AppendLine();
                }

                body.AppendLine($"{initialIndent}{indent}default:");
                body.Append($"{initialIndent}{indent}{indent}");
                body.Append($"throw new {Types.InvalidOperationException}();");

                body.Append($"{initialIndent}}}");
            }
        }

        private void AppendList(
            StringBuilder body,
            ListInfo list,
            bool isElementNullable,
            Action<string> appendSetElement,
            string indent,
            string initialIndent)
        {
            body.AppendLine($"{initialIndent}int {list.Length} = {list.Data}.GetArrayLength();");
            body.AppendLine($"{initialIndent}var {list.Result} = new {list.ResultType}[{list.Length}];");
            body.AppendLine();

            body.AppendLine($"{initialIndent}for (int {list.Counter} = 0; {list.Counter} < {list.Length}; {list.Counter}++)");
            body.AppendLine($"{initialIndent}{{");
            body.AppendLine($"{initialIndent}{indent}{Types.JsonElement} {list.Element} = {list.Data}[{list.Counter}];");
            body.AppendLine();
            if (isElementNullable)
            {
                body.AppendLine($"{initialIndent}{indent}if({list.Element}.ValueKind == {Types.JsonValueKind}.Null)");
                body.AppendLine($"{initialIndent}{indent}{{");
                body.AppendLine($"{initialIndent}{indent}{indent}{list.Result}[{list.Counter}] = null;");
                body.AppendLine($"{initialIndent}{indent}}}");
                body.AppendLine($"{initialIndent}{indent}else");
                body.AppendLine($"{initialIndent}{indent}{{");
                appendSetElement($"{initialIndent}{indent}{indent}");
                body.AppendLine();
                body.AppendLine($"{initialIndent}{indent}}}");
            }
            else
            {
                appendSetElement($"{initialIndent}{indent}");
                body.AppendLine();
            }
            body.Append($"{initialIndent}}}");
        }

        private void AppendNullHandling(
            StringBuilder body,
            string parent,
            string field,
            bool isNullable,
            string indent,
            string initialIndent)
        {
            if (isNullable)
            {
                body.AppendLine($"{initialIndent}if (!{parent}.TryGetProperty(field, out {Types.JsonElement} {field})");
                body.AppendLine($"{initialIndent}{indent}|| obj.ValueKind == {Types.JsonValueKind}.Null)");
                body.AppendLine("{");
                body.AppendLine($"{initialIndent}{indent}return null;");
                body.Append("}");
            }
            else
            {
                body.AppendLine($"{initialIndent}if (!{parent}.TryGetProperty(field, out {Types.JsonElement} {field})");
                body.AppendLine($"{initialIndent}{indent}|| obj.ValueKind == {Types.JsonValueKind}.Null)");
                body.AppendLine("{");
                body.AppendLine($"{initialIndent}{indent}throw new {Types.InvalidOperationException}(field);");
                body.Append("}");
            }
        }

        private void AppendSetElement(
            StringBuilder body,
            string array,
            string counter,
            ResultParserMethodDescriptor methodDescriptor,
            string element,
            string indent,
            string initialIndent)
        {
            bool multipleTypes = methodDescriptor.PossibleTypes.Count > 0;

            AppendTypeCase(
                body,
                methodDescriptor,
                (ii, type) =>
                {
                    body.Append($"{ii}{array}[{counter}] = ");
                    AppendNewObject(
                        body,
                        BuildTypeName(type.Components, type.Components.Count - 1),
                        element,
                        type.Fields,
                        indent,
                        initialIndent);
                    body.AppendLine(";");
                    if (multipleTypes)
                    {
                        body.Append($"{ii}break;");
                    }
                },
                indent,
                initialIndent);
        }

        private static void AppendNewObject(
            StringBuilder body,
            string resultType,
            string element,
            IReadOnlyList<ResultFieldDescriptor> fields,
            string indent,
            string initialIndent)
        {
            body.AppendLine($"new {resultType}");
            body.AppendLine($"{initialIndent}(");

            for (int i = 0; i < fields.Count; i++)
            {
                ResultFieldDescriptor field = fields[i];

                if (i > 0)
                {
                    body.Append(", ");
                    body.AppendLine();
                }

                body.Append(
                    $"{initialIndent}{indent}{field.ParserMethodName}" +
                    $"({element}, \"{field.Name}\")");
            }

            body.AppendLine();
            body.Append($"{initialIndent})");
        }

        private void AppendDeserializeLeafList(
            StringBuilder body,
            ResultParserDeserializerMethodDescriptor methodDescriptor,
            ResultTypeComponentDescriptor type,
            ImmutableQueue<ResultTypeComponentDescriptor> runtimeTypeComponents,
            string indent)
        {
            AppendNullHandling(
                body,
                "obj",
                "value",
                IsNullable(type),
                indent,
                string.Empty);

            body.AppendLine();
            body.AppendLine();

            AppendList(
                body,
                new ListInfo(
                    "value",
                    "i",
                    "count",
                    "element",
                    "result",
                    type.Name),
                IsNullable(runtimeTypeComponents.Peek()),
                initialIndent => AppendSetLeafElement(
                    body,
                    "result",
                    "i",
                    methodDescriptor.Serializer.FieldName,
                    "element",
                    methodDescriptor.SerializationType,
                    runtimeTypeComponents.Peek().Name,
                    IsNullable(runtimeTypeComponents.Peek()),
                    initialIndent),
                indent, string.Empty);

            body.AppendLine();
            body.AppendLine();
            body.AppendLine("return result;");
        }

        private void AppendDeserializeLeaf(
            StringBuilder body,
            ResultParserDeserializerMethodDescriptor methodDescriptor,
            ResultTypeComponentDescriptor type,
            string indent)
        {
            AppendNullHandling(
                body,
                "obj",
                "value",
                IsNullable(type),
                indent,
                string.Empty);

            body.AppendLine();
            body.AppendLine();

            body.Append($"return ");

            AppendDeserializeLeafValue(
                body,
                methodDescriptor.Serializer.FieldName,
                "value",
                methodDescriptor.SerializationType,
                methodDescriptor.RuntimeType,
                IsNullable(type));

            body.Append(";");
        }

        private static void AppendSetLeafElement(
            StringBuilder body,
            string array,
            string counter,
            string serializer,
            string element,
            string serializationType,
            string runtimeType,
            bool isNullable,
            string initialIndent)
        {
            body.Append($"{initialIndent}{array}[{counter}] = ");
            AppendDeserializeLeafValue(
                body, serializer, element,
                serializationType, runtimeType,
                isNullable);
            body.Append(";");
        }

        private static void AppendDeserializeLeafValue(
            StringBuilder body,
            string serializer,
            string element,
            string serializationType,
            string resultType,
            bool isNullable)
        {
            string jsonMethod = _jsonMethod[serializationType];

            if (isNullable)
            {
                body.Append($"({resultType}?)");
            }
            else
            {
                body.Append($"({resultType})");
            }

            body.Append($"{serializer}.Deserialize({element}.{jsonMethod}())!");
        }

        private bool IsNullable(IReadOnlyList<ResultTypeComponentDescriptor> typeComponents) =>
            IsNullable(typeComponents[0]);

        private bool IsNullable(ResultTypeComponentDescriptor typeComponent)
        {
            if (typeComponent.IsNullable)
            {
                if (typeComponent.IsReferenceType)
                {
                    return NullableRefTypes;
                }
                return true;
            }
            return false;
        }

        private string BuildTypeName(
            IReadOnlyList<ResultTypeComponentDescriptor> typeComponents,
            int start = 0)
        {
            var typeName = new StringBuilder();

            for (int i = start; i < typeComponents.Count; i++)
            {
                if (typeName.Length == 0)
                {
                    typeName.Append(IsNullable(typeComponents[i])
                        ? $"{typeComponents[i].Name}?"
                        : typeComponents[i].Name);
                }
                else
                {
                    typeName.Append(IsNullable(typeComponents[i])
                        ? $"<{typeComponents[i].Name}?"
                        : $"<{typeComponents[i].Name}");
                }
            }

            return typeName.Append(new string('>', typeComponents.Count - start)).ToString();
        }

        private readonly ref struct ListInfo
        {
            public ListInfo(
                string data,
                string counter,
                string length,
                string element,
                string result,
                string resultType)
            {
                Data = data;
                Counter = counter;
                Length = length;
                Element = element;
                Result = result;
                ResultType = resultType;
            }

            public string Data { get; }

            public string Counter { get; }

            public string Length { get; }

            public string Element { get; }

            public string Result { get; }

            public string ResultType { get; }
        }
    }
}
