using System;
using System.Linq.Expressions;
using System.Text;

namespace HotChocolate.Client.Core.Utilities
{
    public class ExpressionTreeDebug : ExpressionVisitor
    {
        const int IndentSize = 2;
        StringBuilder builder = new StringBuilder();
        int indent;

        private ExpressionTreeDebug()
        {
        }

        public static string Debug(Expression e)
        {
            var i = new ExpressionTreeDebug();
            i.Visit(e);
            return i.builder.ToString();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            using (AppendAndIndent("[Binary]"))
            {
                using (AppendAndIndent("Left"))
                {
                    Visit(node.Left);
                }

                using (AppendAndIndent("Right"))
                {
                    Visit(node.Right);
                }
            }

            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            using (AppendAndIndent("[Block]"))
            {
                foreach (var e in node.Expressions)
                {
                    using (AppendAndIndent("Expression"))
                    {
                        Visit(e);
                    }
                }
            }

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Append($"[Constant] {node.Value}");
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            using (AppendAndIndent("[Lambda]"))
            using (AppendAndIndent("Body"))
            {
                Visit(node.Body);
            }

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            using (AppendAndIndent($"[Member] {node.Member.DeclaringType}.{node.Member.Name}"))
            {
                Visit(node.Expression);
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            using (AppendAndIndent($"[MethodCall] {node.Method.DeclaringType}.{node.Method.Name}"))
            {
                foreach (var a in node.Arguments)
                {
                    using (AppendAndIndent("Argument"))
                    {
                        Visit(a);
                    }
                }
            }

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            using (AppendAndIndent($"[New] {node.Constructor.DeclaringType}"))
            {
                foreach (var a in node.Arguments)
                {
                    using (AppendAndIndent("Argument"))
                    {
                        Visit(a);
                    }
                }
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            using (AppendAndIndent($"[Unary] {node.Method?.DeclaringType}.{node.Method?.Name}"))
            {
                Visit(node.Operand);
            }

            return node;
        }

        void Append(string s)
        {
            builder.Append(' ', indent);
            builder.AppendLine(s);
        }

        IDisposable AppendAndIndent(string s)
        {
            Append(s);
            indent += IndentSize;
            return Disposable.Create(() => indent -= IndentSize);
        }

        private class Disposable : IDisposable
        {
            Action a;

            public Disposable(Action a)
            {
                this.a = a;
            }

            public static IDisposable Create(Action a)
            {
                return new Disposable(a);
            }

            public void Dispose()
            {
                a();
            }
        }
    }
}
