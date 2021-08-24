namespace StrawberryShake
{
    /// <summary>
    /// Represents the document hash.
    /// </summary>
    public readonly struct DocumentHash
    {
        /// <summary>
        /// Creates a new instance of <see cref="DocumentHash"/>.
        /// </summary>
        /// <param name="algorithm">
        /// The name of the hash algorithm.
        /// </param>
        /// <param name="value">
        /// The document hash value.
        /// </param>
        public DocumentHash(string algorithm, string value)
        {
            Algorithm = algorithm;
            Value = value;
        }

        /// <summary>
        /// Gets the name of the hash algorithm.
        /// </summary>
        public string Algorithm { get; }

        /// <summary>
        /// Gets the document hash value.
        /// </summary>
        public string Value { get; }
    }
}
