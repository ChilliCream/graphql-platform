using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    public partial class CypherVisitor
    {
        public void EnterVisitable(Match match)
        {
            if (match.IsOptional())
            {
                _writer.Write("OPTIONAL ");
            }
            _writer.Write("MATCH ");
        }

        public void LeaveVistable(Match match)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(Where where)
        {
            _writer.Write(" WHERE ");
        }

        public void EnterVisitable(Create create)
        {
            _writer.Write("CREATE ");
        }

        public void LeaveVistable(Create create)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(Node node)
        {
            _writer.Write("(");
        }

        public void LeaveVistable(Node node)
        {
            _writer.Write(")");
        }

        public void EnterVisitable(SymbolicName symbolicName)
        {
            _writer.Write(symbolicName.GetValue());
        }

        public void EnterVisitable(NodeLabel nodeLabel)
        {
            _writer.Write(Symbol.NodeLabelStart);
            _writer.Write(nodeLabel.GetValue());
        }

        public void EnterVisitable(Properties props)
        {
            _writer.Write(" {");
            _writer.Write(string.Join(
                ", ",
                props.Props.Select(i => $" {i.Key}: {i.Value.AsString()} ")).TrimEnd(' ', ','));
        }

        public void LeaveVistable(Properties properties)
        {
            _writer.Write(" }");
        }
    }
}
