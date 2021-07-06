using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal partial class MiddlewareContext
    {
        private sealed class PureResolverContext : IPureResolverContext
        {
            private readonly MiddlewareContext _parentContext;
            private IReadOnlyDictionary<NameString, ArgumentValue> _argumentValues = default!;
            private ISelection _selection = default!;
            private Path _path = default!;
            private object? _parent;



            public PureResolverContext(MiddlewareContext parentContext)
            {
                _parentContext = parentContext;
            }

            public bool Initialize(ISelection selection, Path path, object? parent)
            {
                _selection = selection;
                _path = path;
                _parent = parent;
                _argumentValues = selection.Arguments;

                if (selection.Arguments.IsFinalNoErrors)
                {
                    return true;
                }

                if (selection.Arguments.TryCoerceArguments(
                    _parentContext,
                    out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
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
                _parent = null;
                _argumentValues = default!;
            }

            public ISchema Schema => _parentContext.Schema;

            public IObjectType RootType => _parentContext.RootType;

            public IFieldSelection Selection => _parentContext.Selection;

            public IVariableValueCollection Variables => _parentContext.Variables;

            public IDictionary<string, object?> ContextData
                => _parentContext.ContextData;

            public T Parent<T>()
            {
                if (_parent is T casted)
                {
                    return casted;
                }

                return default!;
            }

            public T ArgumentValue<T>(NameString name)
            {
                if (!_argumentValues.TryGetValue(name, out ArgumentValue? argument))
                {
                    throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
                }

                return CoerceArgumentValue<T>(argument);
            }

            public TValueNode ArgumentLiteral<TValueNode>(NameString name)
                where TValueNode : IValueNode
            {
                if (!_argumentValues.TryGetValue(name, out ArgumentValue? argument))
                {
                    throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
                }

                IValueNode literal = argument.ValueLiteral!;

                if (literal is TValueNode castedLiteral)
                {
                    return castedLiteral;
                }

                throw ResolverContext_LiteralNotCompatible(
                    _selection.SyntaxNode, _path, name,  typeof(TValueNode), literal.GetType());
            }

            public Optional<T> ArgumentOptional<T>(NameString name)
            {
                if (!_argumentValues.TryGetValue(name, out ArgumentValue? argument))
                {
                    throw ResolverContext_ArgumentDoesNotExist(_selection.SyntaxNode, _path, name);
                }

                return argument.IsImplicit
                    ? Optional<T>.Empty(CoerceArgumentValue<T>(argument))
                    : new Optional<T>(CoerceArgumentValue<T>(argument));
            }

            public ValueKind ArgumentKind(NameString name)
            {
                if (!_argumentValues.TryGetValue(name, out ArgumentValue? argument))
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
                if (!argument.IsFinal)
                {
                    value = argument.Type.ParseLiteral(argument.ValueLiteral!);
                    value = argument.Formatter is not null
                        ? argument.Formatter.OnAfterDeserialize(value)
                        : value;
                }

                if (value is null)
                {
                    // if there was a non-null violation the exception would already been triggered.
                    return default!;
                }

                ITypeConverter converter = _parentContext.GetTypeConverter();

                if (value is T castedValue ||
                    converter.TryConvert(value, out castedValue))
                {
                    return castedValue;
                }

                // GraphQL literals are not allowed.
                if (typeof(IValueNode).IsAssignableFrom(typeof(T)))
                {
                    throw ResolverContext_LiteralsNotSupported(
                        _selection.SyntaxNode, _path, argument.Argument.Name, typeof(T));
                }

                // If the object is internally held as a dictionary structure we will try to
                // create from this the required argument value.
                // This however comes with a performance impact of traversing the dictionary structure
                // and creating from this the object.
                if (value is IReadOnlyDictionary<string, object> || value is IReadOnlyList<object>)
                {
                    var dictToObjConverter = new DictionaryToObjectConverter(converter);

                    if (typeof(T).IsInterface)
                    {
                        object o = dictToObjConverter.Convert(value, argument.Type.RuntimeType);
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
                    _selection.SyntaxNode, _path, argument.Argument.Name, typeof(T));
            }
        }
    }
}
