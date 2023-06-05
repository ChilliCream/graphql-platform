using System;
using System.Collections.Generic;
using HotChocolate.Execution.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private sealed class PureResolverContext : IPureResolverContext
    {
        private readonly MiddlewareContext _parentContext;
        private ITypeConverter? _typeConverter;
        private IReadOnlyDictionary<string, ArgumentValue> _argumentValues = default!;
        private ISelection _selection = default!;
        private Path _path = default!;
        private ObjectType _parentType = default!;
        private object? _parent;

        public PureResolverContext(MiddlewareContext parentContext)
        {
            _parentContext = parentContext;
        }

        public bool Initialize(
            ISelection selection,
            Path path,
            ObjectType parentType,
            object? parent)
        {
            _selection = selection;
            _path = path;
            _parentType = parentType;
            _parent = parent;
            _argumentValues = selection.Arguments;

            if (selection.Arguments.IsFullyCoercedNoErrors)
            {
                return true;
            }

            if (selection.Arguments.TryCoerceArguments(
                    _parentContext,
                    out var coercedArgs))
            {
                _argumentValues = coercedArgs;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _selection = default!;
            _path = default!;
            _parentType = default!;
            _parent = null;
            _argumentValues = default!;
        }

        public ISchema Schema => _parentContext.Schema;

        public IObjectType ObjectType => _parentType;

        public IOperation Operation => _parentContext.Operation;

        public ISelection Selection => _selection;

        public Path Path => _path;

        public IReadOnlyDictionary<string, object?> ScopedContextData
            => _parentContext.ScopedContextData;

        public IVariableValueCollection Variables => _parentContext.Variables;

        public IDictionary<string, object?> ContextData
            => _parentContext.ContextData;

        public T Parent<T>()
            => _parent switch
            {
                T casted => casted,
                null => default!,
                object[] o when Temp.ValueConverter.TryGetValue(_selection.Id, out var converter) =>
                    (T)converter(o),
                _ => throw ResolverContext_CannotCastParent(
                    Selection.Field.Coordinate,
                    _path,
                    typeof(T),
                    _parent.GetType())
            };

        public T ArgumentValue<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
            }

            return CoerceArgumentValue<T>(argument);
        }

        public TValueNode ArgumentLiteral<TValueNode>(string name)
            where TValueNode : IValueNode
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
            }

            var literal = argument.ValueLiteral!;

            if (literal is TValueNode castedLiteral)
            {
                return castedLiteral;
            }

            throw ResolverContext_LiteralNotCompatible(
                _selection.SyntaxNode,
                _path,
                name,
                typeof(TValueNode),
                literal.GetType());
        }

        public Optional<T> ArgumentOptional<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
            }

            return argument.IsDefaultValue
                ? Optional<T>.Empty(CoerceArgumentValue<T>(argument))
                : new Optional<T>(CoerceArgumentValue<T>(argument));
        }

        public ValueKind ArgumentKind(string name)
        {
            if (!_argumentValues.TryGetValue(name, out var argument))
            {
                throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
            }

            // There can only be no kind if there was an error which would have
            // already been raised at this point.
            return argument.Kind ?? ValueKind.Unknown;
        }

        public T Service<T>() => _parentContext.Service<T>();

        public T Resolver<T>() => _parentContext.Resolver<T>();

        private T CoerceArgumentValue<T>(ArgumentValue argument)
        {
            var value = argument.Value;

            // if the argument is final and has an already coerced
            // runtime version we can skip over parsing it.
            if (!argument.IsFullyCoerced)
            {
                value = _parentContext._parser.ParseLiteral(
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
                _parentContext.Services.GetService<ITypeConverter>() ??
                    DefaultTypeConverter.Default;

            if (value is T castedValue ||
                _typeConverter.TryConvert(value, out castedValue))
            {
                return castedValue;
            }

            // GraphQL literals are not allowed.
            if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
            {
                throw ResolverContext_LiteralsNotSupported(
                    _selection.SyntaxNode,
                    _path,
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
                    var o = dictToObjConverter.Convert(value, argument.Type.RuntimeType);
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
                _selection.SyntaxNode,
                _path,
                argument.Name,
                typeof(T));
        }
    }
}
