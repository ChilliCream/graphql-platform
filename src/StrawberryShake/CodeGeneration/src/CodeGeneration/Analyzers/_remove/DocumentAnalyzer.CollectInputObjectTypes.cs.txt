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
        private static void CollectInputObjectTypes(
            IDocumentAnalyzerContext context,
            DocumentNode document)
        {
            var analyzer = new InputObjectTypeUsageAnalyzer(context.Schema);
            analyzer.Analyze(document);

            foreach (InputObjectType inputObjectType in analyzer.InputObjectTypes)
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
                        inputField.Type));
                }

                rename = inputObjectType.Directives.SingleOrDefault<RenameDirective>();

                context.Register(new InputObjectTypeModel(
                    GetClassName(rename?.Name ?? inputObjectType.Name),
                    inputObjectType.Description,
                    inputObjectType,
                    fields));
            }
        }
    }
}
