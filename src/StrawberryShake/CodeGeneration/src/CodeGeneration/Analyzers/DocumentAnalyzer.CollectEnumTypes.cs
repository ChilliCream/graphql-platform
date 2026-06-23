using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers;

public partial class DocumentAnalyzer
{
    private static void CollectEnumTypes(IDocumentAnalyzerContext context)
    {
        var analyzer = new EnumTypeUsageAnalyzer(context.Schema);
        analyzer.Analyze(context.Document);

        foreach (var enumType in analyzer.EnumTypes)
        {
            var values = new List<EnumValueModel>();

            foreach (var enumValue in enumType.Values)
            {
                var rename = enumValue.Directives.GetStringArgument("rename", "name");
                var value = enumValue.Directives.GetStringArgument("enumValue", "value");

                values.Add(new EnumValueModel(
                    rename ?? GetEnumValue(enumValue.Name),
                    enumValue.Description,
                    enumValue,
                    value));
            }

            var typeRename = enumType.Directives.GetStringArgument("rename", "name");
            var serializationType = enumType.Directives.GetStringArgument("serializationType", "name");

            var typeName = context.ResolveTypeName(typeRename ?? GetClassName(enumType.Name));

            context.RegisterModel(
                typeName,
                new EnumTypeModel(
                    typeName,
                    enumType.Description,
                    enumType,
                    serializationType,
                    values));
        }
    }
}
