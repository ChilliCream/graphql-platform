using System.Linq;

namespace HotChocolate.Analyzers.Types
{
    public class DataGeneratorContext
    {
        public DataGeneratorContext(
            string queryTypeName,
            OperationDirective operation,
            PagingDirective paging,
            bool filtering,
            bool sorting)
        {
            QueryTypeName = queryTypeName;
            Operation = operation;
            Paging = paging;
            Filtering = filtering;
            Sorting = sorting;
        }

        public string QueryTypeName { get; }

        public OperationDirective Operation { get; }

        public PagingDirective Paging { get; }

        public bool Filtering { get; }

        public bool Sorting { get; }

        public static DataGeneratorContext FromSchema(
            ISchema schema) =>
            new DataGeneratorContext(
                null!,
                schema.GetFirstDirective<OperationDirective>(
                    "operation",
                    new OperationDirective { Operations = new[] { OperationKind.All } })!,
                schema.GetFirstDirective<PagingDirective>(
                    "paging", 
                    new PagingDirective
                    { 
                        Kind = PagingKind.Cursor, 
                        DefaultPageSize = 10, 
                        MaxPageSize = 50, 
                        IncludeTotalCount = false 
                    })!,
                schema.Directives["filtering"].Any(),
                schema.Directives["sorting"].Any());

        public static DataGeneratorContext FromMember(
            HotChocolate.Types.IHasDirectives member,
            DataGeneratorContext rootContext) =>
            new DataGeneratorContext(
                rootContext.QueryTypeName,
                member.GetFirstDirective<OperationDirective>(
                    "operation",
                    rootContext.Operation)!,
                member.GetFirstDirective<PagingDirective>(
                    "paging", 
                    rootContext.Paging)!,
                member.Directives["filtering"].Any() || rootContext.Filtering,
                member.Directives["sorting"].Any() || rootContext.Sorting);
    }
}
