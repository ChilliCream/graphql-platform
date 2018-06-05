using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate
{
    public partial class Schema
    {
        private readonly struct SchemaNames
        {
            public SchemaNames(
                string queryTypeName,
                string mutationTypeName,
                string subscriptionTypeName)
            {
                QueryTypeName = string.IsNullOrEmpty(queryTypeName)
                    ? "Query" : queryTypeName;
                MutationTypeName = string.IsNullOrEmpty(mutationTypeName)
                    ? "Mutation" : mutationTypeName;
                SubscriptionTypeName = string.IsNullOrEmpty(subscriptionTypeName)
                    ? "Subscription" : subscriptionTypeName;
            }

            public string QueryTypeName { get; }
            public string MutationTypeName { get; }
            public string SubscriptionTypeName { get; }
        }
    }
}
