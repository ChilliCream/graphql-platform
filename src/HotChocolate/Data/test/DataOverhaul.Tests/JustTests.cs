using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public class ReferenceCacheBuilder
{
    private readonly Dictionary<Identifier, IBox> _boxes = new();

    public Identifier<T> AddVariable<T>()
        where T : IEquatable<T>
    {
        var id = Identifier.FromIndex(_boxes.Count + 1);
        _boxes.Add(id, new Box<T>());
        return new(id);
    }

    internal ExpressionTreeCache Build(SealedMetaTree tree)
    {
        var cachedExpressions = new CachedExpression[tree.Nodes.Length];
        var boxes = _boxes;
        var boxExpressions = boxes.ToDictionary(kvp => kvp.Key, kvp => BoxExpressions.Create(kvp.Value));
        var variableContext = new VariableContext(boxExpressions, _boxes);
        var result = new ExpressionTreeCache(tree, cachedExpressions, variableContext);

        var context = result.Context;
        for (int i = 0; i < tree.Nodes.Length; i++)
        {
            var node = tree.Nodes[i];
            if (node.AllDependencies.HasNoDependencies)
            {
                context.NodeIndex = i;
                cachedExpressions[i].Expression = node.ExpressionFactory.GetExpression(context);
            }
        }

        return result;
    }
}
public class JustTests
{
#pragma warning disable CS8618
    public sealed class Variables
    {
        public string Name { get; set; }
        public string Computed => "Hello " + Name;
        public int Age { get; set; }
        public bool Flag { get; set; }
    }
#pragma warning restore CS8618

    [Fact]
    public void Test()
    {
        // Done per tree (aka per operation?)
        var referenceCacheBuilder = new ReferenceCacheBuilder();

        var nameId = referenceCacheBuilder.AddVariable<string>();
        var computedId = referenceCacheBuilder.AddVariable<string>();
        var ageId = referenceCacheBuilder.AddVariable<int>();
        var flagId = referenceCacheBuilder.AddVariable<bool>();

        var nameValue = new VariableValue(nameId);
        var computedValue = new VariableValue(computedId);
        var ageValue = new VariableValue(ageId);
        var optional = new HideChildOptionally(flagId);
        var boxAgeInteger = BoxValueType.Instance;

        var expressionPool = new ExpressionNodePool();
        var scopePool = new ScopePool();
        var pools = new ExpressionPools(expressionPool, scopePool);

        var rootNode = expressionPool.CreateInnermost(ObjectCreationAsObjectArray.Instance);
        var metaTree = new PlanMetaTree(
            new Dictionary<Identifier, ExpressionNode>(),
            rootNode);

        var nameNode = expressionPool.CreateInnermost(nameValue);
        var computedNode = expressionPool.CreateInnermost(computedValue);
        var ageNode = expressionPool.CreateInnermost(ageValue);
        ageNode.ExpectedType = ageId.Type;

        var optionalNode = expressionPool.Create(optional);
        MetaTreeConstruction.WrapExpressionNode(
            wrapperNode: optionalNode,
            nodeToWrap: ageNode);
        var boxedOptionalNode = expressionPool.Create(boxAgeInteger);
        MetaTreeConstruction.WrapExpressionNode(
            wrapperNode: boxedOptionalNode,
            nodeToWrap: optionalNode);

        var children = rootNode.Children;
        children.Add(nameNode);
        children.Add(computedNode);
        children.Add(boxedOptionalNode);

        var sealedMetaTree = metaTree.Seal(pools);
        expressionPool.EnsureNoLeaks();

        var referenceCache = referenceCacheBuilder.Build(sealedMetaTree);
        var cacheManager = new ProjectionExpressionCacheManager(referenceCache);

        {
            using var cacheLease = cacheManager.LeaseCache();
            cacheLease.SetVariableValue(nameId, "John");
            cacheLease.SetVariableValue(computedId, "Hello, " + "John");
            cacheLease.SetVariableValue(ageId, 5);
            cacheLease.SetVariableValue(flagId, false);

            var expression = cacheLease.GetRootExpression();
            var lambda = Expression.Lambda<Func<object?[]>>(expression);
            var deleg = lambda.Compile();
            var result = deleg();

            Assert.Equal(new object[] { "John", "Hello, John", 5 }, result);
        }

        {
            using var cacheLease = cacheManager.LeaseCache();
            cacheLease.SetVariableValue(nameId, "John1");
            cacheLease.SetVariableValue(computedId, "Hello, " + "John1");
            cacheLease.SetVariableValue(ageId, 15);
            cacheLease.SetVariableValue(flagId, true);

            var expression = cacheLease.GetRootExpression();
            var lambda = Expression.Lambda<Func<object?[]>>(expression);
            var deleg = lambda.Compile();
            var result = deleg();

            Assert.Equal(new object?[] { "John1", "Hello, John1", null }, result);
        }
    }
}
