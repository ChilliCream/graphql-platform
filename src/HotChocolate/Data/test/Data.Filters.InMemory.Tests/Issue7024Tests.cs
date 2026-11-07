using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class Issue7024Tests
{
    [Fact]
    public async Task Variable_Filter_With_Custom_List_Field_Works()
    {
        var machineIdWithLock = Guid.NewGuid();
        var machineIdWithoutLock = Guid.NewGuid();

        Machine[] machines =
        [
            new()
            {
                Id = machineIdWithLock,
                Locks = new HashSet<MachineLock>
                {
                    new(machineIdWithLock, LockType.Lock1)
                }
            },
            new()
            {
                Id = machineIdWithoutLock,
                Locks = new HashSet<MachineLock>()
            }
        ];

        var executor = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType(
                d => d
                    .Name(OperationTypeNames.Query)
                    .Field("machines")
                    .Resolve(_ => machines.AsQueryable())
                    .UseFiltering<MachineFilterType>())
            .AddType<MachineType>()
            .Create()
            .MakeExecutable();

        var inlineResult = await executor.ExecuteAsync(
            """
            {
              machines(where: { locks: { any: true } }) {
                id
              }
            }
            """);

        var variableResult = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($filter: MachineFilterInput) {
                      machines(where: $filter) {
                        id
                      }
                    }
                    """)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        ["filter"] = new Dictionary<string, object?>
                        {
                            ["locks"] = new Dictionary<string, object?>
                            {
                                ["any"] = true
                            }
                        }
                    })
                .Build());

        var inlineOperation = inlineResult.ExpectOperationResult();
        var variableOperation = variableResult.ExpectOperationResult();

        Assert.Empty(inlineOperation.Errors ?? []);
        Assert.Empty(variableOperation.Errors ?? []);

        Assert.Equal(
            inlineOperation.ToJson(),
            variableOperation.ToJson());
    }

    public class Machine
    {
        public Guid Id { get; set; }

        public ISet<MachineLock> Locks { get; set; } = new HashSet<MachineLock>();
    }

    public record MachineLock(Guid MachineId, LockType Lock);

    public enum LockType
    {
        Lock1,
        Lock2
    }

    public class MachineType : ObjectType<Machine>
    {
        protected override void Configure(IObjectTypeDescriptor<Machine> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Id);
        }
    }

    public class MachineFilterType : FilterInputType<Machine>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Machine> descriptor)
        {
            descriptor.Name("MachineFilterInput");
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Locks.Select(y => y.Lock)).Name("locks");
        }
    }
}
