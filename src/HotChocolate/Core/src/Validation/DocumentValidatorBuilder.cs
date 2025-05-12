using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation;

public sealed class DocumentValidatorBuilder
{
    private readonly List<RuleConfiguration> _rules = [];
    private IServiceProvider _services = EmptyServiceProvider.Instance;
    private int _maxAllowedErrors;

    private DocumentValidatorBuilder()
    {
    }

    public static DocumentValidatorBuilder New() => new();

    public DocumentValidatorBuilder SetServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        return this;
    }

    public DocumentValidatorBuilder SetMaxAllowedErrors(int maxErrors)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxErrors);
        _maxAllowedErrors = maxErrors;
        return this;
    }

    public DocumentValidatorBuilder AddRule<TRule>(
        int priority = ushort.MaxValue)
        where TRule : class, IDocumentValidatorRule
    {
        ArgumentNullException.ThrowIfNull(typeof(TRule));
        _rules.Add(RuleConfiguration.CreateRule<TRule>(priority));
        return this;
    }

    public DocumentValidatorBuilder AddVisitor<TVisitor>(
        int priority = ushort.MaxValue)
        where TVisitor : DocumentValidatorVisitor
    {
        ArgumentNullException.ThrowIfNull(typeof(TVisitor));
        _rules.Add(RuleConfiguration.CreateVisitor<TVisitor>(priority));
        return this;
    }

    public DocumentValidator Build()
    {
        var completed = new HashSet<Type>();
        var rules = new List<IDocumentValidatorRule>();

        foreach (var configuration in _rules.OrderBy(r => r.Priority))
        {
            if (configuration.IsVisitor)
            {
                var visitor = CreateInstance<DocumentValidatorVisitor>(configuration.Rule);
                rules.Add(new DocumentValidatorRule(visitor));
            }
            else
            {
                var rule = CreateInstance<IDocumentValidatorRule>(configuration.Rule);
                rules.Add(rule);
            }
        }

        var contextPool = _services.GetService<ObjectPool<DocumentValidatorContext>>();
        contextPool ??= new DocumentValidatorContextPool();
        return new DocumentValidator(contextPool, [.. rules], _maxAllowedErrors);
    }

    private T CreateInstance<T>(Type type)
    {
        var instance = ActivatorUtilities.GetServiceOrCreateInstance(_services, type);

        if (instance is not T casted)
        {
            throw new InvalidOperationException(
                $"The type {type.FullName} is not of type {typeof(T).FullName}.");
        }

        return casted;
    }

    private sealed class RuleConfiguration
    {
        private RuleConfiguration(Type rule, int priority, bool isCacheable, bool isVisitor = false)
        {
            Rule = rule;
            Priority = priority;
            IsCacheable = isCacheable;
            IsVisitor = isVisitor;
        }

        public readonly Type Rule;

        public readonly int Priority;

        public readonly bool IsCacheable;

        public readonly bool IsVisitor;

        public static RuleConfiguration CreateRule<TRule>(
            int priority = ushort.MaxValue)
            where TRule : IDocumentValidatorRule
            => new(typeof(TRule), priority, isCacheable: true);

        public static RuleConfiguration CreateVisitor<TVisitor>(
            int priority = ushort.MaxValue)
            where TVisitor : DocumentValidatorVisitor
            => new(typeof(TVisitor), priority, isCacheable: false, isVisitor: true);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => null;

        public static readonly EmptyServiceProvider Instance = new();
    }
}
