using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Utilities
{
    public interface IParameterHandler
    {
        bool CanHandle(ParameterInfo parameter);

        Expression CreateExpression(ParameterInfo parameter);
    }
}
