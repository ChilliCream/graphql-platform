using System;
using System.Text;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityIdFactoryGenerator : CodeGenerator<EntityIdFactoryDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EntityIdFactoryDescriptor descriptor)
        {
            var factory = ClassBuilder
                .New()
                .SetStatic()
                .SetAccessModifier(AccessModifier.Public)
                .SetName(descriptor.Name);

            var obj = ParameterBuilder
                .New()
                .SetName("obj")
                .SetType(WellKnownNames.JsonElement);

            var type = ParameterBuilder
                .New()
                .SetName("type")
                .SetType(WellKnownNames.String);

            var createEntityId = MethodBuilder
                .New()
                .SetStatic()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("CreateEntityId")
                .SetReturnType(WellKnownNames.EntityId)
                .AddParameter(obj)
                .AddCode(CreateEntityIdBody(descriptor));
            factory.AddMethod(createEntityId);


            foreach (var entity in descriptor.Entities)
            {
                var createSpecificEntityId = MethodBuilder
                    .New()
                    .SetStatic()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName($"Create{entity.Name}EntityId")
                    .SetReturnType(WellKnownNames.EntityId)
                    .AddParameter(obj)
                    .AddParameter(type)
                    .AddCode(CreateSpecificEntityIdBody(entity));
                factory.AddMethod(createSpecificEntityId);
            }

            factory.Build(writer);
        }

        private ICode CreateEntityIdBody(EntityIdFactoryDescriptor descriptor)
        {
            var sourceText = new StringBuilder();

            sourceText.AppendLine(
                "string typeName = obj.GetProperty(\"__typename\").GetString()!;");
            sourceText.AppendLine();

            sourceText.AppendLine("return typeName switch");
            sourceText.AppendLine("{");

            foreach (var entity in descriptor.Entities)
            {
                sourceText.AppendLine(
                    $"    \"{entity.Name}\" => Create{entity.Name}EntityId(obj, typeName),");
            }

            sourceText.AppendLine($"    _ => throw new {WellKnownNames.NotSupportedException}()");
            sourceText.AppendLine("};");

            return CodeBlockBuilder.From(sourceText);
        }

        private ICode CreateSpecificEntityIdBody(EntityIdDescriptor entity)
        {
            var sourceText = new StringBuilder();

            sourceText.AppendLine($"return new {WellKnownNames.EntityId}(");
            sourceText.AppendLine($"    type,");

            if (entity.Fields.Count == 0)
            {
                var field = entity.Fields[0];

                sourceText.AppendLine(
                    $"    obj.GetProperty(\"{field.Name}\").Get{field.TypeName}()!);");

                return CodeBlockBuilder.From(sourceText);
            }

            sourceText.Append($"(");

            bool next = false;

            foreach (var field in entity.Fields)
            {
                if (next)
                {
                    sourceText.AppendLine(",");
                }
                next = true;

                sourceText.Append($"    obj.GetProperty(\"{field.Name}\").Get{field.TypeName}()!");
            }

            sourceText.AppendLine($"));");

            return CodeBlockBuilder.From(sourceText);
        }
    }
}
