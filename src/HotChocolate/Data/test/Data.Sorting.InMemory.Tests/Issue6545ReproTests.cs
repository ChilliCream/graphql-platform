using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class Issue6545ReproTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Sort_When_ExpressionObjectFieldIsPassedAsVariable()
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
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
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
                    """
                    {
                      "order": [
                        {
                          "legalResidentialAddress": {
                            "cityName": "ASC"
                          }
                        }
                      ]
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "root": [
                  {
                    "name": "Subject-A"
                  },
                  {
                    "name": "Subject-B"
                  }
                ]
              }
            }
            """);
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
