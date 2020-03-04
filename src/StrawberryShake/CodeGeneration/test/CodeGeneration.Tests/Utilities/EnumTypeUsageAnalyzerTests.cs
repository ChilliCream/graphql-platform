using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers;
using Xunit;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class EnumTypeUsageAnalyzerTests
    {
        [Fact]
        public void Find_Enum_In_Nested_Input()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo(input: A): String
                    }

                    input A {
                        b: [B!]!
                    }

                    input B {
                        c: [C!]!
                    }

                    enum C {
                        A
                        B
                        C
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "query($a: A) { foo(input: $a) }");

            // act
            var analyzer = new EnumTypeUsageAnalyzer(schema);
            analyzer.Analyze(document);

            // assert
            Assert.Collection(analyzer.EnumTypes,
                t => Assert.Equal("C", t.Name));
        }

        [Fact]
        public void Find_Enum_As_Return_Type()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo(input: A): D
                    }

                    input A {
                        b: [B!]!
                    }

                    input B {
                        c: [C!]!
                    }

                    enum C {
                        A
                        B
                        C
                    }

                    enum D {
                        A
                        B
                        C
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "query { foo(input: { }) }");

            // act
            var analyzer = new EnumTypeUsageAnalyzer(schema);
            analyzer.Analyze(document);

            // assert
            Assert.Collection(analyzer.EnumTypes,
                t => Assert.Equal("D", t.Name));
        }

        [Fact]
        public void Find_Enum_As_Variable_Type()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo(input: [C!]!): String
                    }

                    enum C {
                        A
                        B
                        C
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "query($a: [C!]!) { foo(input: $a) }");

            // act
            var analyzer = new EnumTypeUsageAnalyzer(schema);
            analyzer.Analyze(document);

            // assert
            Assert.Collection(analyzer.EnumTypes,
                t => Assert.Equal("C", t.Name));
        }

        [Fact]
        public void Find_Only_Selected_Return_Types()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        a: A
                        c: C
                    }

                    enum A {
                        A
                        B
                        C
                    }

                    enum B {
                        A
                        B
                        C
                    }

                    enum C {
                        A
                        B
                        C
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                "query($a: [C!]!) { a c }");

            // act
            var analyzer = new EnumTypeUsageAnalyzer(schema);
            analyzer.Analyze(document);

            // assert
            Assert.Collection(analyzer.EnumTypes.OrderBy(t => t.Name),
                t => Assert.Equal("A", t.Name),
                t => Assert.Equal("C", t.Name));
        }
    }
}
