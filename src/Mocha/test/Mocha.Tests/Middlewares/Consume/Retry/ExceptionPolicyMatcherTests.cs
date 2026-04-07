namespace Mocha.Tests.Middlewares.Consume.Retry;

public sealed class ExceptionPolicyMatcherTests
{
    [Fact]
    public void Match_Should_ReturnNull_When_RulesListIsEmpty()
    {
        // arrange
        var rules = Array.Empty<ExceptionPolicyRule>();
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Match_Should_ReturnNull_When_NoRuleMatchesExceptionType()
    {
        // arrange
        var rules = new[]
        {
            CreateRule<ArgumentException>()
        };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Match_Should_ReturnRule_When_ExceptionTypeMatchesExactly()
    {
        // arrange
        var rule = CreateRule<InvalidOperationException>();
        var rules = new[] { rule };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Same(rule, result);
    }

    [Fact]
    public void Match_Should_ReturnRule_When_ExceptionIsDerivedType()
    {
        // arrange
        var rule = CreateRule<Exception>();
        var rules = new[] { rule };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Same(rule, result);
    }

    [Fact]
    public void Match_Should_ReturnMostSpecificRule_When_MultipleRulesMatchAtDifferentDepths()
    {
        // arrange
        var baseRule = CreateRule<Exception>();
        var specificRule = CreateRule<InvalidOperationException>();
        var rules = new[] { baseRule, specificRule };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Same(specificRule, result);
    }

    [Fact]
    public void Match_Should_ReturnNull_When_TypeMatchesButPredicateReturnsFalse()
    {
        // arrange
        var rule = CreateRule<InvalidOperationException>(predicate: _ => false);
        var rules = new[] { rule };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Match_Should_ReturnRule_When_TypeMatchesAndPredicateReturnsTrue()
    {
        // arrange
        var rule = CreateRule<InvalidOperationException>(predicate: _ => true);
        var rules = new[] { rule };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Same(rule, result);
    }

    [Fact]
    public void Match_Should_ReturnMoreSpecificRule_When_LessSpecificRuleHasPredicate()
    {
        // arrange
        var baseRuleWithPredicate = CreateRule<Exception>(predicate: _ => true);
        var specificRule = CreateRule<InvalidOperationException>();
        var rules = new[] { baseRuleWithPredicate, specificRule };
        var exception = new InvalidOperationException("test");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Same(specificRule, result);
    }

    [Fact]
    public void Match_Should_ReturnPassingRule_When_TwoRulesForSameTypeAndOnePredicateFails()
    {
        // arrange
        var failingRule = CreateRule<ArgumentNullException>(predicate: _ => false);
        var passingRule = CreateRule<ArgumentNullException>(predicate: _ => true);
        var rules = new[] { failingRule, passingRule };
        var exception = new ArgumentNullException("param");

        // act
        var result = ExceptionPolicyMatcher.Match(rules, exception);

        // assert
        Assert.Same(passingRule, result);
    }

    private static ExceptionPolicyRule CreateRule<TException>(
        Func<Exception, bool>? predicate = null)
        where TException : Exception
    {
        return new ExceptionPolicyRule
        {
            ExceptionType = typeof(TException),
            Predicate = predicate
        };
    }
}
