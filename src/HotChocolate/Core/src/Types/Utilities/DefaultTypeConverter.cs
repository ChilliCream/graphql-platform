using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Utilities;

public partial class DefaultTypeConverter : ITypeConverter
{
    private readonly ConcurrentDictionary<(Type, Type), ChangeType> _converters = new();
    private readonly List<IChangeTypeProvider> _changeTypeProvider = [];

    public DefaultTypeConverter(IEnumerable<IChangeTypeProvider>? providers = null)
    {
        if (providers is not null)
        {
            _changeTypeProvider.AddRange(providers);
        }

        RegisterConverters(this);

        _changeTypeProvider.Add(new EnumTypeConverter());
        _changeTypeProvider.Add(new ListTypeConverter());
        _changeTypeProvider.Add(new NullableTypeConverter());
    }

    public object? Convert(Type from, Type to, object? source)
    {
        if (!TryConvert(from, to, source, out var converted))
        {
            throw new NotSupportedException(
                string.Format(
                    TypeResources.TypeConversion_ConvertNotSupported,
                    from.Name,
                    to.Name));
        }
        return converted;
    }

    public bool TryConvert(Type from, Type to, object? source, out object? converted)
    {
        if (from is null)
        {
            throw new ArgumentNullException(nameof(from));
        }

        if (to is null)
        {
            throw new ArgumentNullException(nameof(to));
        }

        if (from == to)
        {
            converted = source;
            return true;
        }

        if (source is null)
        {
            converted = to.IsValueType ? Activator.CreateInstance(to) : null;
            return true;
        }

        try
        {
            var fromInternal = from == typeof(object)
                ? source.GetType()
                : from;

            return TryConvertInternal(
                fromInternal, to, source,
                out converted);
        }
        catch
        {
            converted = null;
            return false;
        }
    }

    private bool TryConvertInternal(Type from, Type to, object source, out object? converted)
    {
        if (from == to)
        {
            converted = source;
            return true;
        }

        if (TryGetOrCreateConverter(from, to, out var converter))
        {
            converted = converter(source);
            return true;
        }

        converted = null;
        return false;
    }

    private bool TryGetOrCreateConverter(
        Type from,
        Type to,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        if (TryGetConverter(from, to, out converter))
        {
            return true;
        }

        if (TryCreateConverterFromFactory(from, to, out converter))
        {
            return true;
        }

        return false;
    }

    private bool TryCreateConverterFromFactory(
        Type source,
        Type target,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        if (_changeTypeProvider.Count > 0)
        {
            foreach (var provider in _changeTypeProvider)
            {
                if (provider.TryCreateConverter(
                    source, target, TryGetOrCreateConverter, out converter))
                {
                    _converters.TryAdd((source, target), converter);
                    return true;
                }
            }
        }

        converter = null;
        return false;
    }

    private bool TryGetConverter(
        Type source,
        Type target,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        if (target == typeof(object) || target.IsAssignableFrom(source))
        {
            converter = s => s;
            return true;
        }

        if (_converters.TryGetValue((source, target), out converter))
        {
            return true;
        }

        converter = null;
        return false;
    }

    private void Register<TSource, TTarget>(ChangeType<TSource, TTarget> converter) =>
        _changeTypeProvider.Add(new DelegateTypeConverter(
            typeof(TSource),
            typeof(TTarget),
            input =>
            {
                if (input is null)
                {
                    return default(TTarget);
                }
                return converter((TSource)input);
            }));

    public static DefaultTypeConverter Default { get; } = new();

    private sealed class DelegateTypeConverter : IChangeTypeProvider
    {
        private readonly Type _source;
        private readonly Type _target;
        private readonly ChangeType _converter;

        public DelegateTypeConverter(Type source, Type target, ChangeType converter)
        {
            _source = source;
            _target = target;
            _converter = converter;
        }

        public bool TryCreateConverter(
            Type source,
            Type target,
            ChangeTypeProvider root,
            [NotNullWhen(true)] out ChangeType? converter)
        {
            if (_source == source && _target == target)
            {
                converter = _converter;
                return true;
            }

            converter = null;
            return false;
        }
    }
}
