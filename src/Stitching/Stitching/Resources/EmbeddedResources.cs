using System;
using System.IO;
using System.Reflection;

namespace HotChocolate.Stitching
{
    internal static class EmbeddedResources
    {
        public static string OpenText(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The resourceName name mustn't benull or empty.",
                    nameof(resourceName));
            }

            var assembly = typeof(EmbeddedResources).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(
                "HotChocolate.Stitching.Resources." + resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
