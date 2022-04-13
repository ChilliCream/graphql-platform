namespace HotChocolate.Data.Neo4J.Language;

/// <summary>
/// An action or event that happens after a MERGE.
/// </summary>
public class MergeAction : Visitable
{
    public MergeAction(Set set, MergeActionType mergeActionType)
    {
        Set = set;
        Type = mergeActionType;
    }
    public override ClauseKind Kind => ClauseKind.MergeAction;

    public enum MergeActionType {
        /**
		 * Triggered when a pattern has been created.
		 */
        OnCreate,
        /**
		 * Triggered when a pattern has been fully matched.
		 */
        OnMatch
    }

    public MergeActionType Type { get; }
    public Set Set { get; }

    public override void Visit(CypherVisitor cypherVisitor)
    {
        cypherVisitor.Enter(this);
        Set.Visit(cypherVisitor);
        cypherVisitor.Leave(this);
    }
}
