using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Stitching
{
    public class FieldDependencyResolverTests
    {
        [Fact]
        public void GetFieldDependencies()
        {
            ISchema schema = Schema.Create(
                FileResource.Open("Stitching.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.RegisterDirective<SchemaDirectiveType>();
                    c.Use(next => context => Task.CompletedTask);
                });

            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("StitchingQuery.graphql"));

            FieldNode selection = query.Definitions
                .OfType<OperationDefinitionNode>().Single()
                .SelectionSet.Selections
                .OfType<FieldNode>().Single();

            var fieldDependencyResolver = new FieldDependencyResolver(schema);
            ISet<string> dependencies = fieldDependencyResolver
                .GetFieldDependencies(
                    query,
                    selection,
                    schema.GetType<ObjectType>("Customer"));

            dependencies.Snapshot();
        }
    }
}
