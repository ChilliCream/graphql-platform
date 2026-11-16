using System.Runtime.ExceptionServices;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.ExecutionGroups;
using HotChocolate.Fetching;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// An initialized operation context and work scheduler for an operation whose first root field is an execution
/// group root.
/// </summary>
internal sealed class ExecutionGroupTestHarness
{
    private ExecutionGroupTestHarness(OperationContext operationContext)
    {
        OperationContext = operationContext;

        var operation = operationContext.Operation;
        var root = operation.RootSelectionSet.Selections[0];
        RootSet = operation.GetSelectionSet(root);
        Plan = RootSet.ExecutionGroupPlan!;
    }

    public OperationContext OperationContext { get; }

    public WorkScheduler Scheduler => OperationContext.Scheduler;

    public SelectionSet RootSet { get; }

    public ExecutionGroupPlan Plan { get; }

    public ConditionFlags IncludeFlags => OperationContext.IncludeConditionFlags;

    public Selection GetSelection(string fieldName)
        => RootSet.Selections.ToArray().Single(s => s.Field.Name == fieldName);

    public static Task<T> RunAsync<T>(string query, Func<ExecutionGroupTestHarness, T> test)
        => RunAsync(query, new Dictionary<string, object?>(), test);

    public static async Task<T> RunAsync<T>(
        string query,
        Dictionary<string, object?> variables,
        Func<ExecutionGroupTestHarness, T> test)
    {
        ExceptionDispatchInfo? failure = null;
        var outcome = default(T);

        var executor = await ExecutionGroupTestSchema.CreateAsync(
            new ExecutionScript(),
            builder => builder
                .UseDefaultPipeline()
                .UseRequest(
                    (factoryContext, _) =>
                    {
                        var contextFactory = factoryContext.Services
                            .GetRequiredService<IFactory<OperationContextOwner>>();

                        return context =>
                        {
                            // the owner is abandoned, because tasks that were registered are never executed.
                            var operationContext = contextFactory.Create().OperationContext;

                            operationContext.Initialize(
                                context,
                                context.RequestServices,
                                context.RequestServices.GetRequiredService<IBatchDispatcher>(),
                                context.GetOperation(),
                                context.VariableValues[0],
                                rootValue: null,
                                resolveQueryRootValue: () => null);

                            try
                            {
                                outcome = test(new ExecutionGroupTestHarness(operationContext));
                            }
                            catch (Exception ex)
                            {
                                failure = ExceptionDispatchInfo.Capture(ex);
                            }

                            context.Result = OperationResult.FromError(
                                ErrorBuilder.New().SetMessage("The test harness stops before execution.").Build());
                            return ValueTask.CompletedTask;
                        };
                    },
                    key: "ExecutionGroupTestHarness",
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware));

        var request = OperationRequestBuilder.New()
            .SetDocument(query)
            .SetVariableValues(variables)
            .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        failure?.Throw();
        return outcome!;
    }
}
