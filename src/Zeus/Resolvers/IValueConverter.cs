using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    internal interface IValueConverter
    {
        T Convert<T>(IValue value);
    }
}