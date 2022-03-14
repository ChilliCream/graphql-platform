using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class ResultTypeGenerator : CodeGenerator<ObjectTypeDescriptor>
{
    protected override void Generate(
        ObjectTypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.RuntimeType.Name;
        path = null;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        IReadOnlyList<PropertyDescriptor> equalityProperties = descriptor.Properties;

        if (descriptor.Deferred.Count > 0)
        {
            var temp = descriptor.Properties.ToList();

            foreach (DeferredFragmentDescriptor deferred in descriptor.Deferred)
            {
                var fieldName = GetFieldName(deferred.Label);
                var propertyName = GetPropertyName(deferred.Label);
                temp.Add(new(propertyName, fieldName, deferred.Interface));
            }

            equalityProperties = temp;
        }

        ClassBuilder classBuilder = ClassBuilder
            .New()
            .SetComment(descriptor.Description)
            .SetName(fileName)
            .AddEquality(fileName, equalityProperties);

        ConstructorBuilder constructorBuilder = classBuilder
            .AddConstructor()
            .SetTypeName(fileName);

        foreach (PropertyDescriptor prop in descriptor.Properties)
        {
            TypeReferenceBuilder propTypeBuilder = prop.Type.ToTypeReference();

            // Add Property to class
            classBuilder
                .AddProperty(prop.Name)
                .SetComment(prop.Description)
                .SetName(prop.Name)
                .SetType(propTypeBuilder)
                .SetPublic();

            // Add initialization of property to the constructor
            var paramName = GetParameterName(prop.Name);
            constructorBuilder
                .AddParameter(paramName, x => x.SetType(propTypeBuilder))
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide(GetLeftPropertyAssignment(prop.Name))
                    .SetRighthandSide(paramName));
        }

        classBuilder.AddImplementsRange(descriptor.Implements.Select(x => x.Value));

        foreach (DeferredFragmentDescriptor deferred in descriptor.Deferred)
        {
            var fieldName = GetFieldName(deferred.Label);
            var propertyName = GetPropertyName(deferred.Label);
            var paramName = GetParameterName(deferred.Label);

            // Add backing field
            classBuilder
                .AddField(FieldBuilder
                    .New()
                    .SetReadOnly()
                    .SetName(fieldName)
                    .SetType($"{deferred.InterfaceName}?"));

            // Add fragment data property
            classBuilder
                .AddProperty(PropertyBuilder
                    .New()
                    .SetPublic()
                    .SetName(propertyName)
                    .AsLambda(fieldName)
                    .SetType($"{deferred.InterfaceName}?"));

            // Add fragment data resolver
            classBuilder
                .AddMethod("TryGetData")
                .AddParameter(ParameterBuilder
                    .New()
                    .SetName("data")
                    .SetType($"out {deferred.InterfaceName}?"))
                .SetReturnType(TypeNames.Boolean)
                .SetPublic()
                .AddBody()
                .AddLine($"data = {fieldName};")
                .AddLine($"return data != null;");

            // Add initialization of property to the constructor
            constructorBuilder
                .AddParameter(paramName, x => x.SetType($"{deferred.InterfaceName}?"))
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide(GetLeftPropertyAssignment(fieldName))
                    .SetRighthandSide(paramName));
        }

        classBuilder.Build(writer);
    }
}
