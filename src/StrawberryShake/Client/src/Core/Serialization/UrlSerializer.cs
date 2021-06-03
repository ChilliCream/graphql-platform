using System;

namespace StrawberryShake.Serialization
{
    public class UrlSerializer
        : ScalarSerializer<string, Uri>
    {
        public UrlSerializer(string typeName = BuiltInScalarNames.Url)
            : base(typeName)
        {
        }

        public override Uri Parse(string serializedValue)
        {
            if (!Uri.TryCreate(serializedValue, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                throw ThrowHelper.UrlFormatter_CouldNotParseUri(serializedValue);
            }

            // Don't accept a relative URI that does not start with '/'
            if (!uri.IsAbsoluteUri && !uri.OriginalString.StartsWith("/"))
            {
                throw ThrowHelper.UrlFormatter_DoesNotStartWithSlash(uri.OriginalString);
            }

            return uri;
        }

        protected override string Format(Uri runtimeValue) => runtimeValue.AbsolutePath;
    }
}
