using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers;

public partial class DocumentAnalyzer
{
    private static void CollectInputObjectTypes(IDocumentAnalyzerContext context)
    {
        var analyzer = new InputObjectTypeUsageAnalyzer(context.Schema);
        analyzer.Analyze(context.Document);

        var namesOfInputTypesWithUploadScalar = CollectTypesWithUploadScalar(analyzer);

        foreach (var namedInputType in analyzer.InputTypes)
        {
            if (namedInputType is IInputObjectTypeDefinition inputObjectType)
            {
                RegisterInputObjectType(
                    context,
                    inputObjectType,
                    namesOfInputTypesWithUploadScalar.Contains(namedInputType.Name));
            }
            else if (namedInputType.IsLeafType())
            {
                context.RegisterType(namedInputType);
            }
        }
    }

    private static void RegisterInputObjectType(
        IDocumentAnalyzerContext context,
        IInputObjectTypeDefinition inputObjectType,
        bool hasUpload)
    {
        var fields = new List<InputFieldModel>();

        foreach (var inputField in inputObjectType.Fields)
        {
            var rename = inputField.Directives.GetStringArgument("rename", "name");

            fields.Add(new InputFieldModel(
                GetClassName(rename ?? inputField.Name),
                inputField.Description,
                inputField,
                inputField.DefaultValue is not null
                    ? (IInputType)inputField.Type.NullableType()
                    : inputField.Type
            ));

            context.RegisterType(inputField.Type.NamedType());
        }

        var typeRename = inputObjectType.Directives.GetStringArgument("rename", "name");

        var typeName = context.ResolveTypeName(
            GetClassName(typeRename ?? inputObjectType.Name));

        context.RegisterModel(
            typeName,
            new InputObjectTypeModel(
                typeName,
                inputObjectType.Description,
                inputObjectType,
                hasUpload,
                fields));
    }

    private static HashSet<string> CollectTypesWithUploadScalar(
        InputObjectTypeUsageAnalyzer analyzer)
    {
        var namesOfInputTypesWithUploadScalar = new HashSet<string>();
        var detected = true;
        while (detected)
        {
            detected = false;
            foreach (var namedInputType in analyzer.InputTypes)
            {
                if (namedInputType is not ITypeDefinition { Name: { } typeName }
                    || namesOfInputTypesWithUploadScalar.Contains(typeName))
                {
                    continue;
                }

                if (namedInputType is IInputObjectTypeDefinition type)
                {
                    foreach (var field in type.Fields)
                    {
                        if (namesOfInputTypesWithUploadScalar.Contains(field.Type.NamedType().Name))
                        {
                            detected = true;
                            namesOfInputTypesWithUploadScalar.Add(typeName);
                            break;
                        }
                    }
                }
                else if (namedInputType is IScalarTypeDefinition { Name: "Upload" })
                {
                    detected = true;
                    namesOfInputTypesWithUploadScalar.Add("Upload");
                    break;
                }
            }
        }

        return namesOfInputTypesWithUploadScalar;
    }
}
