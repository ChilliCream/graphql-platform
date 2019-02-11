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
                    schema_a,
                    "Schema_A"),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    schema_b,
                    "Schema_B"),
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
                    schema_a,
                    "Schema_A"),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    schema_b,
                    "Schema_B"),
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
                    schema_a,
                    "Schema_A"),
                new TypeInfo(
                    schema_b.Definitions.OfType<ITypeDefinitionNode>().First(),
                    schema_b,
                    "Schema_B"),
                new TypeInfo(
                    schema_c.Definitions.OfType<ITypeDefinitionNode>().First(),
                    schema_c,
                    "Schema_C"),
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
