using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Utilities
{
    public partial class DefaultTypeConverter
        : ITypeConverter
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, ChangeType>> _converters =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Type, ChangeType>>();
        private readonly List<IChangeTypeProvider> _changeTypeProvider =
            new List<IChangeTypeProvider>();

        public DefaultTypeConverter(IEnumerable<IChangeTypeProvider>? providers = null)
        {
            if (providers is { })
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
            if (!TryConvert(from, to, source, out object? converted))
            {
                throw new NotSupportedException(
                    string.Format(
                        TypeResources.TypeConvertion_ConvertNotSupported,
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

            if (to.IsAssignableFrom(from))
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
                Type fromInternal = from == typeof(object)
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

            if (TryGetOrCreateConverter(from, to, out ChangeType? converter))
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
            if (TryGetConverter(from, to, out converter)
                || TryCreateConverterFromFactory(from, to, out converter))
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
                foreach (IChangeTypeProvider provider in _changeTypeProvider)
                {
                    if (provider.TryCreateConverter(
                        source, target, TryGetOrCreateConverter, out converter))
                    {
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
            if (target.IsAssignableFrom(source) || target == typeof(object))
            {
                converter = source => source;
                return true;
            }

            if (_converters.TryGetValue(source,
                out ConcurrentDictionary<Type, ChangeType>? toLookUp)
                && toLookUp.TryGetValue(target,
                out ChangeType? changeType))
            {
                converter = changeType;
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

        public static DefaultTypeConverter Default { get; } = new DefaultTypeConverter();

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
}
