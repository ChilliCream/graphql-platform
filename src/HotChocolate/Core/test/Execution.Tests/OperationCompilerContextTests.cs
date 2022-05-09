using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution.OperationCompiler;

#nullable enable

public class OperationCompilerContextTests
{
    [Theory]
    [InlineData("foo", "foo", true)]
    [InlineData(null, null, true)]
    [InlineData("foo", null, false)]
    [InlineData(null, "foo", false)]
    [InlineData("foo", "bar", false)]
    public void SelectionPath_Equals(
        string? leftSegment,
        string? rightSegment,
        bool expected)
    {
        // arrange
        Processing.OperationCompiler.SelectionPath left = default;
        Processing.OperationCompiler.SelectionPath right = default;
        if (leftSegment is { })
        {
            left = left.Append(leftSegment);
        }

        if (rightSegment is { })
        {
            right = right.Append(rightSegment);
        }


        // act
        bool result = left.Equals(right);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("foo", "foo", true)]
    [InlineData(null, null, true)]
    [InlineData("foo", null, false)]
    [InlineData(null, "foo", false)]
    [InlineData("foo", "bar", false)]
    public void SelectionPath_GetHashCode(
        string? leftSegment,
        string? rightSegment,
        bool equal)
    {
        // arrange
        Processing.OperationCompiler.SelectionPath left = default;
        Processing.OperationCompiler.SelectionPath right = default;
        if (leftSegment is { })
        {
            left = left.Append(leftSegment);
        }

        if (rightSegment is { })
        {
            right = right.Append(rightSegment);
        }


        // act

        // assert
        if (equal)
        {
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
        else
        {
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }

    [Theory]
    [InlineData("path", true, "path", true)]
    [InlineData("path", false, "path", false)]
    [InlineData(null, true, "path", false)]
    [InlineData("path", true, null, false)]
    [InlineData(null, false, "path", false)]
    [InlineData("path", false, null, false)]
    public void SelectionReference_Equals(
        string? leftSegment,
        bool sameNodes,
        string? rightSegment,
        bool expected)
    {
        // arrange
        Processing.OperationCompiler.SelectionPath leftPath = default;
        Processing.OperationCompiler.SelectionPath rightPath = default;
        if (leftSegment is { })
        {
            leftPath = leftPath.Append(leftSegment);
        }

        if (rightSegment is { })
        {
            rightPath = rightPath.Append(rightSegment);
        }

        DirectiveNode[] directives = Array.Empty<DirectiveNode>();
        ArgumentNode[] args = Array.Empty<ArgumentNode>();
        FieldNode leftNode =
            new(null, new NameNode("foo"), null, null, directives, args, null);
        FieldNode rightNode = sameNodes
            ? leftNode
            : new(null, new NameNode("bar"), null, null, directives, args, null);

        Processing.OperationCompiler.SelectionReference left = new(leftPath, leftNode);
        Processing.OperationCompiler.SelectionReference right = new(rightPath, rightNode);

        // act
        bool result = left.Equals(right);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("path", true, "path", true)]
    [InlineData("path", false, "path", false)]
    [InlineData(null, true, "path", false)]
    [InlineData("path", true, null, false)]
    [InlineData(null, false, "path", false)]
    [InlineData("path", false, null, false)]
    public void SelectionReference_GetHashCode(
        string? leftSegment,
        bool sameNodes,
        string? rightSegment,
        bool equal)
    {
        // arrange
        Processing.OperationCompiler.SelectionPath leftPath = default;
        Processing.OperationCompiler.SelectionPath rightPath = default;
        if (leftSegment is { })
        {
            leftPath = leftPath.Append(leftSegment);
        }

        if (rightSegment is { })
        {
            rightPath = rightPath.Append(rightSegment);
        }

        DirectiveNode[] directives = Array.Empty<DirectiveNode>();
        ArgumentNode[] args = Array.Empty<ArgumentNode>();
        FieldNode leftNode =
            new(null, new NameNode("foo"), null, null, directives, args, null);
        FieldNode rightNode = sameNodes
            ? leftNode
            : new(null, new NameNode("bar"), null, null, directives, args, null);

        // act
        Processing.OperationCompiler.SelectionReference left = new(leftPath, leftNode);
        Processing.OperationCompiler.SelectionReference right = new(rightPath, rightNode);

        // assert
        if (equal)
        {
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
        else
        {
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }

    [Theory]
    [InlineData("path", true, "path", true)]
    [InlineData("path", false, "path", false)]
    [InlineData(null, true, "path", false)]
    [InlineData("path", true, null, false)]
    [InlineData(null, false, "path", false)]
    [InlineData("path", false, null, false)]
    public void SpreadReference_Equals(
        string? leftSegment,
        bool sameNodes,
        string? rightSegment,
        bool expected)
    {
        // arrange
        Processing.OperationCompiler.SelectionPath leftPath = default;
        Processing.OperationCompiler.SelectionPath rightPath = default;
        if (leftSegment is { })
        {
            leftPath = leftPath.Append(leftSegment);
        }

        if (rightSegment is { })
        {
            rightPath = rightPath.Append(rightSegment);
        }

        DirectiveNode[] directives = Array.Empty<DirectiveNode>();
        ArgumentNode[] args = Array.Empty<ArgumentNode>();
        FieldNode leftNode =
            new(null, new NameNode("foo"), null, null, directives, args, null);
        FieldNode rightNode = sameNodes
            ? leftNode
            : new(null, new NameNode("bar"), null, null, directives, args, null);

        Processing.OperationCompiler.SpreadReference left = new(leftPath, leftNode);
        Processing.OperationCompiler.SpreadReference right = new(rightPath, rightNode);

        // act
        bool result = left.Equals(right);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("path", true, "path", true)]
    [InlineData("path", false, "path", false)]
    [InlineData(null, true, "path", false)]
    [InlineData("path", true, null, false)]
    [InlineData(null, false, "path", false)]
    [InlineData("path", false, null, false)]
    public void SpreadReference_GetHashCode(
        string? leftSegment,
        bool sameNodes,
        string? rightSegment,
        bool equal)
    {
        // arrange
        Processing.OperationCompiler.SelectionPath leftPath = default;
        Processing.OperationCompiler.SelectionPath rightPath = default;
        if (leftSegment is { })
        {
            leftPath = leftPath.Append(leftSegment);
        }

        if (rightSegment is { })
        {
            rightPath = rightPath.Append(rightSegment);
        }

        DirectiveNode[] directives = Array.Empty<DirectiveNode>();
        ArgumentNode[] args = Array.Empty<ArgumentNode>();
        FieldNode leftNode =
            new(null, new NameNode("foo"), null, null, directives, args, null);
        FieldNode rightNode = sameNodes
            ? leftNode
            : new(null, new NameNode("bar"), null, null, directives, args, null);

        // act
        Processing.OperationCompiler.SpreadReference left = new(leftPath, leftNode);
        Processing.OperationCompiler.SpreadReference right = new(rightPath, rightNode);

        // assert
        if (equal)
        {
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
        else
        {
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }
}
