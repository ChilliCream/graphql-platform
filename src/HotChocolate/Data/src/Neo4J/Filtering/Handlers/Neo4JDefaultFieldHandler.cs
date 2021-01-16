using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Extensions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <summary>
    /// The default handler for all <see cref="FilterField"/> for the
    /// <see cref="Neo4JFilterProvider"/>
    /// </summary>
    public class Neo4JDefaultFieldHandler
        : FilterFieldHandler<Neo4JFilterVisitorContext, Neo4JFilterDefinition>
    {
        /// <summary>
        /// Checks if the field not a filter operations field
        /// </summary>
        /// <param name="context">The current context</param>
        /// <param name="typeDefinition">The definition of the type that declares the field</param>
        /// <param name="fieldDefinition">The definition of the field</param>
        /// <returns>True in case the field can be handled</returns>
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition) =>
            !(fieldDefinition is FilterOperationFieldDefinition);

        /// <inheritdoc />
        public override bool TryHandleEnter(
            Neo4JFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                // TODO: Implement error
                //context.ReportError(ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            context.GetNeo4JFilterScope().Path.Push(field.GetName());
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor.Continue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryHandleLeave(
            Neo4JFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.RuntimeTypes.Pop();
            context.GetNeo4JFilterScope().Path.Pop();

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}
