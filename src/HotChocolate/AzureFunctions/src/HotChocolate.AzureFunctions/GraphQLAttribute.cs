using Microsoft.Azure.WebJobs.Description;

namespace HotChocolate.AzureFunctions;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class GraphQLAttribute : Attribute
{
}
