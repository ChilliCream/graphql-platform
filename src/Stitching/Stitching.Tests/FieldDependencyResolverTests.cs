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
            // arrange
            ISchema schema = Schema.Create(
                FileResource.Open("Stitching.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.Use(next => context => Task.CompletedTask);
                });

            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("StitchingQuery.graphql"));

            FieldNode selection = query.Definitions
                .OfType<OperationDefinitionNode>().Single()
                .SelectionSet.Selections
                .OfType<FieldNode>().Single();

            // act
            var fieldDependencyResolver = new FieldDependencyResolver(schema);
            ISet<FieldDependency> dependencies = fieldDependencyResolver
                .GetFieldDependencies(
                    query,
                    selection,
                    schema.GetType<ObjectType>("Customer"));

            // assert
            dependencies.Snapshot();
        }
    }
}
