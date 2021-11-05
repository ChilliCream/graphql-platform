using Microsoft.Azure.WebJobs.Description;

namespace HotChocolate.AzureFunctions;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class GraphQLAttribute : Attribute
{
}
