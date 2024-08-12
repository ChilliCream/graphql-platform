namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public static class TupleBuilderExtensions
{
    public static TupleBuilder AddMemberRange(
        this TupleBuilder builder,
        IEnumerable<string> range)
    {
        foreach (var member in range)
        {
            builder.AddMember(member);
        }

        return builder;
    }

    public static TupleBuilder AddMemberRange(
        this TupleBuilder builder,
        IEnumerable<ICode> range)
    {
        foreach (var member in range)
        {
            builder.AddMember(member);
        }

        return builder;
    }
}
