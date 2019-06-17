using System;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Client.Core
{
    public interface IQueryableValue
    {
        Expression Expression { get; }
    }

    public interface IQueryableValue<out T> : IQueryableValue
    {
    }
}
