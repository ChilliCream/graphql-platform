using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeScalarType : ICompositeNamedType
{
    private DirectiveCollection _directives = default!;
    private bool _completed;

    public CompositeScalarType(string name,
        string? description,
        ResultType resultType = ResultType.Unknown)
    {
        Name = name;
        Description = description;
        ResultType = resultType;

        if(resultType is ResultType.Unknown)
        {
            ResultType = name switch
            {
                "ID" => ResultType.String,
                "String" => ResultType.String,
                "Int" => ResultType.Int,
                "Float" => ResultType.Float,
                "Boolean" => ResultType.Boolean,
                _ => ResultType.Unknown
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

    public ResultType ResultType { get; }

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
}

public enum ResultType
{
    String,
    Int,
    Float,
    Boolean,
    Unknown
}
