using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// The default handler for all <see cref="FilterField"/> for the
    /// <see cref="SqlKataFilterProvider"/>
    /// </summary>
    public class SqlKataDefaultFieldHandler
        : FilterFieldHandler<SqlKataFilterVisitorContext, Query>
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
            SqlKataFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(ErrorHelper.CreateNonNullError(field, node.Value, context));

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (field.RuntimeType is null)
            {
                action = null;
                return false;
            }

            SqlKataFilterScope scope = context.GetSqlKataFilterScope();
            string tableName = context.GetTableName(field);
            string alias = context.GetTableName(field);
            if (scope.TableInfo.TryPeekElement(out var tableInfo))
            {
                //alias = context.GetTableAlias();
                if (field.HasForeignKey())
                {
                    context
                        .GetInstance()
                        .LeftJoin(
                            $"{tableName} as {alias}",
                            $"{tableInfo.Alias}.{context.GetKey(scope.Fields.Peek())}",
                            $"{alias}.{field.GetForeignKey()}");
                }
                else
                {
                    context
                        .GetInstance()
                        .LeftJoin(
                            tableName,
                         //   $"{tableName} as {alias}",
                            $"{tableInfo.Alias}.{scope.Fields.Peek().GetForeignKey()}",
                            $"{alias}.{context.GetKey(scope.Fields.Peek())}");
                }
            }
            else
            {
                context.GetInstance().From(tableName);
            }

            scope.Fields.Push(field);
            scope.TableInfo.Push(new TableInfo(tableName, alias));
            context.RuntimeTypes.Push(field.RuntimeType);
            action = SyntaxVisitor.Continue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryHandleLeave(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.RuntimeTypes.Pop();
            SqlKataFilterScope scope = context.GetSqlKataFilterScope();
            scope.Fields.Pop();
            scope.TableInfo.Pop();

            action = SyntaxVisitor.Continue;
            return true;
        }
    }
}
