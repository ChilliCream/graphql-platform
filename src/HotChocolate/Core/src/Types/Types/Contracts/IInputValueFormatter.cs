#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// An <see cref="IInputValueFormatter"/> can intercept deserialized runtime values and
    /// reformat them into another value. This interface is useful to for instance apply
    /// logic like the relay IDs.
    /// </summary>
    public interface IInputValueFormatter
    {
        /// <summary>
        /// Is called after the field has deserialized its value.
        /// If you do not want to handle a value just return the incoming <paramref name="runtimeValue"/>;
        /// otherwise, return the formatted value.
        /// </summary>
        /// <param name="runtimeValue">
        /// The originally deserialized runtime value.
        /// </param>
        /// <returns>
        /// Returns either the <paramref name="runtimeValue"/> or another value
        /// that represents a formatted version or it.
        /// </returns>
        object? OnAfterDeserialize(object? runtimeValue);
    }
}
