using System.Linq;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class DataGeneratorContext
    {
        public DataGeneratorContext(
            OperationDirective operation,
            PagingDirective paging,
            bool filtering,
            bool sorting)
        {
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
                schema.Directives["operation"].FirstOrDefault()?.ToObject<OperationDirective>() ??
                new OperationDirective(new [] { OperationKind.All }),
                schema.Directives["operation"].FirstOrDefault()?.ToObject<PagingDirective>() ??
                new PagingDirective(PagingKind.Cursor, 10, 50, false),
                true,
                true);

        public static DataGeneratorContext FromMember(
            HotChocolate.Types.IHasDirectives member,
            DataGeneratorContext rootContext) =>
            new DataGeneratorContext(
                member.Directives["operation"].FirstOrDefault()?.ToObject<OperationDirective>() ??
                rootContext.Operation,
                member.Directives["operation"].FirstOrDefault()?.ToObject<PagingDirective>() ??
                rootContext.Paging,
                true,
                true);
    }
}
