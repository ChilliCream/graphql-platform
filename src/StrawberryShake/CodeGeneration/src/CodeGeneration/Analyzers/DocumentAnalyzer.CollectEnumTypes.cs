using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers;

public partial class DocumentAnalyzer
{
    private static void CollectEnumTypes(IDocumentAnalyzerContext context)
    {
        var analyzer = new EnumTypeUsageAnalyzer((Schema)context.Schema);
        analyzer.Analyze(context.Document);

        foreach (var enumType in analyzer.EnumTypes)
        {
            RenameDirective? rename;
            var values = new List<EnumValueModel>();

            foreach (var enumValue in enumType.Values)
            {
                rename = enumValue.Directives.FirstOrDefault<RenameDirective>()?.AsValue<RenameDirective>();

                var value = enumValue.Directives.FirstOrDefault<EnumValueDirective>()?.AsValue<EnumValueDirective>();

                values.Add(new EnumValueModel(
                    rename?.Name ?? GetEnumValue(enumValue.Name),
                    enumValue.Description,
                    enumValue,
                    value?.Value));
            }

            rename = enumType.Directives.FirstOrDefault<RenameDirective>()?.AsValue<RenameDirective>();

            var serializationType =
                enumType.Directives.FirstOrDefault<SerializationTypeDirective>()?.AsValue<SerializationTypeDirective>();

            var typeName = context.ResolveTypeName(rename?.Name ?? GetClassName(enumType.Name));

            context.RegisterModel(
                typeName,
                new EnumTypeModel(
                    typeName,
                    enumType.Description,
                    enumType,
                    serializationType?.Name,
                    values));
        }
    }
}
