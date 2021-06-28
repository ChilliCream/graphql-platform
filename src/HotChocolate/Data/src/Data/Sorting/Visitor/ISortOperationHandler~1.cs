using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Sorting
{
    /// <inheritdoc/>
    public interface ISortOperationHandler<in TContext>
        : ISortOperationHandler
        where TContext : ISortVisitorContext
    {
        /// <summary>
        /// This method is called when the <see cref="SortVisitor{TContext,T}"/> encounters a
        /// field
        /// </summary>
        /// <param name="context">The <see cref="ISortVisitorContext{T}"/> of the visitor</param>
        /// <param name="field">The field that is currently being visited</param>
        /// <param name="enumValue">The sort value of the field</param>
        /// <param name="valueNode">The value node of the field</param>
        /// <param name="action">
        /// The <see cref="ISyntaxVisitorAction"/> that the visitor should
        /// continue with
        /// </param>
        /// <returns>If <c>true</c> is returned the action is used for further processing</returns>
        bool TryHandleEnter(
            TContext context,
            ISortField field,
            ISortEnumValue? enumValue,
            EnumValueNode valueNode,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action);
    }
}
