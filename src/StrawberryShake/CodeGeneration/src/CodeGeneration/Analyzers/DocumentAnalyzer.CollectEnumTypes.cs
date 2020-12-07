using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public partial class DocumentAnalyzer
    {
        private static void CollectEnumTypes(
            IDocumentAnalyzerContext context,
            DocumentNode document)
        {
            var analyzer = new EnumTypeUsageAnalyzer(context.Schema);
            analyzer.Analyze(document);

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
                        enumValue,
                        enumValue.Description,
                        value?.Value));
                }

                rename = enumType.Directives.SingleOrDefault<RenameDirective>();

                SerializationTypeDirective? serializationType =
                    enumType.Directives.SingleOrDefault<SerializationTypeDirective>();

                context.Register(new EnumTypeModel(
                    GetClassName(rename?.Name ?? enumType.Name),
                    enumType.Description,
                    enumType,
                    serializationType?.Name,
                    values));
            }
        }
    }
}
