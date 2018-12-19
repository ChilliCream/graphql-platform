using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

namespace HotChocolate.Utilities
{
    public delegate bool ChangeTypeFactory(Type from, Type to,
        out ChangeType converter);
    public delegate object ChangeType(object source);
    public delegate TTo ChangeType<TFrom, TTo>(TFrom source);

    public interface ITypeConverterRegistry
    {
        void Register<TFrom, TTo>(ChangeType<TFrom, TTo> converter);

        void Register(Type from, Type to, ChangeType converter);

        void Register(ChangeTypeFactory converterFactory);
    }
}
