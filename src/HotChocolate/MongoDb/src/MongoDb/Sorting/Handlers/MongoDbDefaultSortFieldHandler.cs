using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.MongoDb.Data;
using HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
    public class MongoDbDefaultSortFieldHandler
        : SortFieldHandler<MongoDbSortVisitorContext, MongoDbSortDefinition>
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeDefinition typeDefinition,
            ISortFieldDefinition fieldDefinition) =>
            fieldDefinition.Member is not null;

        public override bool TryHandleEnter(
            MongoDbSortVisitorContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

            context.Path.Push(field.GetName());
            action = SyntaxVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            MongoDbSortVisitorContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.Path.Pop();

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}
