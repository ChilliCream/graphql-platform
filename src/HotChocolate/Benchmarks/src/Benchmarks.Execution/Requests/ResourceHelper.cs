using System.IO;
using System.Reflection;
using System.Text;

namespace HotChocolate.Benchmarks
{
    public static class Resources
    {
        private const string _resourcePath = "HotChocolate.Benchmarks.Requests";
        private static readonly Assembly _assembly = typeof(Resources).Assembly;
        private static string? _introspection;
        private static string? _sessionLargeQuery;
        private static string? _sessionLargeDLQuery;
        private static string? _sessionMediumQuery;

        public static string Introspection => 
            _introspection ??= GetResourceString(nameof(Introspection));

        public static string SessionLargeQuery => 
            _sessionLargeQuery ??= GetResourceString(nameof(SessionLargeQuery));

        public static string SessionLargeDLQuery => 
            _sessionLargeDLQuery ??= GetResourceString(nameof(SessionLargeDLQuery));

        public static string SessionMediumQuery => 
            _sessionMediumQuery ??= GetResourceString(nameof(SessionMediumQuery));

        public static string SchemaMedium => 
            _introspection ??= GetResourceString(nameof(SchemaMedium));

        public static string SchemaLarge => 
            _introspection ??= GetResourceString(nameof(SchemaLarge));

        public static string GetResourceString(string fileName)
        {
            Stream? stream = GetResourceStream(fileName + ".graphql");

            if (stream is null)
            {
                throw new FileNotFoundException(
                    "Could not find the specified resource file",
                    fileName);
            }


            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            finally
            {
                stream.Dispose();
            }
        }

        private static Stream? GetResourceStream(string fileName)
        {
            return _assembly.GetManifestResourceStream($"{_resourcePath}.{fileName}");
        }
    }
}
