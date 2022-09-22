using System.Buffers;

namespace CookieCrumble.Formatters;

public class HttpResponseSnapshotValueFormatter : SnapshotValueFormatter<HttpResponseMessage>
{
    protected override void Format(IBufferWriter<byte> snapshot, HttpResponseMessage value)
    {
        var first = true;

        foreach (var header in value.Headers.Concat(value.Content.Headers))
        {
            if (first)
            {
                snapshot.Append("Headers:");
                snapshot.AppendLine();
                first = false;
            }

            snapshot.Append($"{header.Key}: {string.Join(" ", header.Value)}");
            snapshot.AppendLine();
        }

        if (!first)
        {
            snapshot.Append("-------------------------->");
            snapshot.AppendLine();
        }

        snapshot.Append($"Status Code: {value.StatusCode}");

        snapshot.AppendLine();
        snapshot.Append("-------------------------->");
        snapshot.AppendLine();

        snapshot.Append(value.Content.ReadAsStringAsync().Result);
    }
}
