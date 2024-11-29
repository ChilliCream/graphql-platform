using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeScalarType : ICompositeNamedType
{
    private DirectiveCollection _directives = default!;
    private bool _completed;

    public CompositeScalarType(string name,
        string? description,
        ScalarResultType scalarResultType = ScalarResultType.Unknown)
    {
        Name = name;
        Description = description;
        ScalarResultType = scalarResultType;

        if(scalarResultType is ScalarResultType.Unknown)
        {
            ScalarResultType = name switch
            {
                "ID" => ScalarResultType.String,
                "String" => ScalarResultType.String,
                "Int" => ScalarResultType.Int,
                "Float" => ScalarResultType.Float,
                "Boolean" => ScalarResultType.Boolean,
                _ => ScalarResultType.Unknown
            };
        }
    }

    public TypeKind Kind => TypeKind.Scalar;

    public string Name { get; }

    public string? Description { get; }

    public DirectiveCollection Directives
    {
        get => _directives;
        private set
        {
            if(_completed)
            {
                throw new InvalidOperationException(
                    "The type is completed and cannot be modified.");
            }

            _directives = value;
        }
    }

    public ScalarResultType ScalarResultType { get; }

    internal void Complete(CompositeScalarTypeCompletionContext context)
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                "The type is completed and cannot be modified.");
        }

        Directives = context.Directives;
        _completed = true;
    }

    public override string ToString() => Name;
}
