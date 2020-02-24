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
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeDescriptor("Abc", true, true, true),
                            new ResultTypeDescriptor("Def", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                new[] {
                    new ResultParserMethodDescriptor(
                        "ParseFooBar",
                        "IBar",
                        new [] {
                            new ResultTypeDescriptor("Abc", false, true, true),
                            new ResultTypeDescriptor("Def", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Abc", false, true, true),
                            new ResultTypeDescriptor("Def", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Abc", true, true, true),
                            new ResultTypeDescriptor("Def", true, true, true),
                            new ResultTypeDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Abc", false, true, true),
                            new ResultTypeDescriptor("Def", true, true, true),
                            new ResultTypeDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Abc", false, true, true),
                            new ResultTypeDescriptor("Def", false, true, true),
                            new ResultTypeDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Abc", false, true, true),
                            new ResultTypeDescriptor("Def", false, true, true),
                            new ResultTypeDescriptor("Ghi", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Ghi", true, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
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
                            new ResultTypeDescriptor("Ghi", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Generate_Deserialize_Nullable_String_Deserializer()
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
                    new ResultParserDeserializerMethod(
                        "DeserializeNullableString",
                        "string",
                        "string",
                        new [] {
                            new ResultTypeDescriptor("string", true, false, true)
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
                            new ResultTypeDescriptor("Ghi", false, false, true)
                        },
                        false,
                        new [] {
                            new ResultFieldDescriptor("FieldA", "ParseThisAndThat")
                        }
                    ) },
                Array.Empty<ResultParserDeserializerMethod>(),
                Array.Empty<ValueSerializerDescriptor>());

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
