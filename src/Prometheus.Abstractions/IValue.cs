using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public interface IValue
    {
        /// <summary>
        /// Gets the raw inner value.
        /// </summary>
        object Value { get; }
    }
}