namespace StrawberryShake.Serialization
{
    /// <summary>
    /// The input value formatter serializes input values so that they can be send to the server.
    /// </summary>
    public interface IInputValueFormatter : ISerializer
    {
        /// <summary>
        /// Formats an input value for transport.
        /// </summary>
        /// <param name="runtimeValue">
        /// The runtime representation of an input value.
        /// </param>
        /// <returns>
        /// Return a serialized/formatted version of the input value.
        /// </returns>
        object? Format(object? runtimeValue);
    }
}
