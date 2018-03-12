using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    internal interface IValueConverter
    {
        object Convert(IValue value, Type desiredType);
    }
}