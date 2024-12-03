using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
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
            if (namedInputType is InputObjectType inputObjectType)
            {
                RegisterInputObjectType(
                    context,
                    inputObjectType,
                    namesOfInputTypesWithUploadScalar.Contains(namedInputType.Name));
            }
            else if (namedInputType is ILeafType)
            {
                context.RegisterType(namedInputType);
            }
        }
    }

    private static void RegisterInputObjectType(
        IDocumentAnalyzerContext context,
        InputObjectType inputObjectType,
        bool hasUpload)
    {
        RenameDirective? rename;
        var fields = new List<InputFieldModel>();

        foreach (IInputField inputField in inputObjectType.Fields)
        {
            rename = inputField.Directives.SingleOrDefault<RenameDirective>();

            fields.Add(new InputFieldModel(
                GetClassName(rename?.Name ?? inputField.Name),
                inputField.Description,
                inputField,
                inputField.DefaultValue is not null
                    ? (IInputType)inputField.Type.NullableType()
                    : inputField.Type
            ));

            context.RegisterType(inputField.Type.NamedType());
        }

        rename = inputObjectType.Directives.SingleOrDefault<RenameDirective>();

        var typeName = context.ResolveTypeName(
            GetClassName(rename?.Name ?? inputObjectType.Name));

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
                if (namedInputType is not INamedType { Name: { } typeName, } ||
                    namesOfInputTypesWithUploadScalar.Contains(typeName))
                {
                    continue;
                }

                if (namedInputType is InputObjectType type)
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
                else if (namedInputType is ScalarType { Name: "Upload", })
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
