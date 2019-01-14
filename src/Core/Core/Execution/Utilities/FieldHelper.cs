using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class FieldHelper
        : IFieldHelper
    {
        private readonly FieldCollector _fieldCollector;
        private readonly DirectiveLookup _directives;
        private readonly ICollection<IError> _errors;

        public FieldHelper(
            FieldCollector fieldCollector,
            DirectiveLookup directives,
            IVariableCollection variables,
            ICollection<IError> errors)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            _fieldCollector = fieldCollector; ;
            _errors = errors;
            _directives = directives;
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType, SelectionSetNode selectionSet)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            return _fieldCollector.CollectFields(
                objectType, selectionSet,
                error => _errors.Add(error));
        }

        public FieldDelegate CreateMiddleware(
            ObjectType objectType, FieldNode fieldNode)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (fieldNode == null)
            {
                throw new ArgumentNullException(nameof(fieldNode));
            }

            return _directives.GetMiddleware(objectType, fieldNode);
        }
    }
}
