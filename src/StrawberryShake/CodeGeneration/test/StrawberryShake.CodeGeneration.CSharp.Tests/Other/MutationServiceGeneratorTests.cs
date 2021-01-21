using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Other
{
    public class MutationServiceGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly OperationServiceGenerator _generator;

        public MutationServiceGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new OperationServiceGenerator();
        }

        [Fact]
        public async Task GenerateMutationServiceWithoutArguments()
        {
            await _generator.Write(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeReferenceDescriptor(
                        new TypeDescriptor(
                            "Foo",
                            "FooNamespace"
                        ),
                        false,
                        ListType.NoList
                    ),
                    new Dictionary<string, TypeReferenceDescriptor>()
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMutationServiceWithValueArgument()
        {
            await _generator.Write(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeReferenceDescriptor(
                        new TypeDescriptor(
                            "Foo",
                            "FooNamespace"
                        ),
                        false,
                        ListType.NoList
                    ),
                    new Dictionary<string, TypeReferenceDescriptor>()
                    {
                        {
                            "name", new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    "System"
                                ),
                                false,
                                ListType.NoList
                            )
                        }
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMutationServiceWithReferenceArgument()
        {
            await _generator.Write(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeReferenceDescriptor(
                        new TypeDescriptor(
                            "Foo",
                            "FooNamespace"
                        ),
                        false,
                        ListType.NoList
                    ),
                    new Dictionary<string, TypeReferenceDescriptor>()
                    {
                        {
                            "bar", new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "BarInput",
                                    "BarInputNamespace"
                                ),
                                true,
                                ListType.NoList
                            )
                        }
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMutationServiceWithArguments()
        {
            await _generator.Write(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeReferenceDescriptor(
                        new TypeDescriptor(
                            "Foo",
                            "FooNamespace"
                        ),
                        false,
                        ListType.NoList
                    ),
                    new Dictionary<string, TypeReferenceDescriptor>()
                    {
                        {
                            "name", new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    "System"
                                ),
                                false,
                                ListType.NoList
                            )
                        },
                        {
                            "a", new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "BarInput",
                                    "BarInputNamespace"
                                ),
                                true,
                                ListType.NoList
                            )
                        }
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
