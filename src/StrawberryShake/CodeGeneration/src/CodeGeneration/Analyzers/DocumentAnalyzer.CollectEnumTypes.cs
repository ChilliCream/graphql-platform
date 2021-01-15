using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer
    {
        private static void CollectEnumTypes(
            IDocumentAnalyzerContext context)
        {
            var analyzer = new EnumTypeUsageAnalyzer(context.Schema);
            analyzer.Analyze(context.Document);

            foreach (EnumType enumType in analyzer.EnumTypes)
            {
                RenameDirective? rename;
                var values = new List<EnumValueModel>();

                foreach (IEnumValue enumValue in enumType.Values)
                {
                    rename = enumValue.Directives.SingleOrDefault<RenameDirective>();

                    EnumValueDirective? value =
                        enumValue.Directives.SingleOrDefault<EnumValueDirective>();

                    values.Add(new EnumValueModel(
                        GetClassName(rename?.Name ?? enumValue.Name),
                        enumValue.Description,
                        enumValue,
                        value?.Value));
                }

                rename = enumType.Directives.SingleOrDefault<RenameDirective>();

                SerializationTypeDirective? serializationType =
                    enumType.Directives.SingleOrDefault<SerializationTypeDirective>();

                NameString typeName = context.ResolveTypeName(
                    GetClassName(rename?.Name ?? enumType.Name));

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
}
