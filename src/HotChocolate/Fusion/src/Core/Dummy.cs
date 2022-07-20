using System.Collections;
using HotChocolate.Execution.Processing;
using Types = HotChocolate.Types;

namespace HotChocolate.Fusion;

public class QueryPlanBuilder
{
    private readonly Schema _schema = default!;

    public void Foo(IOperation operation, ISelectionSet selectionSet, ObjectType type)
    {
        foreach (var schemaName in _schema.Bindings)
        {

        }


        foreach (var selection in selectionSet.Selections)
        {


        }
    }

    private int CalculateSchemaScore(
        IOperation operation,
        ISelectionSet selectionSet,
        ObjectType type,
        string schemaName)
    {
        var score = 0;

        foreach (var selection in selectionSet.Selections)
        {
            if (type.Fields[selection.Field.Name].Bindings.ContainsSchema(schemaName))
            {
                score++;

                if (selection.SelectionSet is not null)
                {
                    foreach (var possibleType in operation.GetPossibleTypes(selection))
                    {
                        var resultType = _schema.GetType<ObjectType>(possibleType.Name);
                        var resultSelectionSet = operation.GetSelectionSet(selection, possibleType);
                        score += CalculateSchemaScore(operation, resultSelectionSet, resultType, schemaName);
                    }
                }
            }
        }

        return score;
    }
}

public interface IType
{
    string Name { get; }
}

public class Schema
{
    public IReadOnlyList<string> Bindings { get; }

    public IType this[string typeName] => throw new NotImplementedException();

    public T GetType<T>(string typeName) where T : IType => throw new NotImplementedException();
}

public class ObjectType : IType
{
    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public ObjectFieldCollection Fields { get; }
}

public class ObjectField
{
    public string Name { get; }

    public MemberBindingCollection Bindings { get; }
}

public class ObjectFieldCollection
{
    public int Count { get; }

    public ObjectField this[string fieldName] => throw new NotImplementedException();

    public bool TryGetValue(string fieldName, out ObjectField value)
    {
        throw new NotImplementedException();
    }
}

public class MemberBindingCollection
{
    public int Count { get; }

    public MemberBinding this[string schema] => throw new NotImplementedException();

    public bool TryGetValue(string schema, out MemberBinding value)
    {
        throw new NotImplementedException();
    }

    public bool ContainsSchema(string schema) => throw new NotImplementedException();
}


/// <summary>
/// The type system member binding information.
/// </summary>
public class MemberBinding
{
    /// <summary>
    /// Initializes a new instance of <see cref="MemberBinding"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema to which the type system member is bound to.
    /// </param>
    /// <param name="name">
    /// The name which the type system member has in the <see cref="Schema"/>.
    /// </param>
    public MemberBinding(string schema, string name)
    {
        Schema = schema;
        Name = name;
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string Schema { get; }

    /// <summary>
    /// Gets the name which the type system member has in the <see cref="Schema"/>.
    /// </summary>
    public string Name { get; }
}
