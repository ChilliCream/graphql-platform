using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace HotChocolate.Data.Sorting;

public class Issue6545ReproTests
{
    [Fact]
    public async Task Sorting_With_Variables_Works_For_Custom_Object_Sort_Field_Without_Default_Constructor()
    {
        Subject[] subjects =
        [
            new Subject(
                "Subject-B",
                [
                    new Address(AddressType.LegalResidential, "Zurich")
                ]),
            new Subject(
                "Subject-A",
                [
                    new Address(AddressType.LegalResidential, "Amsterdam")
                ])
        ];

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddSorting()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Name("Query")
                        .Field("root")
                        .Resolve(subjects)
                        .UseSorting<SubjectSortInputType>();
                })
            .BuildRequestExecutorAsync();

        var inlineResult = await executor.ExecuteAsync(
            """
            {
              root(order: { legalResidentialAddress: { cityName: ASC } }) {
                name
              }
            }
            """);

        var variableResult = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query Test($order: [SubjectSortInput!]) {
                      root(order: $order) {
                        name
                      }
                    }
                    """)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        {
                            "order",
                            new object[]
                            {
                                new Dictionary<string, object?>
                                {
                                    {
                                        "legalResidentialAddress",
                                        new Dictionary<string, object?>
                                        {
                                            { "cityName", "ASC" }
                                        }
                                    }
                                }
                            }
                        }
                    })
                .Build());

        Assert.False(
            inlineResult.ExpectOperationResult().Errors?.Count > 0,
            inlineResult.ToJson());
        Assert.False(
            variableResult.ExpectOperationResult().Errors?.Count > 0,
            variableResult.ToJson());

        using var inlineJson = JsonDocument.Parse(inlineResult.ToJson());
        using var variableJson = JsonDocument.Parse(variableResult.ToJson());

        var inlineData = inlineJson.RootElement.GetProperty("data").GetProperty("root");
        var variableData = variableJson.RootElement.GetProperty("data").GetProperty("root");

        Assert.Equal(inlineData.GetArrayLength(), variableData.GetArrayLength());
        Assert.Equal(
            inlineData[0].GetProperty("name").GetString(),
            variableData[0].GetProperty("name").GetString());
        Assert.Equal("Subject-A", variableData[0].GetProperty("name").GetString());
    }

    public class SubjectSortInputType : SortInputType<Subject>
    {
        protected override void Configure(ISortInputTypeDescriptor<Subject> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor
                .Field(x =>
                    x.Addresses.FirstOrDefault(address => address.AddressType == AddressType.LegalResidential))
                .Name("legalResidentialAddress")
                .Type<AddressSortInputType>();
        }
    }

    public class AddressSortInputType : SortInputType<Address>;

    public class Subject(string name, IReadOnlyList<Address> addresses)
    {
        public string Name { get; } = name;

        public IReadOnlyList<Address> Addresses { get; } = addresses;
    }

    public class Address(AddressType addressType, string cityName)
    {
        public AddressType AddressType { get; } = addressType;

        public string CityName { get; } = cityName;
    }

    public enum AddressType
    {
        LegalResidential
    }
}
