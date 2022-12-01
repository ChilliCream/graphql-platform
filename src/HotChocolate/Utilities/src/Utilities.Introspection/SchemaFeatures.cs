using System;
using System.Linq;
using static HotChocolate.Utilities.Introspection.WellKnownTypes;

namespace HotChocolate.Utilities.Introspection
{
    internal sealed class SchemaFeatures : ISchemaFeatures
    {
        public const string Locations = "locations";
        public const string IsRepeatable = "isRepeatable";
        public const string SubscriptionType = "subscriptionType";

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

            var directive = result.Data.Schema.Types.FirstOrDefault(t =>
                 t.Name.Equals(__Directive, StringComparison.Ordinal));
            if (directive is not null)
            {
                features.HasRepeatableDirectives = directive.Fields.Any(t =>
                    t.Name.Equals(IsRepeatable, StringComparison.Ordinal));
                features.HasDirectiveLocations = directive.Fields.Any(t =>
                    t.Name.Equals(Locations, StringComparison.Ordinal));
            }

            var schema = result.Data.Schema.Types.FirstOrDefault(t =>
                 t.Name.Equals(__Schema, StringComparison.Ordinal));
            if (schema is not null)
            {
                features.HasSubscriptionSupport = schema.Fields.Any(t =>
                    t.Name.Equals(SubscriptionType, StringComparison.Ordinal));
            }

            return features;
        }
    }
}
