using System.Threading.Tasks;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryDirectiveTests
    {
        [Fact]
        public void SingleDelegateMiddleware()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.MakeExecutable().Execute(
                "{ sayHello @resolve @appendString(s: \"abc\") }");

            // assert
            result.MatchSnapshot();
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<ResolveDirective>();
                c.RegisterDirective<AppendStringDirectiveType>();
                c.RegisterQueryType<Query>();
            });
        }

        public class Query
        {
            public string SayHello()
            {
                return "Hello";
            }
        }

        public class AppendStringDirectiveType
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("appendString");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Argument("s").Type<NonNullType<StringType>>();
                descriptor.Use(next => context =>
                {
                    context.Result = context.Result +
                        context.Directive.GetArgument<string>("s");
                    return next.Invoke(context);
                });
            }
        }

        public class AppendDirectiveMiddleware
        {
            public string AppendString(
                [Result]string result,
                [DirectiveArgument]string s)
            {
                return result + s;
            }

            public Task<string> AppendStringAsync(
               [Result]string result,
               [DirectiveArgument]string s)
            {
                return Task.FromResult<string>(result + s);
            }
        }

    }
}
