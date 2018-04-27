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
            ISchema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    x:c
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { errorRaised = true; });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.Name);
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
            ISchema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    x:c
                    xyz
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { errorRaised = true; });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.Name);
                    Assert.Equal("c", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
            Assert.True(errorRaised);
        }

        [Fact]
        public void SkipFields()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                    x:c @skip(if:true)
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void IncludeFields()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a @include(if:true)
                    x:c @include(if:false)
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void SkipOverIncludeFields()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    a @include(if:true)
                    x:c @include(if:true) @skip(if:true)
                }
            ");

            VariableCollection variables = new VariableCollection(
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndInlineFragments()
        {
            // arrange
            ISchema schema = CreateSchema();
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
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("z", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndFragmentDefinitions()
        {
            // arrange
            ISchema schema = CreateSchema();
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
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        // TODO : enable when schema supports unions
        [Fact(Skip = "Schema does not support unions yet.")]
        public void FieldsAndFragmentDefinitionsUnionType()
        {
            // arrange
            ISchema schema = CreateSchema();
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
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
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
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });

            Assert.Collection(fields_b,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void FieldsAndFragmentDefinitionsInterfaceType()
        {
            // arrange
            ISchema schema = CreateSchema();
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
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
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
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                },
                f =>
                {
                    Assert.Equal("x", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });

            Assert.Collection(fields_b,
                f =>
                {
                    Assert.Equal("a", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        [Fact]
        public void InlineFragments()
        {
            // arrange
            ISchema schema = CreateSchema();
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
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("x", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        public void FragmentDefinitions()
        {
            // arrange
            ISchema schema = CreateSchema();
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
                new Dictionary<string, CoercedVariableValue>());
            FragmentCollection fragments = new FragmentCollection(schema, query);

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().First();

            // act
            FieldResolver fieldResolver = new FieldResolver(variables, fragments);
            IReadOnlyCollection<FieldSelection> fields = fieldResolver
                .CollectFields(schema.GetType<ObjectType>("Foo"),
                    operation.SelectionSet, e => { });

            // assert
            Assert.Collection(fields,
                f =>
                {
                    Assert.Equal("x", f.Name);
                    Assert.Equal("a", f.Field.Name);
                    Assert.Equal("String", f.Field.Type.TypeName());
                });
        }

        private ISchema CreateSchema()
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

                ", c => { });
        }
    }
}
