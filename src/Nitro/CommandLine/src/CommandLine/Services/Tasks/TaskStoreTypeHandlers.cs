using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Teaches Dapper how to read the <see cref="DateTimeOffset"/> columns that
/// the task store persists as text. Registered process-wide via a module
/// initializer.
/// </summary>
internal static class TaskStoreTypeHandlers
{
    [ModuleInitializer]
    internal static void Register()
        => SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
            => DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            => parameter.Value = value;
    }
}
