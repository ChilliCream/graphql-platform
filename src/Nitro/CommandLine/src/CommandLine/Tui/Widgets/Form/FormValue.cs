namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// One value a form field can produce: a single string, or an ordered list of
/// strings. Closed so consumers pattern-match on the shape instead of binding
/// through reflection.
/// </summary>
internal abstract record FormValue
{
    private FormValue()
    {
    }

    /// <summary>
    /// A single string value, produced by text, text area, and select fields.
    /// </summary>
    public sealed record Text(string Value) : FormValue;

    /// <summary>
    /// An ordered list of string values, produced by the editable list field.
    /// </summary>
    public sealed record List(IReadOnlyList<string> Values) : FormValue
    {
        /// <summary>
        /// Two lists are equal when they contain the same values in the same order.
        /// </summary>
        public bool Equals(List? other) => other is not null && Values.SequenceEqual(other.Values);

        /// <inheritdoc cref="Equals(List)" />
        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var value in Values)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }
    }
}
