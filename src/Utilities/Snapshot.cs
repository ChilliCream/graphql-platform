using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HotChocolate
{
    public static class Snapshot
    {
        private readonly static JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = new[]
            {
                new StringEnumConverter()
            }
        };

        public static string Current([CallerMemberNameAttribute]string snapshotName = null)
        {
            string fielPath = Path.Combine(
                "__snapshots__", snapshotName + ".json");
            if (File.Exists(fielPath))
            {
                return File.ReadAllText(fielPath);
            }

            fielPath = Path.Combine(
                "__snapshots__", snapshotName + ".txt");
            if (File.Exists(fielPath))
            {
                return File.ReadAllText(fielPath);
            }

            return null;
        }

        public static string New(object obj,
            [CallerMemberNameAttribute]string snapshotName = null)
        {
            string snapshot = null;

            // save new snapshot
            string directoryPath = Path.Combine("__snapshots__", "new");
            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch { }
            }

            if (obj is string s)
            {
                snapshot = NormalizeLineBreaks(s);

                File.WriteAllText(
                    Path.Combine(directoryPath, snapshotName + ".txt"),
                    snapshot);
            }
            else
            {
                snapshot = NormalizeLineBreaks(
                   JsonConvert.SerializeObject(obj, _settings));

                File.WriteAllText(
                    Path.Combine(directoryPath, snapshotName + ".json"),
                    snapshot);
            }

            // return new snapshot
            return snapshot;
        }

        private static string NormalizeLineBreaks(string snapshot)
        {
            return snapshot.Replace("\r", string.Empty) + "\n";
        }
    }
}
