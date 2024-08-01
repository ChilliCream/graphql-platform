using System.Text;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class EntityIdFactoryGenerator : CodeGenerator<EntityIdFactoryDescriptor>
{
    private const string _obj = "obj";
    private const string _type = "type";
    private const string _typeName = "typeName";
    private const string _options = "_options";
    private const string _writer = "writer";
    private const string _jsonWriter = "jsonWriter";
    private const string _entityId = "entityId";
    private const string _entityIdValues = "entityIdValues";

    protected override bool CanHandle(EntityIdFactoryDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) => !settings.NoStore;

    protected override void Generate(EntityIdFactoryDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.Name;
        path = State;
        ns = descriptor.Namespace;

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .AddImplements(TypeNames.IEntityIdSerializer)
            .SetName(fileName);

        classBuilder
            .AddField(_options)
            .SetStatic()
            .SetReadOnly()
            .SetType(TypeNames.JsonWriterOptions)
            .SetValue(CodeBlockBuilder
                .New()
                .AddCode(MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(TypeNames.JsonWriterOptions))
                .AddCode(CodeInlineBuilder.From("{ Indented = false }")));

        classBuilder
            .AddMethod("Parse")
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(TypeNames.EntityId)
            .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement))
            .AddCode(ParseEntityIdBody(descriptor));

        classBuilder
            .AddMethod("Format")
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(TypeNames.String)
            .AddParameter(_entityId, x => x.SetType(TypeNames.EntityId))
            .AddCode(FormatEntityIdBody(descriptor));

        foreach (var entity in descriptor.Entities)
        {
            classBuilder
                .AddMethod($"Parse{entity.Name}EntityId")
                .SetAccessModifier(AccessModifier.Private)
                .SetReturnType(TypeNames.EntityId)
                .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement))
                .AddParameter(_type, x => x.SetType(TypeNames.String))
                .AddCode(ParseSpecificEntityIdBody(entity));

            classBuilder
                .AddMethod($"Format{entity.Name}EntityId")
                .SetAccessModifier(AccessModifier.Private)
                .SetReturnType(TypeNames.String)
                .AddParameter(_entityId, x => x.SetType(TypeNames.EntityId))
                .AddCode(FormatSpecificEntityIdBody(entity));
        }

        classBuilder.Build(writer);
    }

    private ICode ParseEntityIdBody(EntityIdFactoryDescriptor descriptor)
    {
        var typeNameAssignment =
            AssignmentBuilder
                .New()
                .SetLeftHandSide($"{TypeNames.String} {WellKnownNames.TypeName}")
                .SetRightHandSide(
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName(_obj, nameof(JsonElement.GetProperty))
                        .AddArgument(WellKnownNames.TypeName.AsStringToken())
                        .Chain(x => x
                            .SetMethodName(nameof(JsonElement.GetString))
                            .SetNullForgiving()));

        var typeNameSwitch =
            SwitchExpressionBuilder
                .New()
                .SetReturn()
                .SetExpression(WellKnownNames.TypeName)
                .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.NotSupportedException));

        foreach (var entity in descriptor.Entities)
        {
            typeNameSwitch.AddCase(
                entity.Name.AsStringToken(),
                MethodCallBuilder
                    .Inline()
                    .SetMethodName($"Parse{entity.Name}EntityId")
                    .AddArgument(_obj)
                    .AddArgument(WellKnownNames.TypeName));
        }

        return CodeBlockBuilder
            .New()
            .AddCode(typeNameAssignment)
            .AddEmptyLine()
            .AddCode(typeNameSwitch);
    }

    private ICode ParseSpecificEntityIdBody(EntityIdDescriptor entity)
    {
        ICode value;
        if (entity.Fields.Count == 1)
        {
            value = ParseEntityIdProperty(entity.Fields[0]);
        }
        else
        {
            value = TupleBuilder
                .New()
                .AddMemberRange(entity.Fields.Select(ParseEntityIdProperty));
        }

        return MethodCallBuilder
            .New()
            .SetReturn()
            .SetNew()
            .SetMethodName(TypeNames.EntityId)
            .AddArgument(_type)
            .AddArgument(value);
    }

    private static ICode ParseEntityIdProperty(ScalarEntityIdDescriptor field) =>
        MethodCallBuilder
            .Inline()
            .SetMethodName(_obj, nameof(JsonElement.GetProperty))
            .AddArgument(field.Name.AsStringToken())
            .Chain(x => x.SetMethodName(GetSerializerMethod(field)).SetNullForgiving());

    private static string GetSerializerMethod(ScalarEntityIdDescriptor field)
    {
        return JsonUtils.GetParseMethod(field.SerializationType);
    }

    private static string GetWriteMethod(ScalarEntityIdDescriptor field)
    {
        return JsonUtils.GetWriteMethod(field.SerializationType);
    }

    private ICode FormatEntityIdBody(EntityIdFactoryDescriptor descriptor)
    {
        var typeNameSwitch =
            SwitchExpressionBuilder
                .New()
                .SetReturn()
                .SetExpression($"{_entityId}.Name")
                .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.NotSupportedException));

        foreach (var entity in descriptor.Entities)
        {
            typeNameSwitch.AddCase(
                entity.Name.AsStringToken(),
                MethodCallBuilder
                    .Inline()
                    .SetMethodName($"Format{entity.Name}EntityId")
                    .AddArgument(_entityId));
        }

        return CodeBlockBuilder
            .New()
            .AddCode(typeNameSwitch);
    }

    private ICode FormatSpecificEntityIdBody(EntityIdDescriptor entity)
    {
        var body = CodeBlockBuilder
            .New();

        body.AddAssignment($"using var {_writer}")
            .SetRightHandSide(
                MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(TypeNames.ArrayWriter));

        body.AddAssignment($"using var {_jsonWriter}")
            .SetRightHandSide(
                MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(TypeNames.Utf8JsonWriter)
                    .AddArgument(_writer)
                    .AddArgument(_options));

        body.AddMethodCall()
            .SetMethodName(_jsonWriter, nameof(Utf8JsonWriter.WriteStartObject));

        body.AddEmptyLine();

        body.AddMethodCall()
            .SetMethodName(_jsonWriter, nameof(Utf8JsonWriter.WriteString))
            .AddArgument(WellKnownNames.TypeName.AsStringToken())
            .AddArgument($"{_entityId}.Name");

        body.AddEmptyLine();

        if (entity.Fields.Count == 1)
        {
            var field = entity.Fields[0];

            body.AddMethodCall()
                .SetMethodName(_jsonWriter, GetWriteMethod(field))
                .AddArgument(field.Name.AsStringToken())
                .AddArgument($"({field.SerializationType}){_entityId}.Value");
        }
        else
        {
            body.AddAssignment($"var {_entityIdValues}")
                .SetRightHandSide(CodeBlockBuilder
                    .New()
                    .AddCode("(")
                    .AddCode(TupleBuilder
                        .New()
                        .AddMemberRange(
                            entity.Fields.Select(x => x.SerializationType.ToString())))
                    .AddCode($"){_entityId}.Value"));
            body.AddEmptyLine();

            for (var index = 0; index < entity.Fields.Count; index++)
            {
                var field = entity.Fields[index];

                body.AddMethodCall()
                    .SetMethodName(_jsonWriter, GetWriteMethod(field))
                    .AddArgument(field.Name.AsStringToken())
                    .AddArgument($"{_entityIdValues}.Item{index + 1}");
                body.AddEmptyLine();
            }
        }

        body.AddMethodCall()
            .SetMethodName(_jsonWriter, nameof(Utf8JsonWriter.WriteEndObject));

        body.AddMethodCall()
            .SetMethodName(_jsonWriter, nameof(Utf8JsonWriter.Flush));

        body.AddEmptyLine();

        body.AddMethodCall()
            .SetReturn()
            .SetMethodName(TypeNames.EncodingUtf8, nameof(Encoding.UTF8.GetString))
            .AddArgument(MethodCallBuilder.Inline().SetMethodName(_writer, "GetInternalBuffer"))
            .AddArgument("0")
            .AddArgument($"{_writer}.Length");

        return body;
    }
}
