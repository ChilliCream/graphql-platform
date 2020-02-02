using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class FieldResolverTests
    {
        [Fact]
        public void Fields()
        {
            // arrange
            var errorRaised = false;
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    x:c
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(
                    schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet,
                    null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.ResponseName);
                    Assert.Equal("c", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
            Assert.False(errorRaised);
        }

        [Fact]
        public void InvalidFieldError()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    x:c
                    xyz
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            Action action = () => fieldResolver
               .CollectFields(schema.GetType<ObjectType>("Foo"),
                   operation.SelectionSet, null);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void SkipFields()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    x:c @skip(if:true)
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void IncludeFields()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a @include(if:true)
                    x:c @include(if:false)
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void SkipOverIncludeFields()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a @include(if:true)
                    x:c @include(if:true) @skip(if:true)
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndInlineFragments()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    ... on Foo {
                        z:a
                    }
                    ... on Fa {
                        x:a
                    }
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("z", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndFragmentDefinitions()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    ... Test
                }

                fragment Test on Foo {
                    x: a
                }

                fragment Test on Fa {
                    z: a
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndFragmentDefinitionsUnionType()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    ... Test
                }

                fragment Test on FooUnion {
                    x: a
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields_a = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);
            IReadOnlyCollection<FieldSelection> fields_b = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Fum"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields_a,
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });

            Assert.Collection(fields_b,
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndFragmentDefinitionsInterfaceType()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    a
                    ... Test
                }

                fragment Test on IFoo {
                    x: a
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields_a = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);
            IReadOnlyCollection<FieldSelection> fields_b = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Fum"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields_a,
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });

            Assert.Collection(fields_b,
                f =>
                {
                    Assert.Equal("a", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void InlineFragments()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    ... Test
                }

                fragment Test on Foo {
                    x: a
                }

                fragment Test on Fa {
                    z: a
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("x", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FragmentDefinitions()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    ... Test
                }

                fragment Test on Foo {
                    x: a
                }

                fragment Test on Fa {
                    z: a
                }
            ");

            var variables = new VariableValueCollection(
                TypeConversion.Default,
                new Dictionary<string, VariableValue>());
            var fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            var fieldResolver = new FieldCollector(
                fragments,
                (f, s) => null,
                TypeConversion.Default,
                Array.Empty<IArgumentCoercionHandler>());

            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, null);

            // assert
            Assert.Collection(fields.Where(t => t.IsVisible(variables)),
                f =>
                {
                    Assert.Equal("x", f.ResponseName);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Foo implements IFoo {
                    a: String
                    b: String
                    c: String
                }

                type Fa {
                    a: String
                }

                type Fum {
                    a: String
                }

                interface IFoo {
                    a: String
                }

                union FooUnion = Foo | Fa

                type Query { a: String }
                ",
                c => c.Use(next => context => Task.CompletedTask));
        }
    }
}
