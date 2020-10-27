using System;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore
{
    public sealed class GraphQLEndpointConventionBuilder
        : IGraphQLEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _endpointConventionBuilder;

        public GraphQLEndpointConventionBuilder(
            IEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilder = endpointConventionBuilder ??
                throw new ArgumentNullException(nameof(endpointConventionBuilder));
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            _endpointConventionBuilder.Add(convention);
        }
    }
}
