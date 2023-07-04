using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public interface IExpressionCompilationContext
{
    Identifier NodeId { get; }
    Type ExpectedExpressionType { get; }
    ICompiledExpressions Expressions { get; }
    IVariableContext Variables { get; }
}

public struct CachedExpressionNode
{
    public Expression Expression { get; set; }
    public SealedExpressionNode Node { get; set; }
}

public interface ICompiledExpressions
{
    Expression Instance { get; }
    ParameterExpression InstanceRoot { get; }
    IReadOnlyList<Expression> Children { get; }
}
