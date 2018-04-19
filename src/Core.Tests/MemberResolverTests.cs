using System;
using System.Linq.Expressions;
using Xunit;

namespace HotChocolate
{
    public class MemberResolverTests
    {
        [Fact]
        public void ExpressionTest()
        {
            var dateStr = Expression.Parameter(typeof(string));
            var asDateTime = Expression.Call(typeof(DateTime), "Parse", null, dateStr); // calls static method "DateTime.Parse"
            var fmtExpr = Expression.Constant("MM/dd/yyyy");
            var body = Expression.Call(asDateTime, "ToString", null, fmtExpr); // calls instance method "DateTime.ToString(string)"
            var lambdaExpr = Expression.Lambda<Func<string, string>>(body, dateStr);
            var f = lambdaExpr.Compile();
            string s = f.Invoke("2018-01-01");
            Console.WriteLine(s);

            /* 
            const string exp = @"Hello(a, b)";
            var a  = Expression.Parameter(typeof(string), "a");
            var b  = Expression.Parameter(typeof(string), "b");
            
            Expression.Dynamic(Expression.Call())
            System.Linq.Expressions.DynamicExpression.Dynamic()
            var e = System.Linq.Dynamic.DynamicExpression.ParseLambda(new[] { p }, null, exp);
            var bob = new Person
            {
                Name = "Bob",
                Age = 30,
                Weight = 213,
                FavouriteDay = new DateTime(2000, 1, 1)
            };

            var result = e.Compile().DynamicInvoke(bob);
            Console.WriteLine(result);
            Console.ReadKey();
            */
        }

        public string Hello(string a, string b)
        {
            return "World";
        }
    }
}