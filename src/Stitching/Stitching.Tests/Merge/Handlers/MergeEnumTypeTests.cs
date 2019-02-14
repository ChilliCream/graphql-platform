using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Stitching
{
    public class MergeEnumTypeTests
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
                new TypeInfo(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new TypeInfo(
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
                new TypeInfo(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new TypeInfo(
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
                new TypeInfo(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                new TypeInfo(
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
                new TypeInfo(
                    schema_a.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_A", schema_a)),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    new SchemaInfo("Schema_B", schema_b)),
                new TypeInfo(
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
    }
}
