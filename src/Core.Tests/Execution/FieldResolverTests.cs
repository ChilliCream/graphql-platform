using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class FieldResolverTests
    {
        [Fact]
        public void Fields()
        {
            // arrange
            bool errorRaised = false;
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    x:c
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { errorRaised = true; });

            // assert
            Assert.Collection(fields,
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
            bool errorRaised = false;
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    x:c
                    xyz
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { errorRaised = true; });

            // assert
            Assert.Collection(fields,
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
            Assert.True(errorRaised);
        }

        [Fact]
        public void SkipFields()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    x:c @skip(if:true)
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a @include(if:true)
                    x:c @include(if:false)
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a @include(if:true)
                    x:c @include(if:true) @skip(if:true)
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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
            DocumentNode query = Parser.Default.Parse(@"
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

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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
            DocumentNode query = Parser.Default.Parse(@"
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

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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

        // TODO : enable when schema supports unions
        [Fact]
        public void FieldsAndFragmentDefinitionsUnionType()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    ... Test
                }

                fragment Test on FooUnion {
                    x: a
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields_a = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });
            IReadOnlyCollection<FieldSelection> fields_b = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Fum"),
                    operation.SelectionSet, e => { });

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
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    ... Test
                }

                fragment Test on IFoo {
                    x: a
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields_a = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });
            IReadOnlyCollection<FieldSelection> fields_b = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Fum"),
                    operation.SelectionSet, e => { });

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
            DocumentNode query = Parser.Default.Parse(@"
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

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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
            DocumentNode query = Parser.Default.Parse(@"
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

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, object>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(schema, variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
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
                ", c => { });
        }
    }
}
