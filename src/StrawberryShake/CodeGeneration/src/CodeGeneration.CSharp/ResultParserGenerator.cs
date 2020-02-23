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

            body.AppendLine(
                $"public OnReviewResultParser({Types.ValueSerializerCollection} " +
                "serializerResolver)");
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
                        classBuilder, methodDescriptor, indent)));
        }

        private CodeBlockBuilder CreateParseDataMethodBody(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            var body = new StringBuilder();

            body.AppendLine($"return new {methodDescriptor.ResultType[0].Name}");
            body.AppendLine("(");

            for (int i = 0; i < methodDescriptor.Fields.Count; i++)
            {
                ResultFieldDescriptor field = methodDescriptor.Fields[i];

                if (i > 0)
                {
                    body.Append(", ");
                    body.AppendLine();
                }

                body.Append($"{indent}{field.ParserMethodName}(data, \"{field.Name}\")");
            }

            body.AppendLine();
            body.Append(");");

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AddParseMethod(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            ImmutableStack<ResultTypeDescriptor> resultType =
                ImmutableStack.CreateRange<ResultTypeDescriptor>(methodDescriptor.ResultType);

            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Protected)
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
                        .SetName("obj"))
                    .AddCode(CreateParseMethodBody(
                        classBuilder, methodDescriptor, resultType, indent, string.Empty)));
        }

        private CodeBlockBuilder CreateParseMethodBody(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            IImmutableStack<ResultTypeDescriptor> resultType,
            string indent,
            string initialIndent)
        {
            var body = new StringBuilder();

            IImmutableStack<ResultTypeDescriptor> next =
                resultType.Pop(out ResultTypeDescriptor type);

            if (type.IsList)
            {
                AppendList(
                    body,
                    new ListInfo(
                        "obj",
                        "i",
                        "count",
                        "element",
                        "result",
                        type.Name),
                    () => CreateParseMethodBody(
                        classBuilder, methodDescriptor, next,
                        indent, indent + initialIndent),
                    indent, initialIndent);
            }
            else
            {

            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AppendList(
            StringBuilder body,
            ListInfo list,
            Action appendSetElement,
            string indent,
            string initialIndent)
        {
            body.AppendLine($"{initialIndent}int {list.Length} = {list.Data}.GetArrayLength();");
            body.AppendLine($"{initialIndent}var {list.Result} = new {list.ResultType}[{list.Length}];");
            body.AppendLine($"{initialIndent}for (int {list.Counter} = 0; {list.Counter} < {list.Length}; {list.Counter}++)");
            body.AppendLine($"{initialIndent}{{");
            body.AppendLine($"{initialIndent}{indent}{Types.JsonElement} {list.Element} = {list.Data}[{list.Counter}];");
            body.AppendLine("");
            appendSetElement();
            body.Append($"{initialIndent}}}");
        }

        private void AppendNullHandling(
            StringBuilder body,
            string field,
            string indent,
            string initialIndent)
        {
            body.AppendLine($"{initialIndent}if (!parent.TryGetProperty(field, out {Types.JsonElement} {field})");
            body.AppendLine($"{initialIndent}{indent}|| obj.ValueKind == {Types.JsonValueKind}.Null)");
            body.AppendLine("{");
            body.AppendLine($"{initialIndent}{indent}return null;");
            body.Append("}");
        }

        private void AppendSetElement(
            StringBuilder body,
            string field,
            string indent,
            string initialIndent)
        {

        }

        private void AppendNewObject(
            StringBuilder body,
            string ResultModelType,
            string indent,
            string initialIndent)
        {

        }

        private bool IsNullable(ResultParserMethodDescriptor methodDescriptor)
        {
            if (methodDescriptor.IsNullable)
            {
                if (methodDescriptor.IsReferenceType)
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
            DeserializeNullableString(element, "name")
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
