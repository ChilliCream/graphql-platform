using System.Linq;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class EntityIdFactoryGenerator : CodeGenerator<EntityIdFactoryDescriptor>
    {
        private const string _obj = "obj";
        private const string _type = "type";
        private const string _typeName = "typeName";
        private const string __typename = "__typename";

        protected override void Generate(
            CodeWriter writer,
            EntityIdFactoryDescriptor descriptor,
            out string fileName)
        {
            fileName = descriptor.Name;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetStatic()
                .SetAccessModifier(AccessModifier.Public)
                .SetName(fileName);

            classBuilder
                .AddMethod("CreateEntityId")
                .SetStatic()
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(TypeNames.EntityId)
                .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement))
                .AddCode(CreateEntityIdBody(descriptor));

            foreach (var entity in descriptor.Entities)
            {
                classBuilder
                    .AddMethod($"Create{entity.Name}EntityId")
                    .SetAccessModifier(AccessModifier.Private)
                    .SetStatic()
                    .SetReturnType(TypeNames.EntityId)
                    .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement))
                    .AddParameter(_type, x => x.SetType(TypeNames.String))
                    .AddCode(CreateSpecificEntityIdBody(entity));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private ICode CreateEntityIdBody(EntityIdFactoryDescriptor descriptor)
        {
            AssignmentBuilder typeNameAssigment =
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"{TypeNames.String} {_typeName}")
                    .SetRighthandSide(
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(_obj, nameof(JsonElement.GetProperty))
                            .AddArgument(__typename.AsStringToken())
                            .Chain(x => x
                                .SetMethodName(nameof(JsonElement.GetString))
                                .SetNullForgiving()));

            SwitchExpressionBuilder typeNameSwitch =
                SwitchExpressionBuilder
                    .New()
                    .SetReturn()
                    .SetExpression(_typeName)
                    .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.NotSupportedException));

            foreach (var entity in descriptor.Entities)
            {
                typeNameSwitch.AddCase(
                    entity.Name.AsStringToken(),
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName($"Create{entity.Name}EntityId")
                        .AddArgument(_obj)
                        .AddArgument(_typeName));
            }

            return CodeBlockBuilder
                .New()
                .AddCode(typeNameAssigment)
                .AddEmptyLine()
                .AddCode(typeNameSwitch);
        }

        private ICode CreateSpecificEntityIdBody(EntityIdDescriptor entity)
        {
            ICode value;
            if (entity.Fields.Count == 1)
            {
                value = CreateEntityIdProperty(entity.Fields[0]);
            }
            else
            {
                value = TupleBuilder
                    .New()
                    .AddMemberRange(entity.Fields.Select(CreateEntityIdProperty));
            }

            return MethodCallBuilder
                .New()
                .SetReturn()
                .SetNew()
                .SetMethodName(TypeNames.EntityId)
                .AddArgument(_type)
                .AddArgument(value);
        }

        private static ICode CreateEntityIdProperty(EntityIdDescriptor field) =>
            MethodCallBuilder
                .Inline()
                .SetMethodName(_obj, nameof(JsonElement.GetProperty))
                .AddArgument(field.Name.AsStringToken())
                .Chain(x => x.SetMethodName(GetSerializerMethod(field)).SetNullForgiving());

        private static string GetSerializerMethod(EntityIdDescriptor field) =>
            $"Get{field.TypeName.Split('.').Last()}";
    }
}
