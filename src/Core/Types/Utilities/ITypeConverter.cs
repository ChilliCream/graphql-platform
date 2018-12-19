using System;

namespace HotChocolate.Utilities
{
    public interface ITypeConverter
    {
        Type From { get; }

        Type To { get; }

        object Convert(object source);
    }
}
