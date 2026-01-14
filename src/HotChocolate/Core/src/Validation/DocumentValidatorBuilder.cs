using System.Diagnostics.CodeAnalysis;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation;

/// <summary>
/// The <see cref="DocumentValidatorBuilder"/> is used to create a new <see cref="DocumentValidator"/>.
/// </summary>
public sealed class DocumentValidatorBuilder
{
    private readonly List<RuleConfiguration> _rules = [];
    private readonly ValidationOptions _options = new();
    private IServiceProvider _services = EmptyServiceProvider.Instance;
    private List<Action<IServiceProvider, ValidationOptions>>? _optionModifiers;

    private DocumentValidatorBuilder() { }

    /// <summary>
    /// Creates a new instance of <see cref="DocumentValidatorBuilder"/>.
    /// </summary>
    /// <returns>
    /// Returns a new instance of <see cref="DocumentValidatorBuilder"/>.
    /// </returns>
    public static DocumentValidatorBuilder New() => new();

    /// <summary>
    /// Sets the service provider that will be used within the
    /// <see cref="DocumentValidator"/> to resolve services.
    /// </summary>
    /// <param name="services">
    /// The service provider.
    /// </param>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder SetServices(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        return this;
    }

    /// <summary>
    /// Modifies the <see cref="ValidationOptions"/>.
    /// </summary>
    /// <param name="configure">
    /// The configuration action.
    /// </param>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder ModifyOptions(
        Action<ValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _optionModifiers ??= [];
        _optionModifiers.Add((_, o) => configure(o));
        return this;
    }

    /// <summary>
    /// Modifies the <see cref="ValidationOptions"/>.
    /// </summary>
    /// <param name="configure">
    /// The configuration action.
    /// </param>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder ModifyOptions(
        Action<IServiceProvider, ValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _optionModifiers ??= [];
        _optionModifiers.Add(configure);
        return this;
    }

    /// <summary>
    /// Adds a rule to the <see cref="DocumentValidator"/>.
    /// </summary>
    /// <param name="factory">
    /// The factory to create the validation rule.
    /// </param>
    /// <param name="isEnabled">
    /// A delegate to determine if the rule is enabled and if it needs to be created
    /// when the <see cref="DocumentValidator"/> is built.
    /// </param>
    /// <typeparam name="TRule">
    /// The rule type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder AddRule<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRule>(
        Func<IServiceProvider, ValidationOptions, TRule>? factory = null,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
        where TRule : class, IDocumentValidatorRule
    {
        ArgumentNullException.ThrowIfNull(typeof(TRule));
        _rules.Add(RuleConfiguration.CreateRule<TRule>(isEnabled, factory));
        return this;
    }

    /// <summary>
    /// Removes a rule from the <see cref="DocumentValidator"/>.
    /// </summary>
    /// <typeparam name="TRule">
    /// The rule type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder RemoveRule<TRule>()
        where TRule : class, IDocumentValidatorRule
    {
        _rules.RemoveAll(r => r.Rule == typeof(TRule) && !r.IsVisitor);
        return this;
    }

    /// <summary>
    /// Adds a visitor to the <see cref="DocumentValidator"/> that
    /// will be used to create a validation rule.
    /// </summary>
    /// <param name="factory">
    /// The factory to create the validation visitor.
    /// </param>
    /// <param name="isEnabled">
    /// A delegate to determine if the visitor is enabled and if it needs to be created
    /// when the <see cref="DocumentValidator"/> is built.
    /// </param>
    /// <param name="priority">
    /// The priority of the visitor.
    /// </param>
    /// <param name="isCacheable">
    /// A flag to determine if the visitor is cacheable.
    /// </param>
    /// <typeparam name="TVisitor">
    /// The visitor type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder AddVisitor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TVisitor>(
        Func<IServiceProvider, ValidationOptions, TVisitor>? factory = null,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null,
        ushort priority = ushort.MaxValue,
        bool isCacheable = true)
        where TVisitor : DocumentValidatorVisitor
    {
        ArgumentNullException.ThrowIfNull(typeof(TVisitor));
        _rules.Add(RuleConfiguration.CreateVisitor<TVisitor>(isEnabled, factory, priority, isCacheable));
        return this;
    }

    /// <summary>
    /// Removes a visitor from the <see cref="DocumentValidator"/>.
    /// </summary>
    /// <typeparam name="TVisitor">
    /// The visitor type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="DocumentValidatorBuilder"/> for configuration chaining.
    /// </returns>
    public DocumentValidatorBuilder RemoveVisitor<TVisitor>()
        where TVisitor : DocumentValidatorVisitor
    {
        _rules.RemoveAll(r => r.Rule == typeof(TVisitor) && r.IsVisitor);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="DocumentValidator"/>.
    /// </summary>
    /// <returns>
    /// Returns the <see cref="DocumentValidator"/>.
    /// </returns>
    public DocumentValidator Build()
    {
        var rules = new List<IDocumentValidatorRule>();

        if (_optionModifiers is not null)
        {
            foreach (var modifier in _optionModifiers)
            {
                modifier(_services, _options);
            }
        }

        foreach (var configuration in _rules.OrderBy(r => r.Priority))
        {
            if (configuration.IsEnabled?.Invoke(_services, _options) == false)
            {
                continue;
            }

            if (configuration.IsVisitor)
            {
                var visitor = CreateInstance<DocumentValidatorVisitor>(
                    configuration.Rule,
                    configuration.Factory,
                    _services,
                    _options);

                var rule = new DocumentValidatorRule(
                    visitor,
                    configuration.IsCacheable ?? true,
                    configuration.Priority ?? ushort.MaxValue);

                rules.Add(rule);
            }
            else
            {
                var rule = CreateInstance<IDocumentValidatorRule>(
                    configuration.Rule,
                    configuration.Factory,
                    _services,
                    _options);

                rules.Add(rule);
            }
        }

        var contextPool = _services.GetService<ObjectPool<DocumentValidatorContext>>();
        contextPool ??= new DocumentValidatorContextPool();
        return new DocumentValidator(contextPool, [.. rules], _options.MaxAllowedErrors);
    }

    private static T CreateInstance<T>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        Func<IServiceProvider, ValidationOptions, object>? factory,
        IServiceProvider services,
        ValidationOptions options)
    {
        var instance = factory is null
            ? ActivatorUtilities.GetServiceOrCreateInstance(services, type)
            : factory(services, options);

        if (instance is not T casted)
        {
            throw new InvalidOperationException(
                $"The type {type.FullName} is not of type {typeof(T).FullName}.");
        }

        return casted;
    }

    private sealed class RuleConfiguration
    {
        private RuleConfiguration(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type rule,
            ushort? priority,
            bool? isCacheable,
            Func<IServiceProvider, ValidationOptions, bool>? isEnabled,
            Func<IServiceProvider, ValidationOptions, object>? factory,
            bool isVisitor = false)
        {
            Rule = rule;
            Priority = priority;
            IsCacheable = isCacheable;
            IsVisitor = isVisitor;
            IsEnabled = isEnabled;
            Factory = factory;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public readonly Type Rule;
        public readonly ushort? Priority;

        public readonly bool? IsCacheable;

        public readonly bool IsVisitor;

        public readonly Func<IServiceProvider, ValidationOptions, bool>? IsEnabled;

        public readonly Func<IServiceProvider, ValidationOptions, object>? Factory;

        public static RuleConfiguration CreateRule<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRule>(
            Func<IServiceProvider, ValidationOptions, bool>? isEnabled,
            Func<IServiceProvider, ValidationOptions, object>? factory)
            where TRule : IDocumentValidatorRule
            => new RuleConfiguration(
                typeof(TRule),
                priority: null,
                isCacheable: null,
                isEnabled: isEnabled,
                factory: factory);

        public static RuleConfiguration CreateVisitor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TVisitor>(
            Func<IServiceProvider, ValidationOptions, bool>? isEnabled,
            Func<IServiceProvider, ValidationOptions, object>? factory,
            ushort priority = ushort.MaxValue,
            bool isCacheable = false)
            where TVisitor : DocumentValidatorVisitor
            => new RuleConfiguration(
                typeof(TVisitor),
                priority: priority,
                isCacheable: isCacheable,
                isEnabled: isEnabled,
                factory: factory,
                isVisitor: true);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => null;

        public static readonly EmptyServiceProvider Instance = new();
    }
}
