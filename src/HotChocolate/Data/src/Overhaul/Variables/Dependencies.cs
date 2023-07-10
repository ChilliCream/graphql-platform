using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

public readonly struct StructuralDependencies
{
    [MemberNotNullWhen(false, nameof(Unspecified))]
    public IReadOnlySet<Identifier>? VariableIds { get; init; }

    public bool Unspecified => VariableIds is null;

    public static StructuralDependencies All => new() { VariableIds = null };

    private static readonly HashSet<Identifier> _none = new();
    public static StructuralDependencies None => new() { VariableIds = _none };
}

public readonly struct Dependencies
{
    public StructuralDependencies Structural { get; init; }

    // TODO: further decrease the number of computed box expressions by keeping track
    //       of which expression dependencies are used as well.
    //       This can be done by having a hash set here instead of a bool
    public bool HasExpressionDependencies { get; init; }

    public bool HasNoDependencies => Structural.VariableIds?.Count == 0 && !HasExpressionDependencies;
}

public readonly struct VariableExpressionsEnumerable
{
    public VariableExpressionsEnumerable(
        StructuralDependencies dependencies,
        IVariableContext context)
    {
        _dependencies = dependencies;
        _context = context;
    }

    private readonly StructuralDependencies _dependencies;
    private readonly IVariableContext _context;

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator
    {
        private readonly IVariableContext _variables;
        private readonly bool _iteratingAll;
        private HashSet<Identifier>.Enumerator _idEnumerator;
        private Dictionary<Identifier, BoxExpressions>.Enumerator _boxExpressionsEnumerator;

        public Enumerator(VariableExpressionsEnumerable enumerable)
        {
            _variables = enumerable._context;
            _iteratingAll = enumerable._dependencies.Unspecified;
            if (_iteratingAll)
                _idEnumerator = ((HashSet<Identifier>) enumerable._dependencies.VariableIds!).GetEnumerator();
            else
                _boxExpressionsEnumerator = ((Dictionary<Identifier, BoxExpressions>) enumerable._context.Expressions).GetEnumerator();
        }

        public bool MoveNext()
        {
            if (_iteratingAll)
                return _boxExpressionsEnumerator.MoveNext();
            else
                return _idEnumerator.MoveNext();
        }

        public readonly (Identifier Id, BoxExpressions Box) Current
        {
            get
            {
                if (_iteratingAll)
                {
                    var (id, box) = _boxExpressionsEnumerator.Current;
                    return (id, box);
                }
                else
                {
                    var id = _idEnumerator.Current;
                    var box = _variables.GetExpressions(id);
                    return (id, box);
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DependencyAttribute : Attribute
{
    public bool Structural { get; set; }
    public bool Expression { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class NoStructuralDependenciesAttribute : Attribute
{
}

public interface IGetDependencies
{
    Dependencies Dependencies { get; }
}

public static class DependencyHelper
{
    private readonly struct Cache
    {
        [MemberNotNullWhen(false, nameof(Unspecified))]
        public List<Func<IExpressionFactory, Identifier>>? StructuralDependencyGetters { get; init; }
        public bool NoDependencies { get; init; }
        public bool NoExpressionDependencies { get; init; }

        public readonly bool Unspecified => !NoDependencies && StructuralDependencyGetters is null;

        public static Cache All => default;
        public static Cache None => new() { NoDependencies = true, NoExpressionDependencies = true };
        public static Cache Structural(List<Func<IExpressionFactory, Identifier>> getters)
            => new() { StructuralDependencyGetters = getters.ToList() };
    }
    private static readonly ConcurrentDictionary<Type, Cache> _cache = new();

    private static Func<IExpressionFactory, Identifier> GetGetIdentifier1<TFactory>(
        Func<TFactory, Identifier> getter)
        where TFactory : IExpressionFactory
    {
        return f => getter((TFactory) f);
    }

    private static Func<IExpressionFactory, Identifier> GetGetIdentifier2<TFactory, T>(
        Func<TFactory, Identifier<T>> getter)
        where TFactory : IExpressionFactory
    {
        return f => getter((TFactory) f).Id;
    }

    private static readonly MethodInfo _getGetIdentifier1MethodInfo = typeof(DependencyHelper)
        .GetMethod(nameof(GetGetIdentifier1), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo _getGetIdentifier2MethodInfo = typeof(DependencyHelper)
        .GetMethod(nameof(GetGetIdentifier2), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static Dependencies GetDependencies(IExpressionFactory factory)
    {
        if (factory is IGetDependencies deps)
            return deps.Dependencies;

        var type = factory.GetType();
        var dependencies = new HashSet<Identifier>();
        var cache = _cache.GetOrAdd(type, CreateCache);

        StructuralDependencies GetStructural()
        {
            if (cache.Unspecified)
                return StructuralDependencies.All;
            if (cache.NoDependencies)
                return StructuralDependencies.None;

            foreach (var depGetter in cache.StructuralDependencyGetters!)
                dependencies.Add(depGetter(factory));

            return new() { VariableIds = dependencies };
        }

        return new()
        {
            Structural = GetStructural(),
            HasExpressionDependencies = !cache.NoExpressionDependencies,
        };
    }

    private static Cache CreateCache(Type type)
    {
        if (type.CustomAttributes.Any(a => a.AttributeType == typeof(NoStructuralDependenciesAttribute)))
            return Cache.None;

        var getters = new List<Func<IExpressionFactory, Identifier>>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        bool isUnspecified = true;
        bool hasNoExpressionDependency = true;
        foreach (var p in properties)
        {
            if (p.GetCustomAttribute<DependencyAttribute>() is not { } deps)
                continue;

            if (p.GetMethod is not { } getMethod)
                throw new InvalidOperationException("Invalid attribute usage: the property must have a public getter.");
            isUnspecified = false;

            if (deps.Expression)
                hasNoExpressionDependency = false;

            if (!deps.Structural)
                continue;

            var idType = p.PropertyType;
            var getterFunc = getMethod.CreateDelegate(
                typeof(Func<,>).MakeGenericType(type, idType));
            if (idType == typeof(Identifier))
            {
                var adapterFunc = _getGetIdentifier1MethodInfo
                    .MakeGenericMethod(type)
                    .Invoke(null, new object?[] { getterFunc })!;
                getters.Add((Func<IExpressionFactory, Identifier>) adapterFunc);
            }
            else if (idType.IsGenericType && idType.GetGenericTypeDefinition() == typeof(Identifier<>))
            {
                var valueType = idType.GetGenericArguments()[0];
                var convertedGetterFunc = _getGetIdentifier2MethodInfo
                    .MakeGenericMethod(type, valueType)
                    .Invoke(null, new object?[] { getterFunc })!;
                getters.Add((Func<IExpressionFactory, Identifier>) convertedGetterFunc);
            }
            else
            {
                throw new InvalidOperationException("Invalid attribute usage: the property must be of type Identifier or Identifier<T>.");
            }
        }

        if (isUnspecified)
            return Cache.All;

        return Cache.Structural(getters) with
        {
            NoExpressionDependencies = hasNoExpressionDependency
        };
    }
}
