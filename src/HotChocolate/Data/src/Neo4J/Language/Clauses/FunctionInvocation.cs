using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/FunctionInvocation.html
    /// 
    /// </summary>
    public class FunctionInvocation<T, U> : Expression
    {

        private readonly string _funcName;
        private readonly static TypedSubtree<T, U> _arguments;


        public FunctionInvocationExpressionList(string funcName, Expression[] arguments)
        {
            _funcName = funcName;
            _arguments = new ExpressionList(arguments);
        }

        public string GetFuncName() => _funcName;

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _arguments.Visit(visitor);
            visitor.Leave(this);
        }
    }
}