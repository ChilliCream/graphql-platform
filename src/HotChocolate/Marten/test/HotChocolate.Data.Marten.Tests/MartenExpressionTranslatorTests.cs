using System.Linq.Expressions;

namespace HotChocolate.Data.Marten.Tests;

public class MartenExpressionTranslatorTests
{
    private class ReferenceType
    {
        public int Count { get; set; }
    }

    [Fact]
    public void Filter_Expression_Is_Translated()
    {
        var leftOperand = Expression.NotEqual(
            Expression.Parameter(typeof(ReferenceType), "x"),
            Expression.Constant(null, typeof(ReferenceType)));
        var rightOperand = Expression.GreaterThan(
            Expression.MakeMemberAccess(
                Expression.Parameter(typeof(ReferenceType), "x"),
                typeof(ReferenceType).GetMember(nameof(ReferenceType.Count)).First()),
            Expression.Constant(10)
        );
        var expression = Expression.AndAlso(leftOperand, rightOperand);
        var result = MartenExpressionTranslator.TranslateFilterExpression(expression);
        Assert.Equal(rightOperand, result);
    }

    [Fact]
    public void Filter_Expression_Is_Not_Translated()
    {
        var expression = Expression.GreaterThan(
            Expression.MakeMemberAccess(
                Expression.Parameter(typeof(ReferenceType), "x"),
                typeof(ReferenceType).GetMember(nameof(ReferenceType.Count)).First()),
            Expression.Constant(10)
        );
        var result = MartenExpressionTranslator.TranslateFilterExpression(expression);
        Assert.Equal(expression, result);
    }

    [Fact]
    public void Sorting_Expression_Is_Translated()
    {
        var ifTrue = Expression.Default(typeof(int));
        var ifFalse = Expression.Property(
            Expression.Parameter(typeof(ReferenceType), "x"),
            typeof(ReferenceType).GetProperty(nameof(ReferenceType.Count))!);
        var test = Expression.Equal(
            Expression.Parameter(typeof(ReferenceType), "x"),
            Expression.Constant(null, typeof(ReferenceType)));
        var expression = Expression.Condition(test, ifTrue, ifFalse);
        var result = MartenExpressionTranslator.TranslateSortExpression(expression);
        Assert.Equal(ifFalse, result);
    }

    [Fact]
    public void Sorting_Expression_Is_Not_Translated()
    {
        var expression = Expression.Property(
            Expression.Parameter(typeof(ReferenceType), "x"),
            typeof(ReferenceType).GetProperty(nameof(ReferenceType.Count))!);
        var result = MartenExpressionTranslator.TranslateFilterExpression(expression);
        Assert.Equal(expression, result);
    }
}
