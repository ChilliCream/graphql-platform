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
        private readonly Func<FieldSelection, FieldDelegate> _middlewareRes;
        private readonly Action<IError> _reportError;

        public FieldHelper(
            FieldCollector fieldCollector,
            Func<FieldSelection, FieldDelegate> middlewareResolver,
            Action<IError> reportError)
        {
            _fieldCollector = fieldCollector
                ?? throw new ArgumentNullException(nameof(fieldCollector));
            _reportError = reportError
                ?? throw new ArgumentNullException(nameof(reportError));
            _middlewareRes = middlewareResolver
                ?? throw new ArgumentNullException(nameof(middlewareResolver));
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType,
            SelectionSetNode selectionSet)
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
                objectType, selectionSet, _reportError);
        }

        public FieldDelegate CreateMiddleware(
            FieldSelection fieldSelection)
        {
            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            return _middlewareRes.Invoke(fieldSelection);
        }
    }
}
