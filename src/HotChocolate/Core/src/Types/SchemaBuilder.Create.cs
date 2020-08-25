namespace HotChocolate
{
    public partial class SchemaBuilder
    {
        public Schema Create() => Setup.Create(this);

        ISchema ISchemaBuilder.Create() => Create();
    }
}
