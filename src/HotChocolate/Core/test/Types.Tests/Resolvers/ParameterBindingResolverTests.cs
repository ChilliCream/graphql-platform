using HotChocolate.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Resolvers;

public class ParameterBindingResolverTests
{
    [Fact]
    public void GetBindingInfo_DefaultCustomBuilder_ClassifiedAsCustom()
    {
        // arrange
        var resolver = CreateResolver(
            new CustomParameterExpressionBuilder<CustomState>(_ => new CustomState("hello")));
        var parameter = new ParameterDescriptor("state", typeof(CustomState), false, []);

        // act
        var (kind, _) = resolver.GetBindingInfo(parameter);

        // assert
        Assert.Equal(ArgumentKind.Custom, kind);
    }

    [Fact]
    public void GetBinding_DefaultCustomBuilder_ExecutesLambda()
    {
        // arrange
        var resolver = CreateResolver(
            new CustomParameterExpressionBuilder<CustomState>(_ => new CustomState("hello")));
        var parameter = new ParameterDescriptor("state", typeof(CustomState), false, []);

        // act
        var value = resolver.GetBinding(parameter).Execute<CustomState>(null!);

        // assert
        Assert.Equal("hello", value.Greetings);
    }

    [Fact]
    public void GetBindingInfo_CustomPredicateBuilder_MatchingType_Throws()
    {
        // arrange
        var resolver = CreateResolver(
            new CustomParameterExpressionBuilder<CustomState>(
                _ => new CustomState("hello"),
                canHandle: p => p.Name == "state"));
        var parameter = new ParameterDescriptor("state", typeof(CustomState), false, []);

        // act
        void Act() => resolver.GetBindingInfo(parameter);

        // assert
        Assert.Throws<SchemaException>(Act);
    }

    [Fact]
    public void GetBindingInfo_CustomPredicateBuilder_NonMatchingType_FallsBackToArgument()
    {
        // arrange
        // a custom-predicate builder for CustomState must not affect an unrelated string parameter
        var resolver = CreateResolver(
            new CustomParameterExpressionBuilder<CustomState>(
                _ => new CustomState("hello"),
                canHandle: p => p.Name == "state"));
        var parameter = new ParameterDescriptor("name", typeof(string), false, []);

        // act
        var (kind, _) = resolver.GetBindingInfo(parameter);

        // assert
        Assert.Equal(ArgumentKind.Argument, kind);
    }

    private static ParameterBindingResolver CreateResolver(
        params IParameterExpressionBuilder[] builders)
        => new(new ServiceCollection().BuildServiceProvider(), builders);

    public class CustomState(string greetings)
    {
        public string Greetings { get; } = greetings;
    }
}
