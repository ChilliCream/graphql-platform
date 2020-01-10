using System;
using System.Linq;

namespace HotChocolate.Utilities.Introspection
{
    internal sealed class SchemaFeatures : ISchemaFeatures
    {
        private const string _schemaName = "__Schema";
        private const string _directiveName = "__Directive";
        private const string _locations = "locations";
        private const string _isRepeatable = "isRepeatable";
        private const string _subscriptionType = "subscriptionType";

        private SchemaFeatures()
        {
        }

        public bool HasDirectiveLocations { get; private set; }

        public bool HasRepeatableDirectives { get; private set; }

        public bool HasSubscriptionSupport { get; private set; }

        internal static SchemaFeatures FromIntrospectionResult(
            IntrospectionResult result)
        {
            var features = new SchemaFeatures();
            FullType directive = result.Data.Schema.Types.First(t =>
                 t.Name.Equals(_directiveName, StringComparison.Ordinal));
            features.HasRepeatableDirectives = directive.Fields.Any(t =>
                t.Name.Equals(_isRepeatable, StringComparison.Ordinal));
            features.HasDirectiveLocations = directive.Fields.Any(t =>
                t.Name.Equals(_locations, StringComparison.Ordinal));

            FullType schema = result.Data.Schema.Types.First(t =>
                 t.Name.Equals(_schemaName, StringComparison.Ordinal));
            features.HasSubscriptionSupport = schema.Fields.Any(t =>
                t.Name.Equals(_subscriptionType, StringComparison.Ordinal));

            return features;
        }
    }
}
