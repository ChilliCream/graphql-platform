using System;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.StarWars;
using System.Collections.Generic;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class VariableCoercionBenchmarks
    {
        private readonly VariableCoercionHelper _variableCoercionHelper;
        private readonly ISchema _schema;
        private readonly  List<VariableDefinitionNode> _stringDefinition =
            new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };
        private readonly  Dictionary<string, object> _stringLiteral =
            new Dictionary<string, object>
            {
                {"abc", new StringValueNode("foo")}
            };
        private readonly  Dictionary<string, object> _string =
            new Dictionary<string, object>
            {
                {"abc","foo"}
            };
        private readonly  Dictionary<string, VariableValue> _result =
            new Dictionary<string, VariableValue>();

        public VariableCoercionBenchmarks()
        {
            _variableCoercionHelper = new VariableCoercionHelper();
            _schema = SchemaBuilder.New().AddStarWarsTypes().Create();
        }

        [Benchmark]
        public void One_String_Literal()
        {
            _result.Clear();
            _variableCoercionHelper.CoerceVariableValues(
                _schema, _stringDefinition, _stringLiteral, _result);
        }

        [Benchmark]
        public void One_String()
        {
            _result.Clear();
            _variableCoercionHelper.CoerceVariableValues(
                _schema, _stringDefinition, _string, _result);
        }
    }
}
