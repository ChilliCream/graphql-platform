using System;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Client.Core
{
    public interface IQueryableList
    {
        Expression Expression { get; }
    }

    public interface IQueryableList<out T> : IQueryableList
    {
    }
}
