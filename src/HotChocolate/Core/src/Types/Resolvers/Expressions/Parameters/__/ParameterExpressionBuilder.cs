using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    /// <summary>
    /// Builds parameter expressions injecting the parent object.
    /// Parameters representing the parent object must be annotated with
    /// <see cref="ParentAttribute"/>.
    /// </summary>
    internal sealed class ParentParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private const string _parent = nameof(IPureResolverContext.Parent);
        private static readonly MethodInfo _getParentMethod;

        static ParentParameterExpressionBuilder()
        {
            _getParentMethod = PureContextType.GetMethods().First(IsParentMethod);
            Debug.Assert(_getParentMethod is not null!, "Parent method is missing." );

            static bool IsParentMethod(MethodInfo method)
                => method.Name.Equals(_parent, StringComparison.Ordinal) &&
                   method.IsGenericMethod;
        }

        public ArgumentKind Kind => ArgumentKind.Source;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => parameter.IsDefined(typeof(ParentAttribute));

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
        {
            Type parameterType = parameter.ParameterType;
            MethodInfo argumentMethod = _getParentMethod.MakeGenericMethod(parameterType);
            return Expression.Call(context, argumentMethod);
        }
    }

    internal sealed class DocumentParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _document;

        static DocumentParameterExpressionBuilder()
        {
            _document = ContextType.GetProperty(nameof(IResolverContext.Document))!;
            Debug.Assert(_document is not null!, "Document property is missing." );
        }

        public ArgumentKind Kind => ArgumentKind.DocumentSyntax;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(DocumentNode) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => Expression.Property(context, _document);
    }

    internal sealed class CancellationTokenParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _cancellationToken;

        static CancellationTokenParameterExpressionBuilder()
        {
            _cancellationToken = ContextType.GetProperty(nameof(IResolverContext.RequestAborted))!;
            Debug.Assert(_cancellationToken is not null!, "RequestAborted property is missing." );
        }

        public ArgumentKind Kind => ArgumentKind.DocumentSyntax;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(CancellationToken) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => Expression.Property(context, _cancellationToken);
    }

    internal class ArgumentParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private const string _argumentValue = nameof(IPureResolverContext.ArgumentValue);
        private const string _argumentLiteral = nameof(IPureResolverContext.ArgumentLiteral);
        private const string _argumentOptional = nameof(IPureResolverContext.ArgumentOptional);
        private static readonly Type _optional = typeof(Optional<>);
        private static readonly MethodInfo _getArgumentValue;
        private static readonly MethodInfo _getArgumentLiteral;
        private static readonly MethodInfo _getArgumentOptional;

        static ArgumentParameterExpressionBuilder()
        {
            _getArgumentValue = PureContextType.GetMethods().First(IsArgumentValueMethod);
            Debug.Assert(_getArgumentValue is not null!, "ArgumentValue method is missing." );

            _getArgumentLiteral = PureContextType.GetMethods().First(IsArgumentLiteralMethod);
            Debug.Assert(_getArgumentValue is not null!, "ArgumentLiteral method is missing." );

            _getArgumentOptional = PureContextType.GetMethods().First(IsArgumentOptionalMethod);
            Debug.Assert(_getArgumentValue is not null!, "ArgumentOptional method is missing." );

            static bool IsArgumentValueMethod(MethodInfo method)
                => method.Name.Equals(_argumentValue, StringComparison.Ordinal) &&
                   method.IsGenericMethod;

            static bool IsArgumentLiteralMethod(MethodInfo method)
                => method.Name.Equals(_argumentLiteral, StringComparison.Ordinal) &&
                   method.IsGenericMethod;

            static bool IsArgumentOptionalMethod(MethodInfo method)
                => method.Name.Equals(_argumentOptional, StringComparison.Ordinal) &&
                   method.IsGenericMethod;
        }

        public ArgumentKind Kind => ArgumentKind.Argument;

        public bool IsPure => true;

        public virtual bool CanHandle(ParameterInfo parameter, Type source)
            => parameter.IsDefined(typeof(ArgumentAttribute));

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
        {
            string name = parameter.IsDefined(typeof(ArgumentAttribute))
                ? parameter.GetCustomAttribute<ArgumentAttribute>()!.Name ?? parameter.Name!
                : parameter.Name!;

            if (parameter.IsDefined(typeof(GraphQLNameAttribute)))
            {
                name = parameter.GetCustomAttribute<GraphQLNameAttribute>()!.Name;
            }

            MethodInfo argumentMethod;

            if (parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == _optional)
            {
                argumentMethod = _getArgumentOptional.MakeGenericMethod(
                    parameter.ParameterType.GenericTypeArguments[0]);
            }
            else if (typeof(IValueNode).IsAssignableFrom(parameter.ParameterType))
            {
                argumentMethod = _getArgumentLiteral.MakeGenericMethod(
                    parameter.ParameterType);
            }
            else
            {
                argumentMethod = _getArgumentValue.MakeGenericMethod(
                    parameter.ParameterType);
            }

            return Expression.Call(context, argumentMethod,
                Expression.Constant(new NameString(name)));
        }
    }

    internal sealed class ResolverContextParameterExpressionBuilder : IParameterExpressionBuilder
    {
        public ArgumentKind Kind => ArgumentKind.Context;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(IResolverContext) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => context;
    }

    internal sealed class PureResolverContextParameterExpressionBuilder : IParameterExpressionBuilder
    {
        public ArgumentKind Kind => ArgumentKind.Context;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(IPureResolverContext) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => context;
    }

    internal sealed class SchemaParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _schema;

        static SchemaParameterExpressionBuilder()
        {
            _schema = PureContextType.GetProperty(nameof(IPureResolverContext.Schema))!;
            Debug.Assert(_schema is not null!, "Schema property is missing." );
        }

        public ArgumentKind Kind => ArgumentKind.Schema;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(ISchema) == parameter.ParameterType ||
               typeof(Schema) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => Expression.Convert(Expression.Property(context, _schema), parameter.ParameterType);
    }

    internal sealed class GlobalStateParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _contextData =
            typeof(IHasContextData).GetProperty(
                nameof(IHasContextData.ContextData))!;
        private static readonly MethodInfo _getGlobalState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetGlobalState))!;
        private static readonly MethodInfo _getGlobalStateWithDefault =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetGlobalStateWithDefault))!;
        private static readonly MethodInfo _setGlobalState =
            typeof(ExpressionHelper)
                .GetMethod(nameof(ExpressionHelper.SetGlobalState))!;
        private static readonly MethodInfo _setGlobalStateGeneric =
            typeof(ExpressionHelper)
                .GetMethod(nameof(ExpressionHelper.SetGlobalStateGeneric))!;

        public ArgumentKind Kind => ArgumentKind.GlobalState;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => parameter.IsDefined(typeof(GlobalStateAttribute));

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
        {
            GlobalStateAttribute attribute = parameter.GetCustomAttribute<GlobalStateAttribute>()!;

            ConstantExpression key =
                attribute.Key is null
                    ? Expression.Constant(parameter.Name, typeof(string))
                    : Expression.Constant(attribute.Key, typeof(string));

            MemberExpression contextData = Expression.Property(context, _contextData);

            if (IsStateSetter(parameter.ParameterType))
            {
                return BuildSetter(parameter, key, contextData);
            }

            return BuildGetter(parameter, key, contextData);
        }

        private Expression BuildSetter(
            ParameterInfo parameter,
            ConstantExpression key,
            MemberExpression contextData)
        {
            MethodInfo setGlobalState =
                parameter.ParameterType.IsGenericType
                    ? _setGlobalStateGeneric.MakeGenericMethod(
                        parameter.ParameterType.GetGenericArguments()[0])
                    : _setGlobalState;

            return Expression.Call(
                setGlobalState,
                contextData,
                key);
        }

        private Expression BuildGetter(
            ParameterInfo parameter,
            ConstantExpression key,
            MemberExpression contextData)
        {
            MethodInfo getGlobalState =
                parameter.HasDefaultValue
                    ? _getGlobalStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                    : _getGlobalState.MakeGenericMethod(parameter.ParameterType);

            return parameter.HasDefaultValue
                ? Expression.Call(
                    getGlobalState,
                    contextData,
                    key,
                    Expression.Constant(true, typeof(bool)),
                    Expression.Constant(parameter.RawDefaultValue, parameter.ParameterType))
                : Expression.Call(
                    getGlobalState,
                    contextData,
                    key,
                    Expression.Constant(
                        new NullableHelper(parameter.ParameterType)
                            .GetFlags(parameter).FirstOrDefault() ?? false,
                        typeof(bool)));
        }
    }
}
