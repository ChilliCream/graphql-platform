using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests
{
    public class SubscriptionServiceGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly OperationServiceGenerator _generator;

        public SubscriptionServiceGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new OperationServiceGenerator();
        }

        [Fact]
        public async Task GenerateQueryServiceWithoutArguments()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new SubscriptionOperationDescriptor(
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
        public async Task GenerateQueryServiceWithValueArgument()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new SubscriptionOperationDescriptor(
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
                        {"name", TestHelper.GetNonNullStringTypeReference()}
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateQueryServiceWithReferenceArgument()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new SubscriptionOperationDescriptor(
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
        public async Task GenerateQueryServiceWithArguments()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new SubscriptionOperationDescriptor(
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
                        {"name", TestHelper.GetNonNullStringTypeReference()},
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
