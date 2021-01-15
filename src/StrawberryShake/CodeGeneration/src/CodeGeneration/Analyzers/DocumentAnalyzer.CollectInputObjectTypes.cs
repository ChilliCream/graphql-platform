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
        private static void CollectInputObjectTypes(
            IDocumentAnalyzerContext context)
        {
            var analyzer = new InputObjectTypeUsageAnalyzer(context.Schema);
            analyzer.Analyze(context.Document);

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

                NameString typeName = context.ResolveTypeName(
                    GetClassName(rename?.Name ?? inputObjectType.Name));

                context.RegisterModel(
                    typeName,
                    new InputObjectTypeModel(
                        GetClassName(rename?.Name ?? inputObjectType.Name),
                        inputObjectType.Description,
                        inputObjectType,
                        fields));
            }
        }
    }
}
