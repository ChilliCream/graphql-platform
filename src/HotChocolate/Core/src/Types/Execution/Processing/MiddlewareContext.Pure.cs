using System.Collections.Immutable;
using HotChocolate.Execution.Internal;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private sealed class PureResolverContext(MiddlewareContext parentContext) : IResolverContext
    {
        private ITypeConverter? _typeConverter;
        private IReadOnlyDictionary<string, ArgumentValue> _argumentValues = null!;
        private Selection _selection = null!;
        private ObjectType _selectionSetType = null!;
        private ResultElement _resultValue;
        private object? _parent;

        public bool Initialize(
            Selection selection,
            ObjectType selectionSetType,
            ResultElement resultValue,
            object? parent)
        {
            _selection = selection;
            _selectionSetType = selectionSetType;
            _resultValue = resultValue;
            _parent = parent;
            _argumentValues = selection.Arguments;

            if (selection.Arguments.IsFullyCoercedNoErrors)
            {
                return true;
            }

            if (selection.Arguments.TryCoerceArguments(parentContext, out var coercedArgs))
            {
                _argumentValues = coercedArgs;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _selection = null!;
            _selectionSetType = null!;
            _resultValue = default;
            _parent = null;
            _argumentValues = null!;
        }

        public Schema Schema => parentContext.Schema;

        public ObjectType ObjectType => _selectionSetType;

        public Operation Operation => parentContext.Operation;

        public Selection Selection => _selection;

        public Path Path => _resultValue.Path;

        public ulong IncludeFlags => parentContext.IncludeFlags;

        public CancellationToken RequestAborted => parentContext.RequestAborted;

        public void ReportError(string errorMessage)
            => throw new NotSupportedException();

        public void ReportError(IError error)
            => throw new NotSupportedException();

        public void ReportError(Exception exception, Action<ErrorBuilder>? configure = null)
            => throw new NotSupportedException();

        public SelectionEnumerator GetSelections(
            ObjectType typeContext,
            Selection? selection = null,
            bool allowInternals = false)
            => throw new NotSupportedException();

        public ISelectionCollection Select()
            => throw new NotSupportedException();

        public ISelectionCollection Select(string fieldName)
            => throw new NotSupportedException();

        public T GetQueryRoot<T>()
            => throw new NotSupportedException();

        public IResolverContext Clone()
            => throw new NotSupportedException();

        public string ResponseName => _selection.ResponseName;

        public bool HasErrors
            => throw new NotSupportedException();

        public IImmutableDictionary<string, object?> ScopedContextData
        {
            get => parentContext.ScopedContextData;
            set => throw new NotSupportedException();
        }

        public IImmutableDictionary<string, object?> LocalContextData
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public IVariableValueCollection Variables => parentContext.Variables;

        public IDictionary<string, object?> ContextData
            => parentContext.ContextData;

        public T Parent<T>()
            => _parent switch
            {
                T casted => casted,
                null => default!,
                _ => throw ResolverContext_CannotCastParent(
                    Selection.Field.Coordinate,
                    Path,
                    typeof(T),
                    _parent.GetType())
            };

        public T ArgumentValue<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNodes[0].Node, Path, name);
            }

            return CoerceArgumentValue<T>(argument);
        }

        public TValueNode ArgumentLiteral<TValueNode>(string name)
            where TValueNode : IValueNode
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNodes[0].Node, Path, name);
            }

            var literal = argument.ValueLiteral!;

            if (literal is TValueNode castedLiteral)
            {
                return castedLiteral;
            }

            throw ResolverContext_LiteralNotCompatible(
                _selection.SyntaxNodes[0].Node,
                Path,
                name,
                typeof(TValueNode),
                literal.GetType());
        }

        public Optional<T> ArgumentOptional<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNodes[0].Node, Path, name);
            }

            return argument.IsDefaultValue
                ? Optional<T>.Empty(CoerceArgumentValue<T>(argument))
                : new Optional<T>(CoerceArgumentValue<T>(argument));
        }

        public ValueKind ArgumentKind(string name)
        {
            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNodes[0].Node, Path, name);
            }

            // There can only be no kind of there was an error which would have
            // already been raised at this point.
            return argument.Kind ?? ValueKind.Unknown;
        }

        public IServiceProvider Services
        {
            get => parentContext.Services;
            set => throw new NotSupportedException();
        }

        public IServiceProvider RequestServices
            => parentContext.RequestServices;

        public IFeatureCollection Features
            => parentContext.Features;

        public object Service(Type service)
            => parentContext.Service(service);

        public T Service<T>() where T : notnull => parentContext.Service<T>();

        public T Service<T>(object key) where T : notnull => parentContext.Service<T>(key);

        public T Resolver<T>() => parentContext.Resolver<T>();

        private T CoerceArgumentValue<T>(ArgumentValue argument)
        {
            var value = argument.Value;

            // if the argument is final and has an already coerced
            // runtime version we can skip over parsing it.
            if (!argument.IsFullyCoerced)
            {
                value = parentContext._parser.ParseLiteral(
                    argument.ValueLiteral!,
                    argument,
                    typeof(T));
            }

            if (value is null)
            {
                // if there was a non-null violation the exception would already been triggered.
                return default!;
            }

            _typeConverter ??=
                parentContext.Services.GetService<ITypeConverter>() ??
                DefaultTypeConverter.Default;

            if (value is T castedValue
                || _typeConverter.TryConvert(value, out castedValue, out var conversionException))
            {
                return castedValue;
            }

            // GraphQL literals are not allowed.
            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                throw ResolverContext_LiteralsNotSupported(
                    _selection.SyntaxNodes[0].Node,
                    Path,
                    argument.Name,
                    typeof(T));
            }

            // If the object is internally held as a dictionary structure we will try to
            // create from this the required argument value.
            // This however comes with a performance impact of traversing the dictionary structure
            // and creating from this the object.
            if (value is IReadOnlyDictionary<string, object> || value is IReadOnlyList<object>)
            {
                var dictToObjConverter = new DictionaryToObjectConverter(_typeConverter);

                if (typeof(T).IsInterface)
                {
                    var o = dictToObjConverter.Convert(value, argument.Type.ToRuntimeType());
                    if (o is T c)
                    {
                        return c;
                    }
                }
                else
                {
                    return (T)dictToObjConverter.Convert(value, typeof(T));
                }
            }

            // we are unable to convert the argument to the request type.
            throw ResolverContext_CannotConvertArgument(
                _selection.SyntaxNodes[0].Node,
                Path,
                argument.Name,
                typeof(T),
                conversionException);
        }
    }
}
