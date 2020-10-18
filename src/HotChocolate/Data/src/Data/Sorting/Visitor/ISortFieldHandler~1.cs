using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    public interface ISortFieldHandler<in TContext>
        : ISortFieldHandler
        where TContext : ISortVisitorContext
    {
        /// <summary>
        /// This method is called when the <see cref="SortVisitor{TContext,T}"/> encounters a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="ISortVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="node">The value node of this field</param>
        /// <param name="action">
        /// The <see cref="ISyntaxVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleEnter(
            TContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);

        /// <summary>
        /// This method is called when the <see cref="SortVisitor{TContext,T}"/> leaves a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="ISortVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="node">The value node of this field</param>
        /// <param name="action">
        /// The <see cref="ISyntaxVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleLeave(
            TContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);
    }
}
