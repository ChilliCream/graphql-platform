using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace HotChocolate.Utilities
{
    public delegate object ChangeType(object value);
    public delegate TTo ChangeType<TFrom, TTo>(TFrom value);

    public interface ITypeConverterRegistry
    {
        void Register<TFrom, TTo>(ChangeType<TFrom, TTo> converter);

        void Register(Type from, Type to, ChangeType converter);
    }
}
