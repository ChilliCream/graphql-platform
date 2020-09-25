using System;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.StarWars;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class VariableCoercionBenchmarks
    {
        private readonly VariableCoercionHelper _variableCoercionHelper;
        private readonly ISchema _schema;
        private readonly List<VariableDefinitionNode> _stringDefinition =
            new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("String"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };
        private readonly List<VariableDefinitionNode> _objectDefinition =
            new List<VariableDefinitionNode>
            {
                new VariableDefinitionNode(
                    null,
                    new VariableNode("abc"),
                    new NamedTypeNode("ReviewInput"),
                    new StringValueNode("def"),
                    Array.Empty<DirectiveNode>())
            };
        private readonly Dictionary<string, object> _stringLiteral =
            new Dictionary<string, object>
            {
                {"abc", new StringValueNode("foo")}
            };
        private readonly Dictionary<string, object> _objectLiteral =
            new Dictionary<string, object>
            {
                {"abc", new ObjectValueNode(new ObjectFieldNode("stars", 5))}
            };
        private readonly Dictionary<string, object> _string =
            new Dictionary<string, object>
            {
                {"abc","foo"}
            };
        private readonly Dictionary<string, object> _object =
            new Dictionary<string, object>
            {
                {"abc", new Dictionary<string, object> { {"stars", 5} }}
            };
        private readonly Dictionary<string, VariableValue> _result =
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

        [Benchmark]
        public void One_Object_Literal()
        {
            _result.Clear();
            _variableCoercionHelper.CoerceVariableValues(
                _schema, _objectDefinition, _objectLiteral, _result);
        }

        [Benchmark]
        public void One_Object_Dictionary()
        {
            _result.Clear();
            _variableCoercionHelper.CoerceVariableValues(
                _schema, _objectDefinition, _object, _result);
        }
    }
}
