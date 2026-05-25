# Filter_With_Multi_Expression

## SQL

```text

```

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "path": [
        "multiFilterExpression"
      ],
      "extensions": {
        "exception": {
          "message": "The LINQ expression 'DbSet<Brand>()\n    .Where(b => __keys_0\n        .Contains(b.Id))\n    .Where(b => b.Name.StartsWith(\"Brand\") && b.Name.EndsWith(0))' could not be translated. Additional information: Translation of method 'string.EndsWith' failed. If this method can be mapped to your custom function, see https://go.microsoft.com/fwlink/?linkid=2132413 for more information. Either rewrite the query in a form that can be translated, or switch to client evaluation explicitly by inserting a call to 'AsEnumerable', 'AsAsyncEnumerable', 'ToList', or 'ToListAsync'. See https://go.microsoft.com/fwlink/?linkid=2101038 for more information.",
          "stackTrace": "   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.Translate(Expression expression)\n   at Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor.Translate(Expression expression)\n   at Microsoft.EntityFrameworkCore.Query.QueryCompilationContext.CreateQueryExecutor[TResult](Expression query)\n   at Microsoft.EntityFrameworkCore.Storage.Database.CompileQuery[TResult](Expression query, Boolean async)\n   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.CompileQueryCore[TResult](IDatabase database, Expression query, IModel model, Boolean async)\n   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.<>c__DisplayClass9_0`1.<Execute>b__0()\n   at Microsoft.EntityFrameworkCore.Query.Internal.CompiledQueryCache.GetOrAddQuery[TResult](Object cacheKey, Func`1 compiler)\n   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.Execute[TResult](Expression query)\n   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider.Execute[TResult](Expression expression)\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToQueryString(IQueryable source)\n   at HotChocolate.Data.Predicates.DataLoaderTests.BrandByIdDataLoader.LoadBatchAsync(IReadOnlyList`1 keys, DataLoaderFetchContext`1 context, CancellationToken cancellationToken) in /workspaces/repo/src/HotChocolate/Data/test/Data.Filters.SqlServer.Tests/DataLoaderTests.cs:line 275\n   at GreenDonut.StatefulBatchDataLoader`2.FetchAsync(IReadOnlyList`1 keys, Memory`1 results, DataLoaderFetchContext`1 context, CancellationToken cancellationToken) in /workspaces/repo/src/GreenDonut/src/GreenDonut/BatchDataLoader.cs:line 116\n   at GreenDonut.DataLoaderBase`2.<>c__DisplayClass34_0.<<DispatchBatchAsync>g__StartDispatchingAsync|0>d.MoveNext() in /workspaces/repo/src/GreenDonut/src/GreenDonut/DataLoaderBase.cs:line 372\n--- End of stack trace from previous location ---\n   at HotChocolate.Data.Predicates.DataLoaderTests.Query.MultiFilterExpression(Int32 id, BrandByIdDataLoader brandById, CancellationToken cancellationToken) in /workspaces/repo/src/HotChocolate/Data/test/Data.Filters.SqlServer.Tests/DataLoaderTests.cs:line 211\n   at HotChocolate.Resolvers.Expressions.ExpressionHelper.AwaitTaskHelper[T](Task`1 task) in /workspaces/repo/src/HotChocolate/Core/src/Types/Resolvers/Expressions/ExpressionHelper.cs:line 16\n   at HotChocolate.Types.Helpers.FieldMiddlewareCompiler.<>c__DisplayClass9_0.<<CreateResolverMiddleware>b__0>d.MoveNext() in /workspaces/repo/src/HotChocolate/Core/src/Types/Types/Helpers/FieldMiddlewareCompiler.cs:line 127\n--- End of stack trace from previous location ---\n   at HotChocolate.Execution.Processing.Tasks.ResolverTask.ExecuteResolverPipelineAsync(CancellationToken cancellationToken) in /workspaces/repo/src/HotChocolate/Core/src/Types/Execution/Processing/Tasks/ResolverTask.Execute.cs:line 135\n   at HotChocolate.Execution.Processing.Tasks.ResolverTask.TryExecuteAsync(CancellationToken cancellationToken) in /workspaces/repo/src/HotChocolate/Core/src/Types/Execution/Processing/Tasks/ResolverTask.Execute.cs:line 81"
        }
      }
    }
  ],
  "data": {
    "multiFilterExpression": null
  }
}
```
