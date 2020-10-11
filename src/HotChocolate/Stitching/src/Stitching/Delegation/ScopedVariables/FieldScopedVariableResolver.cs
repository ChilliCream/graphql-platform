using System;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal class FieldScopedVariableResolver
        : IScopedVariableResolver
    {
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

            if (context.ObjectType.Fields.TryGetField(variable.Name.Value, out IObjectField field))
            {
                object parent = context.Parent<object>();
                ObjectValueNode objectLiteral =
                    parent is ObjectValueNode o
                        ? o :
                        (ObjectValueNode)targetType.ParseValue(parent);

                ObjectFieldNode? fieldLiteral =
                    objectLiteral.Fields.FirstOrDefault(
                        t => t.Name.Value.EqualsOrdinal(field.Name));

                return new VariableValue
                (
                    variable.ToVariableName(),
                    targetType.ToTypeNode(),
                    fieldLiteral?.Value ?? NullValueNode.Default,
                    null
                );
            }

            throw new QueryException(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.FieldScopedVariableResolver_InvalidFieldName,
                    variable.Name.Value)
                .SetCode(ErrorCodes.Stitching.FieldNotDefined)
                .SetPath(context.Path)
                .AddLocation(context.FieldSelection)
                .Build());
        }
    }
}
