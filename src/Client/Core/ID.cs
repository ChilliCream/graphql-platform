using System;
using Newtonsoft.Json;
using HotChocolate.Client.Internal;

namespace HotChocolate.Client
{
    /// <summary>
    /// Represents a unique identifier.
    /// </summary>
    [JsonConverter(typeof(IDConverter))]
    public readonly struct ID
    {
        /// <summary>
        /// Generates a new instance of the <see cref="ID"/> struct.
        /// </summary>
        /// <param name="value">The ID string.</param>
        public ID(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the ID as a string value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Converts the ID to a string.
        /// </summary>
        /// <returns>The ID as a string.</returns>
        public override string ToString() => Value;
    }
}
