using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HotChocolate
{
    public static class Snapshot
    {
        private static readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = new[]
                {
                    new StringEnumConverter()
                }
            };

        public static string Current(
            [CallerMemberNameAttribute]string snapshotName = null)
        {
            string filePath = Path.Combine(
                "__snapshots__", snapshotName + ".json");
            if (File.Exists(filePath))
            {
                return NormalizeLineBreaks(File.ReadAllText(filePath));
            }

            filePath = Path.Combine(
                "__snapshots__", snapshotName + ".txt");
            if (File.Exists(filePath))
            {
                return NormalizeLineBreaks(File.ReadAllText(filePath));
            }

            return null;
        }

        public static string New(object obj,
            [CallerMemberNameAttribute]string snapshotName = null)
        {
            string snapshot = null;

            // save new snapshot
            string directoryPath = Path.Combine("__snapshots__new");

            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch
                {
                    // ignore any error
                }
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

        public static void Clean(object obj,
            [CallerMemberNameAttribute]string snapshotName = null)
        {
            string directoryPath = Path.Combine("__snapshots__new");

            if (Directory.Exists(directoryPath))
            {
                if (obj is string s)
                {
                    File.Delete(
                        Path.Combine(directoryPath, snapshotName + ".txt"));
                }
                else
                {
                    File.Delete(
                        Path.Combine(directoryPath, snapshotName + ".json"));
                }
            }
        }

        private static string NormalizeLineBreaks(string snapshot)
        {
            string s = snapshot.Replace("\r", string.Empty);
            if (s.Last() == '\n')
            {
                return s;
            }
            return s + "\n";
        }
    }
}
