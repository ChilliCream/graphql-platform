using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities;

internal sealed class EnumTypeConverter : IChangeTypeProvider
{
    public bool TryCreateConverter(
        Type source,
        Type target,
        ChangeTypeProvider root,
        [NotNullWhen(true)] out ChangeType converter)
    {
        if (source == typeof(string) && target.IsEnum)
        {
            converter = input => Enum.Parse(target, (string)input, true);
            return true;
        }

        if (source.IsEnum && target == typeof(string))
        {
            converter = input => input?.ToString();
            return true;
        }

        converter = null;
        return false;
    }
}
