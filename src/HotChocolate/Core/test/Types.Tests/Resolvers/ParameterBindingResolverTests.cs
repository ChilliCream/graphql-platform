using HotChocolate.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Resolvers;

public class ParameterBindingResolverTests
{
    [Fact]
    public void GetBinding_Should_PreservePurityAndExecuteExpression_When_BuilderMatchesType()
    {
        // arrange
        var builder = new CustomParameterExpressionBuilder<string>(
            static _ => "custom",
            isPure: false);
        var resolver = new ParameterBindingResolver(
            new ServiceCollection().BuildServiceProvider(),
            [builder]);
        var parameter = new ParameterDescriptor(
            "value",
            typeof(string),
            isNullable: false,
            []);

        // act
        var bindingInfo = resolver.GetBindingInfo(parameter);
        var binding = resolver.GetBinding(parameter, out var bindingKind);

        // assert
        Assert.Equal((ArgumentKind.Custom, false), bindingInfo);
        Assert.Equal(ArgumentKind.Custom, bindingKind);
        Assert.False(binding.IsPure);
        Assert.Equal("custom", binding.Execute<string>(null!));
    }

    [Fact]
    public void GetBindingInfo_Should_RejectParameterInfoPredicate_When_ResultTypeIsAssignableToParameter()
    {
        // arrange
        var builder = new CustomParameterExpressionBuilder<TestUser>(
            static _ => new TestUser(),
            static _ => true);
        var resolver = new ParameterBindingResolver(
            new ServiceCollection().BuildServiceProvider(),
            [builder]);
        var parameter = new ParameterDescriptor(
            "user",
            typeof(ITestUser),
            isNullable: false,
            []);

        // act
        var exception = Assert.Throws<SchemaException>(() => resolver.GetBindingInfo(parameter));

        // assert
        Assert.Equal(
            "Custom parameter expression builders that use a ParameterInfo predicate cannot be "
            + "used with source-generated resolvers. Omit the canHandle predicate to match "
            + "parameters by type.",
            exception.Errors.Single().Message);
    }

    [Fact]
    public void GetBindingInfo_Should_UseArgumentBinding_When_PredicateResultTypeIsUnrelated()
    {
        // arrange
        var builder = new CustomParameterExpressionBuilder<TestUser>(
            static _ => new TestUser(),
            static _ => true);
        var resolver = new ParameterBindingResolver(
            new ServiceCollection().BuildServiceProvider(),
            [builder]);
        var parameter = new ParameterDescriptor(
            "value",
            typeof(int),
            isNullable: false,
            []);

        // act
        var bindingInfo = resolver.GetBindingInfo(parameter);

        // assert
        Assert.Equal((ArgumentKind.Argument, true), bindingInfo);
    }

    private interface ITestUser;

    private sealed class TestUser : ITestUser;
}
