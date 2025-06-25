using System.Buffers;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// The <see cref="InterfaceFieldConfiguration"/> contains the settings
/// to create a <see cref="InterfaceField"/>.
/// </summary>
public class InterfaceFieldConfiguration : OutputFieldConfiguration
{
    private List<FieldMiddlewareConfiguration>? _middlewareDefinitions;
    private List<ResultFormatterConfiguration>? _resultConverters;
    private List<IParameterExpressionBuilder>? _expressionBuilders;
    private bool _middlewareDefinitionsCleaned;
    private bool _resultConvertersCleaned;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeConfiguration"/>.
    /// </summary>
    public InterfaceFieldConfiguration()
    {
        IsParallelExecutable = true;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeConfiguration"/>.
    /// </summary>
    public InterfaceFieldConfiguration(
        string name,
        string? description = null,
        TypeReference? type = null)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Type = type;
        IsParallelExecutable = true;
    }

    /// <summary>
    /// The object runtime type.
    /// </summary>
    public Type? SourceType { get; set; }

    /// <summary>
    /// The resolver type that exposes the resolver member.
    /// </summary>
    public Type? ResolverType { get; set; }

    /// <summary>
    /// Gets the interface member to which this field is bound to.
    /// </summary>
    public MemberInfo? Member { get; set; }

    /// <summary>
    /// Defines a binding to another object field.
    /// </summary>
    public ObjectFieldBinding? BindToField { get; set; }

    /// <summary>
    /// The member that represents the resolver.
    /// </summary>
    public MemberInfo? ResolverMember { get; set; }

    /// <summary>
    /// The delegate that represents the resolver.
    /// </summary>
    public FieldResolverDelegate? Resolver { get; set; }

    /// <summary>
    /// The delegate that represents an optional pure resolver.
    /// </summary>
    public PureFieldDelegate? PureResolver { get; set; }

    /// <summary>
    /// Gets or sets all resolvers at once.
    /// </summary>
    public FieldResolverDelegates Resolvers
    {
        get => GetResolvers();
        set
        {
            Resolver = value.Resolver;
            PureResolver = value.PureResolver;
        }
    }

    /// <summary>
    /// Gets or sets the result post processor that shall be applied to the resolver result.
    /// </summary>
    public IResolverResultPostProcessor? ResultPostProcessor { get; set; }

    /// <summary>
    /// A list of middleware components which will be used to form the field pipeline.
    /// </summary>
    public IList<FieldMiddlewareConfiguration> MiddlewareDefinitions
    {
        get
        {
            _middlewareDefinitionsCleaned = false;
            return _middlewareDefinitions ??= [];
        }
    }

    /// <summary>
    /// A list of formatters that can transform the resolver result.
    /// </summary>
    public IList<ResultFormatterConfiguration> FormatterDefinitions
    {
        get
        {
            _resultConvertersCleaned = false;
            return _resultConverters ??= [];
        }
    }

    /// <summary>
    /// A list of parameter expression builders that shall be applied when compiling
    /// the resolver or when arguments are inferred from a method.
    /// </summary>
    public IList<IParameterExpressionBuilder> ParameterExpressionBuilders
    {
        get
        {
            return _expressionBuilders ??= [];
        }
    }

    /// <summary>
    /// Defines if this field can be executed in parallel with other fields.
    /// </summary>
    public bool IsParallelExecutable
    {
        get => (Flags & CoreFieldFlags.ParallelExecutable) == CoreFieldFlags.ParallelExecutable;
        set
        {
            if (value)
            {
                Flags |= CoreFieldFlags.ParallelExecutable;
            }
            else
            {
                Flags &= ~CoreFieldFlags.ParallelExecutable;
            }
        }
    }

    /// <summary>
    /// Defines in which DI scope this field is executed.
    /// </summary>
    public DependencyInjectionScope? DependencyInjectionScope { get; set; }

    /// <summary>
    /// Defines that the resolver pipeline returns an
    /// <see cref="IAsyncEnumerable{T}"/> as its result.
    /// </summary>
    public bool HasStreamResult
    {
        get => (Flags & CoreFieldFlags.Stream) == CoreFieldFlags.Stream;
        set
        {
            if (value)
            {
                Flags |= CoreFieldFlags.Stream;
            }
            else
            {
                Flags &= ~CoreFieldFlags.Stream;
            }
        }
    }

    /// <summary>
    /// A list of middleware components which will be used to form the field pipeline.
    /// </summary>
    internal IReadOnlyList<FieldMiddlewareConfiguration> GetMiddlewareDefinitions()
    {
        if (_middlewareDefinitions is null)
        {
            return [];
        }

        CleanRepeatableConfigurations(_middlewareDefinitions, ref _middlewareDefinitionsCleaned);

        return _middlewareDefinitions;
    }

    /// <summary>
    /// A list of converters that can transform the resolver result.
    /// </summary>
    internal IReadOnlyList<ResultFormatterConfiguration> GetResultConverters()
    {
        if (_resultConverters is null)
        {
            return [];
        }

        CleanRepeatableConfigurations(_resultConverters, ref _resultConvertersCleaned);

        return _resultConverters;
    }

    /// <summary>
    /// A list of parameter expression builders that shall be applied when compiling
    /// the resolver or when arguments are inferred from a method.
    /// </summary>
    internal IReadOnlyList<IParameterExpressionBuilder> GetParameterExpressionBuilders()
    {
        if (_expressionBuilders is null)
        {
            return [];
        }

        return _expressionBuilders;
    }

    private FieldResolverDelegates GetResolvers()
        => new(Resolver, PureResolver);

