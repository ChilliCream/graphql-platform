using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class EnumTypeMergeHandlerTests
    {
        [Fact]
        public void MergeIdenticalEnums()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("enum Foo { BAR BAZ }");
            DocumentNode schema_b =
                Parser.Default.Parse("enum Foo { BAR BAZ }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new EnumTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }

        [Fact]
        public void MergeIdenticalEnumsTakeDescriptionFromSecondType()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("enum Foo { BAR BAZ }");
            DocumentNode schema_b =
                Parser.Default.Parse(@"""Foo Bar"" enum Foo { BAR BAZ }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new EnumTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }

        [Fact]
        public void MergeNonIdenticalEnums()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("enum Foo { BAR BAZ }");
            DocumentNode schema_b =
                Parser.Default.Parse("enum Foo { BAR BAZ }");
            DocumentNode schema_c =
                Parser.Default.Parse("enum Foo { BAR BAZ QUX }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                TypeInfo.Create(
                    schema_c.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_C", schema_c))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new EnumTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }

        [Fact]
        public void MergeNonIdenticalEnums2()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("enum Foo { BAR BAZ }");
            DocumentNode schema_b =
                Parser.Default.Parse("enum Foo { BAR BAZ QUX }");
            DocumentNode schema_c =
                Parser.Default.Parse("enum Foo { BAR BAZ QUX }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                TypeInfo.Create(
                    schema_c.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_C", schema_c))
            };

            var context = new SchemaMergeContext();

            // act
            var typeMerger = new EnumTypeMergeHandler((c, t) => { });
            typeMerger.Merge(context, types);

            // assert
            SchemaSyntaxSerializer.Serialize(context.CreateSchema()).Snapshot();
        }

        [Fact]
        public void Merge_DifferentTypes_InputMergesLeftoversArePassed()
        {
            // arrange
            DocumentNode schema_a =
                Parser.Default.Parse("input A { b: String }");
            DocumentNode schema_b =
                Parser.Default.Parse("enum A { B C }");

            var types = new List<ITypeInfo>
            {
                TypeInfo.Create(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                TypeInfo.Create(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
            };

            var context = new SchemaMergeContext();

            var leftovers = new List<ITypeInfo>();

            // act
            var typeMerger = new EnumTypeMergeHandler(
                (c, t) => leftovers.AddRange(t));
            typeMerger.Merge(context, types);

            // assert
            Assert.Collection(leftovers,
                t => Assert.IsType<InputObjectTypeInfo>(t));

            Snapshooter.Xunit.Snapshot.Match(new List<object>
            {
                SchemaSyntaxSerializer.Serialize(context.CreateSchema()),
                leftovers
            });
        }
    }
}
