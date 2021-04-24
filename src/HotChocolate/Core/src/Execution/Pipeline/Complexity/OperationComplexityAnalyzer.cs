using System;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline.Complexity
{
    internal class OperationComplexityAnalyzer
    {
        public OperationComplexityAnalyzer(
            OperationDefinitionNode operationDefinitionNode,
            ComplexityAnalyzerDelegate analyzer)
        {
            OperationDefinitionNode = operationDefinitionNode;
            Analyzer = analyzer;
        }

        public OperationDefinitionNode OperationDefinitionNode { get;  }

        public ComplexityAnalyzerDelegate Analyzer { get; }
    }
}
