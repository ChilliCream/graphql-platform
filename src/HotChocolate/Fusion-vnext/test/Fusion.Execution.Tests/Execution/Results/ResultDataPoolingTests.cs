using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.Extensions.DependencyInjection.HotChocolateFusionServiceCollectionExtensions;

namespace HotChocolate.Fusion.Execution.Results;

public class ResultDataPoolingTests
{
    [Fact]
    public void Rent_Single_Result()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions());
        var services = serviceCollection.BuildServiceProvider();

        ObjectFieldResult obj;
        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();
            obj = session.RentObjectFieldResult();
            obj.IsInvalidated = true;
        }

        Assert.False(obj.IsInvalidated);
    }

    [Fact]
    public void Rent_Multiple_Objects_From_Same_Batch()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions { ObjectFieldBatchSize = 8 });
        var services = serviceCollection.BuildServiceProvider();

        var objects = new List<ObjectFieldResult>();

        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

            // Rent multiple objects from the same batch
            for (var i = 0; i < 5; i++)
            {
                var obj = session.RentObjectFieldResult();
                obj.IsInvalidated = true;
                objects.Add(obj);
            }
        }

        // All objects should be reset
        Assert.All(objects, obj => Assert.False(obj.IsInvalidated));
    }

    [Fact]
    public void Rent_Exceeds_Batch_Size_Creates_New_Batch()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions { ObjectFieldBatchSize = 8 });
        var services = serviceCollection.BuildServiceProvider();

        var objects = new List<ObjectFieldResult>();

        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

            // Rent more objects than batch size
            for (var i = 0; i < 10; i++)
            {
                var obj = session.RentObjectFieldResult();
                obj.IsInvalidated = true;
                objects.Add(obj);
            }
        }

        // All objects should be reset regardless of which batch they came from
        Assert.All(objects, obj => Assert.False(obj.IsInvalidated));
    }

    [Fact]
    public void All_Result_Types_Are_Properly_Reset()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions());
        var services = serviceCollection.BuildServiceProvider();

        ObjectResult objectResult;
        LeafFieldResult leafFieldResult;
        ListFieldResult listFieldResult;
        ObjectFieldResult objectFieldResult;
        ObjectListResult objectListResult;
        NestedListResult nestedListResult;
        LeafListResult leafListResult;

        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

            objectResult = session.RentObjectResult();
            leafFieldResult = session.RentLeafFieldResult();
            listFieldResult = session.RentListFieldResult();
            objectFieldResult = session.RentObjectFieldResult();
            objectListResult = session.RentObjectListResult();
            nestedListResult = session.RentNestedListResult();
            leafListResult = session.RentLeafListResult();

            // Modify all objects
            objectResult.IsInvalidated = true;
            leafFieldResult.IsInvalidated = true;
            listFieldResult.IsInvalidated = true;
            objectFieldResult.IsInvalidated = true;
            objectListResult.IsInvalidated = true;
            nestedListResult.IsInvalidated = true;
            leafListResult.IsInvalidated = true;
        }

        // All should be reset
        Assert.False(objectResult.IsInvalidated);
        Assert.False(leafFieldResult.IsInvalidated);
        Assert.False(listFieldResult.IsInvalidated);
        Assert.False(objectFieldResult.IsInvalidated);
        Assert.False(objectListResult.IsInvalidated);
        Assert.False(nestedListResult.IsInvalidated);
        Assert.False(leafListResult.IsInvalidated);
    }

    [Fact]
    public void Multiple_Scopes_Reuse_Same_Objects()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions { ObjectFieldBatchSize = 8 });
        var services = serviceCollection.BuildServiceProvider();

        ObjectFieldResult firstScopeObject;
        ObjectFieldResult secondScopeObject;

        // First scope
        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();
            firstScopeObject = session.RentObjectFieldResult();
        }

        // Second scope - should reuse the same object
        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();
            secondScopeObject = session.RentObjectFieldResult();
        }

        // Should be the same instance (object reuse)
        Assert.Same(firstScopeObject, secondScopeObject);
    }

    [Fact]
    public async Task Concurrent_Access_To_Different_Scopes()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions { ObjectFieldBatchSize = 8 });
        var services = serviceCollection.BuildServiceProvider();

        var tasks = new List<Task<ObjectFieldResult>>();
        var barrier = new Barrier(16);

        // Create 16 concurrent tasks that each rent from their own scope
        for (var i = 0; i < 16; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                using var scope = services.CreateScope();
                var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

                barrier.SignalAndWait(); // Synchronize all threads

                var obj = session.RentObjectFieldResult();
                obj.IsInvalidated = true;
                return obj;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // All objects should be reset after scopes are disposed
        Thread.Sleep(100); // Give time for disposal
        Assert.All(results, obj => Assert.False(obj.IsInvalidated));
    }

    [Fact]
    public void Batch_Exhaustion_Triggers_New_Batch_Creation()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions { ObjectFieldBatchSize = 8 });
        var services = serviceCollection.BuildServiceProvider();

        using var scope = services.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

        var objects = new List<ObjectFieldResult>();

        // Rent exactly batch size + 1 to trigger new batch
        for (var i = 0; i < 9; i++)
        {
            objects.Add(session.RentObjectFieldResult());
        }

        // All should be non-null
        Assert.All(objects, Assert.NotNull);

        // All should be different instances
        for (var i = 0; i < objects.Count; i++)
        {
            for (var j = i + 1; j < objects.Count; j++)
            {
                Assert.NotSame(objects[i], objects[j]);
            }
        }
    }

    [Fact]
    public void List_Objects_Reset_Their_Collections()
    {
        var serviceCollection = new ServiceCollection();
        AddResultObjectPools(serviceCollection, new FusionMemoryPoolOptions());
        var services = serviceCollection.BuildServiceProvider();

        ObjectListResult objectListResult;
        NestedListResult nestedListResult;
        LeafListResult leafListResult;

        using (var scope = services.CreateScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

            objectListResult = session.RentObjectListResult();
            nestedListResult = session.RentNestedListResult();
            leafListResult = session.RentLeafListResult();

            // Add items to lists
            objectListResult.Items.Add(null);
            objectListResult.Items.Add(null);
            nestedListResult.Items.Add(null);
            leafListResult.Items.Add(default);
            leafListResult.Items.Add(default);
        }

        // All collections should be cleared
        Assert.Empty(objectListResult.Items);
        Assert.Empty(nestedListResult.Items);
        Assert.Empty(leafListResult.Items);
    }
}
