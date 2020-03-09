using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal class InterfaceTypeSelectionSetAnalyzer
        : SelectionSetAnalyzerBase<InterfaceType>
    {
        public override void Analyze(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            IType fieldType,
            InterfaceType namedType,
            Path path)
        {
            IFragmentNode returnTypeFragment =
                ResolveReturnType(
                    namedType,
                    fieldSelection,
                    possibleSelections.ReturnType);

            ComplexOutputTypeModel returnType =
                CreateTypeModel(
                    context,
                    returnTypeFragment,
                    path);

            CreateClassModels(
                context,
                operation,
                fieldType,
                fieldSelection,
                possibleSelections,
                returnTypeFragment,
                returnType,
                path);
        }

        private void CreateClassModels(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            IType fieldType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            IFragmentNode returnTypeFragment,
            ComplexOutputTypeModel returnType,
            Path path)
        {
            IReadOnlyCollection<SelectionInfo> selections = possibleSelections.Variants;
            var resultParserTypes = new List<ComplexOutputTypeModel>();

            if (selections.Count == 1)
            {
                /*
                CreateClassModel(
                    context,
                    returnTypeFragment,
                    returnType,
                    selections.Single(),
                    resultParserTypes);*/
            }
            else
            {
                var interfaces = new List<ComplexOutputTypeModel>();

                foreach (SelectionInfo selection in selections)
                {
                    IFragmentNode modelType = ResolveReturnType(
                        selection.Type,
                        fieldSelection,
                        selection);

                    foreach (IFragmentNode fragment in ShedNonMatchingFragments(selection.Type, modelType))
                    {
                        interfaces.Add(CreateTypeModel(context, fragment, path));
                    }

                    interfaces.Insert(0, returnType);

                    NameString typeName = HoistName(selection.Type, modelType);
                    if (typeName.IsEmpty)
                    {
                        typeName = selection.Type.Name;
                    }

                    var fieldNames = new HashSet<string>(
                        selection.Fields.Select(t => GetPropertyName(t.ResponseName)));

                    string className = context.GetOrCreateName(
                        modelType.Fragment.SelectionSet,
                        GetClassName(typeName),
                        fieldNames);




                    /*
                    modelClass = new ClassDescriptor(
                        className,
                        context.Namespace,
                        selection.Type,
                        interfaces);
                        */

                    // context.Register(modelClass, update);
                    // resultParserTypes.Add(new ResultParserTypeDescriptor(modelClass));
                }
            }

/*
            context.Register(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldSelection,
                    path,
                    returnType,
                    resultParserTypes));
                    */
        }

    }
}
