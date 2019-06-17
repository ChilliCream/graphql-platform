using System;
using System.Linq.Expressions;
using HotChocolate.Client.Core.Builders;

namespace HotChocolate.Client.Core.Introspection
{
    public class Schema : QueryableValue<Schema>, IQueryableValue<Schema>
    {
        public Schema(Expression expression)
            : base(expression)
        {
        }

        public IQueryableList<SchemaType> Types => this.CreateProperty(x => x.Types);
        public SchemaType QueryType => this.CreateProperty(x => x.QueryType, SchemaType.Create);
        public SchemaType MutationType => this.CreateProperty(x => x.MutationType, SchemaType.Create);
        public IQueryableList<Directive> Directives => this.CreateProperty(x => x.Directives);
        public Schema Value { get; }

        internal static Schema Create(Expression expression)
        {
            return new Schema(expression);
        }
    }
}
