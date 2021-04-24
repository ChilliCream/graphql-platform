using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using static System.Linq.Expressions.Expression;

namespace HotChocolate.Execution.Pipeline.Complexity
{
    internal sealed class MaxComplexityVisitor : TypeDocumentValidatorVisitor
    {
        private readonly Expression _options;
        private readonly Expression _zero = Constant(0, typeof(int));
        private readonly ParameterExpression _variables =
            Parameter(typeof(IVariableValueCollection), "variables");

        public MaxComplexityVisitor(IMaxComplexityOptionsAccessor options)
        {
            _options = Constant(options, typeof(IMaxComplexityOptionsAccessor));
        }

        protected override ISyntaxVisitorAction Enter(
            DocumentNode node,
            IDocumentValidatorContext context)
        {
            context.List.Push(new List<OperationComplexityAnalyzer>());
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.List.Push(new List<Expression>());
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            var expressions = (List<Expression>)context.List.Pop();
            var analyzers = (List<OperationComplexityAnalyzer>)context.List.Peek();

            analyzers.Add(new OperationComplexityAnalyzer(
                node,
                Lambda<AnalyzeComplexity>(Combine(expressions), _variables).Compile()));

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ot &&
                ot.Fields.TryGetField(node.Name.Value, out IOutputField? of))
            {
                context.List.Push(new List<Expression>());
                context.OutputFields.Push(of);
                context.Types.Push(of.Type);
                return Continue;
            }

            context.UnexpectedErrorsDetected = true;
            return Skip;
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            IOutputField field = context.OutputFields.Pop();
            var children = (List<MethodCallExpression>)context.List.Pop();
            context.Types.Pop();

            context.List.Push(CreateFieldComplexityExpression(context, field, node, children));

            return Continue;
        }

        private Expression CreateFieldComplexityExpression(
            IDocumentValidatorContext context,
            IOutputField field,
            FieldNode selection,
            List<MethodCallExpression> children)
        {
            return children.Count switch
            {
                0 => CreateCalculateExpression(context, field, selection, _zero),
                1 => CreateCalculateExpression(context, field, selection, children[0]),
                _ => CreateCalculateExpression(context, field, selection, Combine(children))
            };
        }

        private Expression CreateCalculateExpression(
            IDocumentValidatorContext context,
            IOutputField field,
            FieldNode selection,
            Expression childComplexity)
        {
            return Call(
                Helper.CalculateMethod,
                Constant(field, typeof(IOutputField)),
                Constant(selection, typeof(FieldNode)),
                Constant(context.Fields.Count + 1, typeof(int)),
                Constant(context.Path.Count + 1, typeof(int)),
                childComplexity,
                _variables,
                _options);
        }

        private Expression Combine(IReadOnlyList<Expression> expressions)
        {
            Expression combinedComplexity = expressions[0];

            for (var i = 1; i < expressions.Count; i++)
            {
                combinedComplexity = Add(combinedComplexity, expressions[i]);
            }

            return combinedComplexity;
        }

        private static class Helper
        {
            public static readonly MethodInfo CalculateMethod =
                typeof(Helper).GetMethod(nameof(Calculate))!;

            public static int Calculate(
                IOutputField field,
                FieldNode selection,
                int fieldDepth,
                int nodeDepth,
                int childComplexity,
                IVariableValueCollection variables,
                IMaxComplexityOptionsAccessor options)
            {
                return options.ComplexityCalculation(
                    new ComplexityContext(
                        field,
                        selection,
                        field.Directives["cost"].FirstOrDefault()?.ToObject<CostDirective>(),
                        fieldDepth,
                        nodeDepth,
                        childComplexity,
                        variables,
                        options));
            }
        }
    }

    public class OperationComplexityAnalyzer
    {
        private readonly AnalyzeComplexity _analyze;

        public OperationComplexityAnalyzer(
            OperationDefinitionNode operationDefinitionNode,
            AnalyzeComplexity analyze)
        {
            _analyze = analyze;
            OperationDefinitionNode = operationDefinitionNode;
        }

        public OperationDefinitionNode OperationDefinitionNode { get;  }

        public int Analyze(IVariableValueCollection variableValues)
        {
            if (variableValues is null)
            {
                throw new ArgumentNullException(nameof(variableValues));
            }

            return _analyze(variableValues);
        }
    }

    public delegate int AnalyzeComplexity(IVariableValueCollection variableValues);


    public delegate int ComplexityCalculation(ComplexityContext context);

    public ref struct ComplexityContext
    {
        private readonly IVariableValueCollection _valueCollection;

        public ComplexityContext(
            IOutputField field,
            FieldNode selection,
            CostDirective? cost,
            int fieldDepth,
            int nodeDepth,
            int childComplexity,
            IVariableValueCollection valueCollection,
            IMaxComplexityOptionsAccessor options)
        {
            Field = field;
            Selection = selection;
            Complexity = cost?.Complexity ?? options.DefaultComplexity;
            ChildComplexity = childComplexity;
            Multipliers = cost?.Multipliers ?? Array.Empty<MultiplierPathString>();
            FieldDepth = fieldDepth;
            NodeDepth = nodeDepth;
            _valueCollection = valueCollection;
        }

        /// <summary>
        /// Gets the field for which the complexity is calculated.
        /// </summary>
        public IOutputField Field { get; }

        /// <summary>
        /// Gets the field selection that references the field in the query.
        /// </summary>
        public FieldNode Selection { get; }

        public int Complexity { get; }

        public int ChildComplexity { get; }

        public IReadOnlyList<MultiplierPathString> Multipliers { get; }

        public int FieldDepth { get; }

        public int NodeDepth { get; }

        public bool TryGetArgumentValue<T>(string name, [NotNullWhen(true)] out T value)
        {
            if (Field.Arguments.TryGetField(name, out IInputField? argument))
            {
                IValueNode? argumentValue = Selection.Arguments
                    .FirstOrDefault(t => t.Name.Value.EqualsOrdinal(name))?
                    .Value;

                if (argumentValue is VariableNode variable &&
                    _valueCollection.TryGetVariable(variable.Name.Value, out T castedVariable))
                {
                    value = castedVariable;
                    return true;
                }

                if (argumentValue is not null)
                {
                    try
                    {
                        if (argument.Type.ParseLiteral(argumentValue) is T castedArgument)
                        {
                            value = castedArgument;
                            return true;
                        }
                    }
                    catch (SerializationException)
                    {
                        // we ignore serialization errors and fall through.
                    }
                }
            }

            value = default!;
            return false;
        }
    }
}
