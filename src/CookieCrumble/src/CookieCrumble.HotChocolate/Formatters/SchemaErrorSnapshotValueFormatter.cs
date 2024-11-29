using System.Buffers;
using CookieCrumble.Formatters;
using HotChocolate;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class SchemaErrorSnapshotValueFormatter
    : SnapshotValueFormatter<ISchemaError[]>
{
    protected override void Format(IBufferWriter<byte> snapshot, ISchemaError[] value)
    {
        foreach (var error in value)
        {
            snapshot.Append(error.Message);

            if (error.Code is not null)
            {
                snapshot.AppendLine();
                snapshot.Append(error.Code);
            }

            if (error.Exception is not null)
            {
                var exception = error.Exception;

                snapshot.AppendLine();
                snapshot.Append(exception.Message);
                snapshot.AppendLine();
                snapshot.Append(exception.GetType().FullName ?? exception.GetType().Name);
            }
        }
    }
}
