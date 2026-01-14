using System.Collections.Immutable;
using System.Text;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class SatisfiabilityError(
    string message,
    ImmutableArray<SatisfiabilityError>? errors = null)
{
    public string Message { get; } = message;

    public override string ToString()
    {
        return ToString(indentationLevel: 0);
    }

    private string ToString(int indentationLevel)
    {
        var indent = new string(' ', indentationLevel * 2);
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(indent + Message);

        if (errors is not null)
        {
            foreach (var error in errors)
            {
                stringBuilder.Append("\n" + error.ToString(indentationLevel + 1));
            }
        }

        return stringBuilder.ToString();
    }
}
