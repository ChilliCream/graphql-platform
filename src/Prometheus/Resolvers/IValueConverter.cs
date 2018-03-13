using System;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    internal interface IValueConverter
    {
        object Convert(IValue value, Type desiredType);

        IValue Convert(object value, ISchemaDocument schema, IType desiredType);
    }
}