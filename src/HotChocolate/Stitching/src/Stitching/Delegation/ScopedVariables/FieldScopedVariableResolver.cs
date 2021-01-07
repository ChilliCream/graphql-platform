using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal class FieldScopedVariableResolver
        : IScopedVariableResolver
    {
        public ScopedVariableValue Resolve(
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
                    FieldScopedVariableResolver_CannotHandleVariable,
                    nameof(variable));
            }

            if (context.ObjectType.Fields.TryGetField(variable.Name.Value, out IObjectField? field))
            {
                object parent = context.Parent<object>();

                IValueNode? valueLiteral = null;

                if (parent is IReadOnlyDictionary<string, object> dict &&
                    dict.TryGetValue(field.Name, out object? value))
                {
                    if (value is IValueNode v)
                    {
                        valueLiteral = v;
                    }
                    else if(field.Type.IsInputType() && field.Type is IInputType type)
                    {
                        valueLiteral = type.ParseValue(value);
                    }
                }

                return new ScopedVariableValue
                (
                    variable.ToVariableName(),
                    targetType.ToTypeNode(),
                    valueLiteral ?? NullValueNode.Default,
                    null
                );
            }

            throw ThrowHelper.FieldScopedVariableResolver_InvalidFieldName(
                variable.Name.Value,
                context.Selection.SyntaxNode,
                context.Path);
        }
    }
}
