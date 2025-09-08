using System.Text;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class EntityIdFactoryGenerator : CodeGenerator<EntityIdFactoryDescriptor>
{
    private const string Obj = "obj";
    private const string Type = "type";
    private const string UnderscoreOptions = "_options";
    private const string Writer = "writer";
    private const string JsonWriter = "jsonWriter";
    private const string EntityId = "entityId";
    private const string EntityIdValues = "entityIdValues";

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
            .AddField(UnderscoreOptions)
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
            .AddParameter(Obj, x => x.SetType(TypeNames.JsonElement))
            .AddCode(ParseEntityIdBody(descriptor));

        classBuilder
            .AddMethod("Format")
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(TypeNames.String)
            .AddParameter(EntityId, x => x.SetType(TypeNames.EntityId))
            .AddCode(FormatEntityIdBody(descriptor));

        foreach (var entity in descriptor.Entities)
        {
            classBuilder
                .AddMethod($"Parse{entity.Name}EntityId")
                .SetAccessModifier(AccessModifier.Private)
                .SetReturnType(TypeNames.EntityId)
                .AddParameter(Obj, x => x.SetType(TypeNames.JsonElement))
                .AddParameter(Type, x => x.SetType(TypeNames.String))
                .AddCode(ParseSpecificEntityIdBody(entity));

            classBuilder
                .AddMethod($"Format{entity.Name}EntityId")
                .SetAccessModifier(AccessModifier.Private)
                .SetReturnType(TypeNames.String)
                .AddParameter(EntityId, x => x.SetType(TypeNames.EntityId))
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
                        .SetMethodName(Obj, nameof(JsonElement.GetProperty))
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
                    .AddArgument(Obj)
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
            .AddArgument(Type)
            .AddArgument(value);
    }

    private static ICode ParseEntityIdProperty(ScalarEntityIdDescriptor field) =>
        MethodCallBuilder
            .Inline()
            .SetMethodName(Obj, nameof(JsonElement.GetProperty))
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
                .SetExpression($"{EntityId}.Name")
                .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.NotSupportedException));

        foreach (var entity in descriptor.Entities)
        {
            typeNameSwitch.AddCase(
                entity.Name.AsStringToken(),
                MethodCallBuilder
                    .Inline()
                    .SetMethodName($"Format{entity.Name}EntityId")
                    .AddArgument(EntityId));
        }

        return CodeBlockBuilder
            .New()
            .AddCode(typeNameSwitch);
    }

    private ICode FormatSpecificEntityIdBody(EntityIdDescriptor entity)
    {
        var body = CodeBlockBuilder
            .New();

        body.AddAssignment($"using var {Writer}")
            .SetRightHandSide(
                MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(TypeNames.ArrayWriter));

        body.AddAssignment($"using var {JsonWriter}")
            .SetRightHandSide(
                MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(TypeNames.Utf8JsonWriter)
                    .AddArgument(Writer)
                    .AddArgument(UnderscoreOptions));

        body.AddMethodCall()
            .SetMethodName(JsonWriter, nameof(Utf8JsonWriter.WriteStartObject));

        body.AddEmptyLine();

        body.AddMethodCall()
            .SetMethodName(JsonWriter, nameof(Utf8JsonWriter.WriteString))
            .AddArgument(WellKnownNames.TypeName.AsStringToken())
            .AddArgument($"{EntityId}.Name");

        body.AddEmptyLine();

        if (entity.Fields.Count == 1)
        {
            var field = entity.Fields[0];

            body.AddMethodCall()
                .SetMethodName(JsonWriter, GetWriteMethod(field))
                .AddArgument(field.Name.AsStringToken())
                .AddArgument($"({field.SerializationType}){EntityId}.Value");
        }
        else
        {
            body.AddAssignment($"var {EntityIdValues}")
                .SetRightHandSide(CodeBlockBuilder
                    .New()
                    .AddCode("(")
                    .AddCode(TupleBuilder
                        .New()
                        .AddMemberRange(
                            entity.Fields.Select(x => x.SerializationType.ToString())))
                    .AddCode($"){EntityId}.Value"));
            body.AddEmptyLine();

            for (var index = 0; index < entity.Fields.Count; index++)
            {
                var field = entity.Fields[index];

                body.AddMethodCall()
                    .SetMethodName(JsonWriter, GetWriteMethod(field))
                    .AddArgument(field.Name.AsStringToken())
                    .AddArgument($"{EntityIdValues}.Item{index + 1}");
                body.AddEmptyLine();
            }
        }

        body.AddMethodCall()
            .SetMethodName(JsonWriter, nameof(Utf8JsonWriter.WriteEndObject));

        body.AddMethodCall()
            .SetMethodName(JsonWriter, nameof(Utf8JsonWriter.Flush));

        body.AddEmptyLine();

        body.AddMethodCall()
            .SetReturn()
            .SetMethodName(TypeNames.EncodingUtf8, nameof(Encoding.UTF8.GetString))
            .AddArgument(MethodCallBuilder.Inline().SetMethodName(Writer, "GetInternalBuffer"))
            .AddArgument("0")
            .AddArgument($"{Writer}.Length");

        return body;
    }
}
