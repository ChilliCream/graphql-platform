using System.Collections.Generic;
using System.Linq;
using Prometheus.Abstractions;

namespace Prometheus.Validation
{
    /*
    public class FieldSelectionMustBeDefinedRule
        : IQueryValidationRule
    {
        public string Code { get; } = "Q521";

        public string Description { get; } = "The target field of a field "
            + "selection must be defined on the scoped type of the "
            + "selection set. There are no limitations on alias names.";

        public IEnumerable<IValidationResult> Apply(
            ISchemaDocument schemaDocument,
            IQueryDocument queryDocument)
        {
            List<IValidationResult> errors = new List<IValidationResult>();

            foreach (OperationDefinition operation in
                queryDocument.OfType<ObjectTypeDefinition>())
            {
                errors.AddRange(ValidateOperation(
                    schemaDocument, queryDocument, operation));
            }
        }

        private IEnumerable<IHasSelectionSet> GetNodesWithSelectionSet()
        {
            yield break;
        }



        private IEnumerable<IValidationResult> ValidateOperation(
            ISchemaDocument schemaDocument,
            IQueryDocument queryDocument,
            OperationDefinition operation)
        {
            Queue<TypeAndSelection> queue = new Queue<TypeAndSelection>();
            EnqueueSelections(queue, operation.Type.ToString(), operation.SelectionSet);

            while (queue.Any())
            {
                var current = queue.Dequeue();
                if (current.Selection is Field f)
                {
                    if (TryGetFieldDefinition(
                        schemaDocument, current.Type, f.Name,
                        out var fieldDefinition))
                    {
                        EnqueueSelections(queue,
                            fieldDefinition.Type.TypeName(),
                            f.SelectionSet);
                    }
                    else
                    {
                        yield return CreateError(current.Type, f.Name);
                    }
                }
                else if (current.Selection is InlineFragment i)
                {
                    EnqueueSelections(queue,
                        i.TypeCondition.Name,
                        i.SelectionSet);
                }
            }
        }

        private void EnqueueSelections(Queue<TypeAndSelection> queue,
            string typeName, ISelectionSet selectionSet)
        {
            foreach (ISelection selection in selectionSet)
            {
                queue.Enqueue(new TypeAndSelection(typeName, selection));
            }
        }

        private class TypeAndSelection
        {
            public TypeAndSelection(string typeName, ISelection selection)
            {
                Type = typeName;
                Selection = selection;
            }
            public string Type { get; }
            public ISelection Selection { get; }
        }

        private bool TryGetFieldDefinition(
            ISchemaDocument schemaDocument,
            string typeName, string fieldName,
            out FieldDefinition fieldDefinition)
        {
            if (schemaDocument.ObjectTypes.TryGetValue(
                typeName, out var objectType))
            {
                return objectType.Fields.TryGetValue(
                    fieldName, out fieldDefinition);
            }

            if (schemaDocument.InterfaceTypes.TryGetValue(
                typeName, out var interfaceType))
            {
                return interfaceType.Fields.TryGetValue(
                    fieldName, out fieldDefinition);
            }

            fieldDefinition = null;
            return false;
        }

        private ErrorResult CreateError(string typeName, string fieldName)
        {
            return new ErrorResult(this,
                $"The field \"{fieldName}\" does not exist on {fieldName}.");
        }
    }
     */
}