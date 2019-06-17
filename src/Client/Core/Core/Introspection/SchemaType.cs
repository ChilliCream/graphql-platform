using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Client.Core.Builders;

namespace HotChocolate.Client.Core.Introspection
{
    public class SchemaType : QueryableValue<SchemaType>, IQuery, IQueryableValue<SchemaType>
    {
        public SchemaType(Expression expression)
            : base(expression)
        {
        }

        public TypeKind Kind { get; }
        public string Name { get; }
        public string Description { get; }
        public IQueryableList<SchemaType> Interfaces => this.CreateProperty(x => Interfaces);
        public IQueryableList<SchemaType> PossibleTypes => this.CreateProperty(x => x.PossibleTypes);
        public IQueryableList<InputValue> InputFields => this.CreateProperty(x => x.InputFields);
        public SchemaType OfType => this.CreateProperty(x => x.OfType, SchemaType.Create);

        public IQueryableList<Field> Fields(bool includeDeprecated = false)
        {
            return this.CreateMethodCall(x => x.Fields(includeDeprecated));
        }

        public IQueryableList<EnumValue> EnumValues(bool includeDeprecated = false)
        {
            return this.CreateMethodCall(x => x.EnumValues(includeDeprecated));
        }

        internal static SchemaType Create(Expression expression)
        {
            return new SchemaType(expression);
        }
    }
}
