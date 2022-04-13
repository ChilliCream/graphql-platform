using System.Collections.Generic;
using ServiceStack;

namespace HotChocolate.Data.Neo4J.Language;

/// <summary>
/// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Merge.html
/// </summary>
public class Merge : Visitable, IUpdatingClause
{
    public Merge(Pattern pattern)
    {
        Pattern = pattern;
        OnCreateOrMatchEvents = new List<Visitable>();
    }

    public Merge(Pattern pattern, List<MergeAction> mergeActions)
    {
        Pattern = pattern;
        OnCreateOrMatchEvents = new List<Visitable>();
        OnCreateOrMatchEvents.AddRange(mergeActions);
    }
    public override ClauseKind Kind => ClauseKind.Merge;

    public Pattern Pattern { get; }
    public List<Visitable> OnCreateOrMatchEvents { get; }

    public bool HasEvents() => !OnCreateOrMatchEvents.IsNullOrEmpty();

    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);
        Pattern.Visit(cypherVisitor);
        OnCreateOrMatchEvents.ForEach(e => e.Visit(cypherVisitor));
        cypherVisitor.Leave(this);
    }
}
