using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    internal class SchemaSyntaxVisitor
        : SyntaxNodeWalker
    {
        private readonly List<ITypeDefinition> _typeDefinitions = new List<ITypeDefinition>();

        public IReadOnlyCollection<ITypeDefinition> TypeDefinitions => _typeDefinitions;

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition node)
        {
            _typeDefinitions.Add(new ObjectTypeDefinition(
                node.Name.Value,
                GetFieldDefinitions(node.Fields),
                node.Interfaces.Select(t => t.Name.Value)));
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition node)
        {
            _typeDefinitions.Add(new InputObjectTypeDefinition(
                node.Name.Value,
                GetInputValueDefinitions(node.Fields)));
        }

        protected override void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition node)
        {
            _typeDefinitions.Add(new InterfaceTypeDefinition(
                node.Name.Value,
                GetFieldDefinitions(node.Fields)));
        }

        protected override void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition node)
        {
            _typeDefinitions.Add(new UnionTypeDefinition(
                node.Name.Value,
                node.Types.Select(t => new NamedType(t.Name.Value))));
        }

        protected override void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition node)
        {
            _typeDefinitions.Add(new EnumTypeDefinition(
                node.Name.Value,
                node.Values.Select(t => t.ToString())));
        }

        private IEnumerable<FieldDefinition> GetFieldDefinitions(IEnumerable<GraphQLFieldDefinition> fieldDefinitions)
        {
            foreach (GraphQLFieldDefinition fieldDefinition in fieldDefinitions)
            {
                yield return new FieldDefinition(
                    fieldDefinition.Name.Value,
                    fieldDefinition.Type.Map(),
                    GetInputValueDefinitions(fieldDefinition.Arguments));
            }
        }

        private IEnumerable<InputValueDefinition> GetInputValueDefinitions(IEnumerable<GraphQLInputValueDefinition> inputValueDefinitions)
        {
            foreach (GraphQLInputValueDefinition inputValueDefinition in inputValueDefinitions)
            {
                yield return new InputValueDefinition(
                    inputValueDefinition.Name.Value,
                    inputValueDefinition.Type.Map(),
                    inputValueDefinition.DefaultValue.Map());
            }
        }
    }

    internal class SchemaSyntaxVisitor2
        : SyntaxNodeWalker
    {
        private readonly List<ITypeDefinition> _typeDefinitions = new List<ITypeDefinition>();

        public IReadOnlyCollection<ITypeDefinition> TypeDefinitions => _typeDefinitions;

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition node)
        {
            _typeDefinitions.Add(new ObjectTypeDefinition(
                node.Name.Value,
                GetFieldDefinitions(node.Fields),
                node.Interfaces.Select(t => t.Name.Value)));
        }

        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition node)
        {
            _typeDefinitions.Add(new InputObjectTypeDefinition(
                node.Name.Value,
                GetInputValueDefinitions(node.Fields)));
        }

        protected override void VisitInterfaceTypeDefinition(GraphQLInterfaceTypeDefinition node)
        {
            _typeDefinitions.Add(new InterfaceTypeDefinition(
                node.Name.Value,
                GetFieldDefinitions(node.Fields)));
        }

        protected override void VisitUnionTypeDefinition(GraphQLUnionTypeDefinition node)
        {
            _typeDefinitions.Add(new UnionTypeDefinition(
                node.Name.Value,
                node.Types.Select(t => new NamedType(t.Name.Value))));
        }

        protected override void VisitEnumTypeDefinition(GraphQLEnumTypeDefinition node)
        {
            _typeDefinitions.Add(new EnumTypeDefinition(
                node.Name.Value,
                node.Values.Select(t => t.ToString())));
        }

        private IEnumerable<FieldDefinition> GetFieldDefinitions(IEnumerable<GraphQLFieldDefinition> fieldDefinitions)
        {
            foreach (GraphQLFieldDefinition fieldDefinition in fieldDefinitions)
            {
                yield return new FieldDefinition(
                    fieldDefinition.Name.Value,
                    fieldDefinition.Type.Map(),
                    GetInputValueDefinitions(fieldDefinition.Arguments));
            }
        }

        private IEnumerable<InputValueDefinition> GetInputValueDefinitions(IEnumerable<GraphQLInputValueDefinition> inputValueDefinitions)
        {
            foreach (GraphQLInputValueDefinition inputValueDefinition in inputValueDefinitions)
            {
                yield return new InputValueDefinition(
                    inputValueDefinition.Name.Value,
                    inputValueDefinition.Type.Map(),
                    inputValueDefinition.DefaultValue.Map());
            }
        }
    }
}