    internal void CopyTo(InterfaceFieldConfiguration target)
    {
        base.CopyTo(target);

        if (_middlewareDefinitions is { Count: > 0 })
        {
            target._middlewareDefinitions = [.. _middlewareDefinitions];
            _middlewareDefinitionsCleaned = false;
        }

        if (_resultConverters is { Count: > 0 })
        {
            target._resultConverters = [.. _resultConverters];
            _resultConvertersCleaned = false;
        }

        if (_expressionBuilders is { Count: > 0 })
        {
            target._expressionBuilders = [.. _expressionBuilders];
        }

        target.SourceType = SourceType;
        target.ResolverType = ResolverType;
        target.Member = Member;
        target.BindToField = BindToField;
        target.ResolverMember = ResolverMember;
        target.ResultType = ResultType;
        target.Resolver = Resolver;
        target.PureResolver = PureResolver;
        target.IsParallelExecutable = IsParallelExecutable;
        target.DependencyInjectionScope = DependencyInjectionScope;
        target.HasStreamResult = HasStreamResult;
        target.ResultPostProcessor = ResultPostProcessor;
    }

    internal void CopyTo(ObjectFieldConfiguration target)
    {
        base.CopyTo(target);

        if (_middlewareDefinitions is { Count: > 0 })
        {
            foreach (var definition in _middlewareDefinitions)
            {
                target.MiddlewareConfigurations.Add(definition);
            }
            _middlewareDefinitionsCleaned = false;
        }

        if (_resultConverters is { Count: > 0 })
        {
            foreach (var definition in _resultConverters)
            {
                target.FormatterConfigurations.Add(definition);
            }
            _resultConvertersCleaned = false;
        }

        if (_expressionBuilders is { Count: > 0 })
        {
            foreach (var builder in _expressionBuilders)
            {
                target.ParameterExpressionBuilders.Add(builder);
            }
        }

        target.SourceType = SourceType;
        target.ResolverType = ResolverType;
        target.Member = Member;
        target.BindToField = BindToField;
        target.ResolverMember = ResolverMember;
        target.Resolver = Resolver;
        target.PureResolver = PureResolver;
        target.IsParallelExecutable = IsParallelExecutable;
        target.DependencyInjectionScope = DependencyInjectionScope;
        target.HasStreamResult = HasStreamResult;
        target.ResultPostProcessor = ResultPostProcessor;
    }

    internal void MergeInto(InterfaceFieldConfiguration target)
    {
        base.MergeInto(target);

        if (_middlewareDefinitions is { Count: > 0 })
        {
            target._middlewareDefinitions ??= [];
            target._middlewareDefinitions.AddRange(_middlewareDefinitions);
            _middlewareDefinitionsCleaned = false;
        }

        if (_resultConverters is { Count: > 0 })
        {
            target._resultConverters ??= [];
            target._resultConverters.AddRange(_resultConverters);
            _resultConvertersCleaned = false;
        }

        if (_expressionBuilders is { Count: > 0 })
        {
            target._expressionBuilders ??= [];
            target._expressionBuilders.AddRange(_expressionBuilders);
        }

        if (!IsParallelExecutable)
        {
            target.IsParallelExecutable = false;
        }

        if (DependencyInjectionScope.HasValue)
        {
            target.DependencyInjectionScope = DependencyInjectionScope;
        }

        if (!HasStreamResult)
        {
            target.HasStreamResult = false;
        }

        if (ResolverType is not null)
        {
            target.ResolverType = ResolverType;
        }

        if (Member is not null)
        {
            target.Member = Member;
        }

        if (ResolverMember is not null)
        {
            target.ResolverMember = ResolverMember;
        }

        if (Resolver is not null)
        {
            target.Resolver = Resolver;
        }

        if (PureResolver is not null)
        {
            target.PureResolver = PureResolver;
        }

        if (ResultPostProcessor is not null)
        {
            target.ResultPostProcessor = ResultPostProcessor;
        }
    }

    private static void CleanRepeatableConfigurations<T>(
        List<T> definitions,
        ref bool isClean)
        where T : IRepeatableConfiguration
    {
        var count = definitions.Count;

        if (!isClean && count > 1)
        {
            if (count == 2 && definitions[0].IsRepeatable)
            {
                isClean = true;
            }

            if (count == 3 &&
                definitions[0].IsRepeatable &&
                definitions[1].IsRepeatable &&
                definitions[2].IsRepeatable)
            {
                isClean = true;
            }

            if (count == 4 &&
                definitions[0].IsRepeatable &&
                definitions[1].IsRepeatable &&
                definitions[2].IsRepeatable &&
                definitions[3].IsRepeatable)
            {
                isClean = true;
            }

            if (!isClean)
            {
                var nonRepeatable = 0;

                foreach (var def in definitions)
                {
                    if (def is { IsRepeatable: false, Key: not null })
                    {
                        nonRepeatable++;
                    }
                }

                if (nonRepeatable > 1)
                {
                    var keys = ArrayPool<string>.Shared.Rent(nonRepeatable);

                    // we clear the section of the array we need before we are using it.
                    keys.AsSpan()[..nonRepeatable].Clear();
                    int i = 0, ki = 0;

                    do
                    {
                        var def = definitions[i];

                        if (def.IsRepeatable || def.Key is null)
                        {
                            i++;
                        }
                        else
                        {
                            if (ki > 0 && Array.IndexOf(keys, def.Key, 0, ki) != -1)
                            {
                                count--;
                                definitions.RemoveAt(i);
                                continue;
                            }

                            keys[ki++] = def.Key;
                            i++;
                        }
                    } while (i < count);

                    ArrayPool<string>.Shared.Return(keys);
                }

                isClean = true;
            }
        }
    }
}
