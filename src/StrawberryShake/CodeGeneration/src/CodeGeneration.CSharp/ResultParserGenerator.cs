using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultParserGenerator
        : CSharpCodeGenerator<ResultParserDescriptor>
    {
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
                        .SetType(Types.ValueSerializer)
                        .SetName(serializer.FieldName));
            }

            classBuilder.AddConstructor(
                ConstructorBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.ValueSerializerCollection)
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
                    .SetReturnType($"{methodDescriptor.ResultTypeInterface}?", IsNullable(methodDescriptor))
                    .SetReturnType($"{methodDescriptor.ResultTypeInterface}?", !IsNullable(methodDescriptor))
                    .SetName("ParseData")
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.JsonElement)
                        .SetName("data"))
                    .AddCode(CreateParseDataMethodBody(
                        methodDescriptor, indent)));
        }

        private CodeBlockBuilder CreateParseDataMethodBody(
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            var body = new StringBuilder();

            body.Append($"return ");

            AppendNewObject(
                body,
                methodDescriptor.ResultType[0].Name,
                "data",
                methodDescriptor.Fields,
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
            ImmutableQueue<ResultTypeDescriptor> resultType =
                ImmutableQueue.CreateRange<ResultTypeDescriptor>(
                    methodDescriptor.ResultType);

            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetInheritance(Inheritance.Override)
                    .SetReturnType(
                        $"{methodDescriptor.ResultTypeInterface}?",
                        IsNullable(methodDescriptor))
                    .SetReturnType(
                        $"{methodDescriptor.ResultTypeInterface}?",
                        !IsNullable(methodDescriptor))
                    .SetName(methodDescriptor.Name)
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.JsonElement)
                        .SetName("parent"))
                    .AddCode(CreateParseMethodBody(
                        methodDescriptor, resultType, indent)));
        }

        private CodeBlockBuilder CreateParseMethodBody(
            ResultParserMethodDescriptor methodDescriptor,
            ImmutableQueue<ResultTypeDescriptor> resultType,
            string indent)
        {
            var body = new StringBuilder();

            ImmutableQueue<ResultTypeDescriptor> next =
                resultType.Dequeue(out ResultTypeDescriptor type);

            if (type.IsList && next.Peek().IsList)
            {
                AppendNestedList(body, methodDescriptor, type, next, indent);
            }
            else if (type.IsList)
            {
                AppendList(body, methodDescriptor, type, next, indent);
            }
            else
            {
                AppendObject(body, methodDescriptor, type, indent);
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AppendNestedList(
            StringBuilder body,
            ResultParserMethodDescriptor methodDescriptor,
            ResultTypeDescriptor type,
            ImmutableQueue<ResultTypeDescriptor> elementType,
            string indent)
        {
            AppendNullHandling(
                body,
                "obj",
                type.IsNullable,
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
                    type.Name),
                    elementType.Peek().IsNullable,
                listIndent => AppendList(
                    body,
                    new ListInfo(
                        "element",
                        "j",
                        "innerCount",
                        "innerElement",
                        "innerResult",
                        elementType.Peek().Name),
                    elementType.Dequeue().Peek().IsNullable,
                    elementIndent => AppendSetElement(
                        body,
                        "innerResult",
                        "j",
                        elementType.Dequeue().Peek().Name,
                        "innerElement",
                        methodDescriptor.Fields,
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
            ResultTypeDescriptor type,
            ImmutableQueue<ResultTypeDescriptor> elementType,
            string indent)
        {
            AppendNullHandling(
                body,
                "obj",
                type.IsNullable,
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
                    type.Name),
                elementType.Peek().IsNullable,
                initialIndent => AppendSetElement(
                    body,
                    "result",
                    "i",
                    elementType.Peek().Name,
                    "element",
                    methodDescriptor.Fields,
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
            ResultTypeDescriptor type,
            string indent)
        {
            AppendNullHandling(
                body,
                "obj",
                type.IsNullable,
                indent,
                string.Empty);

            body.AppendLine();
            body.AppendLine();

            body.Append($"return ");

            AppendNewObject(
                body,
                type.Name,
                "obj",
                methodDescriptor.Fields,
                indent,
                string.Empty);

            body.Append(";");
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
            string element,
            bool isNullable,
            string indent,
            string initialIndent)
        {
            if (isNullable)
            {
                body.AppendLine($"{initialIndent}if (!parent.TryGetProperty(field, out {Types.JsonElement} {element})");
                body.AppendLine($"{initialIndent}{indent}|| obj.ValueKind == {Types.JsonValueKind}.Null)");
                body.AppendLine("{");
                body.AppendLine($"{initialIndent}{indent}return null;");
                body.Append("}");
            }
            else
            {
                body.AppendLine($"{initialIndent}if (!parent.TryGetProperty(field, out {Types.JsonElement} {element})");
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
            string resultType,
            string element,
            IReadOnlyList<ResultFieldDescriptor> fields,
            string indent,
            string initialIndent)
        {
            body.Append($"{initialIndent}{array}[{counter}] = ");
            AppendNewObject(body, resultType, element, fields, indent, initialIndent);
            body.Append(";");
        }

        private void AppendNewObject(
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

        private bool IsNullable(ResultParserMethodDescriptor methodDescriptor)
        {
            if (methodDescriptor.ResultType[0].IsNullable)
            {
                if (methodDescriptor.ResultType[0].IsReferenceType)
                {
                    return NullableRefTypes;
                }
                return true;
            }
            return false;
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

/*

1. Object
2. List of Leaf Types
3. List of List of Lead Types
4. List of Object Types
6. List of List of Object Types

if (!parent.TryGetProperty(field, out JsonElement obj)
    || obj.ValueKind == JsonValueKind.Null)
{
    return null;
}


int count = obj.GetArrayLength();
var result = new IHasName[objLength];

for (int i = 0; i < count; i++)
{
    JsonElement element = obj[i];

    if (!parent.TryGetProperty(field, out JsonElement obj)
        || obj.ValueKind == JsonValueKind.Null)
    {
        list[i] = null;
    }
    else
    {
        list[i] = new HasName
        (
            DeserializeNullableString(obj, "name")
        );
    }
}

int count = obj.GetArrayLength();
var result = new IHasName[objLength];

for (int i = 0; i < count; i++)
{
    JsonElement element = obj[i];

    result[i] = new HasName
    (
        DeserializeNullableString(element, "name")
    );
}

int count = obj.GetArrayLength();
var result = new IReadOnlyList<IHasName>[objLength];

for (int i = 0; i < count; i++)
{
    JsonElement element = obj[i];

    int innerCount = obj.GetArrayLength();
    var innerResult = new IHasName[objLength];

    for (int j = 0; j < innerCount; j++)
    {
        JsonElement innerElement = element[j];

        innerResult[i] = new HasName
        (
            DeserializeNullableString(element, "name")
        );
    }

    result[i] = innerResult
}

*/
