using System;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultParserGeneratorTests
    {
        [Fact]
        public async Task Generate_Nullable_List_Nullable_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[]
                {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        new ResultTypeDescriptor(
                            "IBar",
                            "Bar",
                            new []
                            {
                                new ResultTypeComponentDescriptor("IAbc", true, true, true),
                                new ResultTypeComponentDescriptor("IDef", true, false, true)
                            },
                            Array.Empty<ResultFieldDescriptor>()),
                        new[]
                        {
                            new ResultTypeDescriptor(
                                "Foo",
                                "Foo",
                                new []
                                {
                                    new ResultTypeComponentDescriptor("Abc", true, true, true),
                                    new ResultTypeComponentDescriptor("Def", true, false, true)
                                },
                                Array.Empty<ResultFieldDescriptor>())
                        },
                        false)
                },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_NonNull_List_Nullable_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[]
                {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        new ResultTypeDescriptor(
                            "IBar",
                            "Bar",
                            new []
                            {
                                new ResultTypeComponentDescriptor("IAbc", false, true, true),
                                new ResultTypeComponentDescriptor("IDef", true, false, true)
                            },
                            Array.Empty<ResultFieldDescriptor>()),
                        new[]
                        {
                            new ResultTypeDescriptor(
                                "Foo",
                                "Foo",
                                new []
                                {
                                    new ResultTypeComponentDescriptor("Abc", false, true, true),
                                    new ResultTypeComponentDescriptor("Def", true, false, true)
                                },
                                Array.Empty<ResultFieldDescriptor>())
                        },
                        false)
                },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_NonNull_List_NonNull_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Abc", false, true, true),
                            new ResultTypeComponentDescriptor("Def", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_Nullable_List_Nullable_NestedList_Nullable_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Abc", true, true, true),
                            new ResultTypeComponentDescriptor("Def", true, true, true),
                            new ResultTypeComponentDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_NonNull_List_Nullable_NestedList_Nullable_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Abc", false, true, true),
                            new ResultTypeComponentDescriptor("Def", true, true, true),
                            new ResultTypeComponentDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_NonNull_List_NonNull_NestedList_Nullable_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Abc", false, true, true),
                            new ResultTypeComponentDescriptor("Def", false, true, true),
                            new ResultTypeComponentDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_NonNull_List_NonNull_NestedList_NonNull_Element()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Abc", false, true, true),
                            new ResultTypeComponentDescriptor("Def", false, true, true),
                            new ResultTypeComponentDescriptor("Ghi", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_Nullable_Object()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_NonNull_Object()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Ghi", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_Deserializer_Nullable_String()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                Array.Empty<ResultParserMethodDescriptor>(),
                new[] {
                    new ResultParserDeserializerMethodDescriptor(
                        "DeserializeNullableString",
                        "string",
                        "string",
                        new [] {
                            new ResultTypeComponentDescriptor("string", true, false, true)
                        },
                        new ValueSerializerDescriptor("String", "_stringSerializer"))
                },
                new[] {
                    new ValueSerializerDescriptor("String", "_stringSerializer")
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_Deserializer_NonNull_String()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                Array.Empty<ResultParserMethodDescriptor>(),
                new[] {
                    new ResultParserDeserializerMethodDescriptor(
                        "DeserializeNullableString",
                        "string",
                        "string",
                        new [] {
                            new ResultTypeComponentDescriptor("string", false, false, true)
                        },
                        new ValueSerializerDescriptor("String", "_stringSerializer"))
                },
                new[] {
                    new ValueSerializerDescriptor("String", "_stringSerializer")
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_Deserializer_Nullable_List_Nullable_String()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                Array.Empty<ResultParserMethodDescriptor>(),
                new[] {
                    new ResultParserDeserializerMethodDescriptor(
                        "DeserializeNullableString",
                        "string",
                        "string",
                        new [] {
                            new ResultTypeComponentDescriptor("IReadOnlyList", true, true, true),
                            new ResultTypeComponentDescriptor("string", true, false, true)
                        },
                        new ValueSerializerDescriptor("String", "_stringSerializer"))
                },
                new[] {
                    new ValueSerializerDescriptor("String", "_stringSerializer")
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new ResultParserGenerator();

            var descriptor = new ResultParserDescriptor(
                "GetHumanResultParser",
                "Demo",
                "IFoo",
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeComponentDescriptor("Ghi", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethodDescriptor>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
