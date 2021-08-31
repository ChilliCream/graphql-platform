using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Internal
{
    /// <summary>
    /// This interface represents an expression builder to resolver resolver parameter values.
    /// </summary>
    public interface IParameterExpressionBuilder
    {
        /// <summary>
        /// Defines the argument kind that is handled by this builder.
        /// </summary>
        ArgumentKind Kind { get; }

        /// <summary>
        /// Specifies if this expression builder can build parameter value resolvers
        /// for pure resolvers.
        /// </summary>
        bool IsPure { get; }

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
        bool CanHandle(ParameterInfo parameter);

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
        Expression Build(ParameterInfo parameter, Expression context);
    }
}
