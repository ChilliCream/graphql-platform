using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using HotChocolate.Types.Sorting.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting.Conventions
{
    public class SortingExpressionVisitorDefinition
        : SortingVisitorDefinitionBase
    {
        public SortOperationFactory OperationFactory { get; set; }
            = CreateSortOperationDefault.CreateSortOperation;

        public SortCompiler Compiler { get; set; }
            = SortCompilerDefault.Compile;

        public async override Task ApplySorting<T>(
            ISortingConvention convention,
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context)
        {
            await next(context).ConfigureAwait(false);

            IValueNode sortArgument = context.Argument<IValueNode>(convention.GetArgumentName());

            if (sortArgument is null || sortArgument is NullValueNode)
            {
                return;
            }

            IQueryable<T>? source = null;
            PageableData<T>? p = null;

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            if (context.Result is PageableData<T> pb)
            {
                source = pb.Source;
                p = pb;
            }

            if (source != null &&
                context.Field.Arguments[convention.GetArgumentName()].Type is InputObjectType iot &&
                iot is ISortInputType fit &&
                fit.EntityType is { })
            {
                var visitorCtx = new QueryableSortVisitorContext(
                    iot,
                    fit.EntityType,
                    source is EnumerableQuery,
                    this);

                QueryableSortVisitor.Default.Visit(sortArgument, visitorCtx);

                source = visitorCtx.Sort(source);
                context.Result = p is null
                    ? (object)source
                    : new PageableData<T>(source, p.Properties);
            }
        }
    }
}
