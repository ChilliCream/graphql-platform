using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldHandler<in TContext>
        : IFilterFieldHandler
        where TContext : IFilterVisitorContext
    {
        /// <summary>
        /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> encounters a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="IFilterVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="node">The value node of this field</param>
        /// <param name="action">
        /// The <see cref="ISyntaxVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleEnter(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);

        /// <summary>
        /// This method is called when the <see cref="FilterVisitor{TContext,T}"/> leaves a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="IFilterVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="node">The value node of this field</param>
        /// <param name="action">
        /// The <see cref="ISyntaxVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleLeave(
            TContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);
    }
}
