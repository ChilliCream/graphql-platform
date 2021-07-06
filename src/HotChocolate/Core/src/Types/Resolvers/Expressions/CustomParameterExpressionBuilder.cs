using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions
{
    /// <summary>
    /// A custom parameter expression builder allows to implement custom resolver parameter
    /// injection logic.
    /// </summary>
    public abstract class CustomParameterExpressionBuilder : IParameterExpressionBuilder
    {
        ArgumentKind IParameterExpressionBuilder.Kind => ArgumentKind.Custom;

        bool IParameterExpressionBuilder.IsPure => false;

        /// <summary>
        /// Checks if this expression builder can handle the following parameter.
        /// </summary>
        /// <param name="parameter">
        /// The parameter that needs to be resolved.
        /// </param>
        /// <param name="source">
        /// The runtime type of the object that is being resolved.
        /// </param>
        /// <returns>
        /// <c>true</c> if the parameter can be handled by this expression builder;
        /// otherwise <c>false</c>.
        /// </returns>
        public abstract bool CanHandle(ParameterInfo parameter, Type source);

        /// <summary>
        /// Builds an expression that resolves a resolver parameter.
        /// </summary>
        /// <param name="parameter">
        /// The parameter that needs to be resolved.
        /// </param>
        /// <param name="source">
        /// The runtime type of the object that is being resolved.
        /// </param>
        /// <param name="context">
        /// An expression that represents the resolver context.
        /// </param>
        /// <returns>
        /// Returns an expression that resolves the value for this <paramref name="parameter"/>.
        /// </returns>
        public abstract Expression Build(ParameterInfo parameter, Type source, Expression context);
    }
}
