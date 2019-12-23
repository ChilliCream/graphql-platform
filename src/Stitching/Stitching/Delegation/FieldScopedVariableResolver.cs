using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    internal class FieldScopedVariableResolver
        : IScopedVariableResolver
    {
        private readonly DictionaryToObjectValueConverter _converter =
            new DictionaryToObjectValueConverter();

        public VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            IInputType targetType)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (!ScopeNames.Fields.Equals(variable.Scope.Value))
            {
                throw new ArgumentException(
                    StitchingResources
                        .FieldScopedVariableResolver_CannotHandleVariable,
                    nameof(variable));
            }

            if (context.ObjectType.Fields.TryGetField(variable.Name.Value,
                out ObjectField field))
            {
                IReadOnlyDictionary<string, object> obj =
                    context.Parent<IReadOnlyDictionary<string, object>>();

                return new VariableValue
                (
                    variable.ToVariableName(),
                    targetType.ToTypeNode(),
                    _converter.Convert(obj[field.Name], targetType, variable.Value),
                    null
                );
            }

            throw new QueryException(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.FieldScopedVariableResolver_InvalidFieldName,
                    variable.Name.Value)
                .SetCode(ErrorCodes.FieldNotDefined)
                .SetPath(context.Path)
                .AddLocation(context.FieldSelection)
                .Build());
        }
    }
}
