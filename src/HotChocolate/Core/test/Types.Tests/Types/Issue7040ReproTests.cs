namespace HotChocolate.Types;

public class Issue7040ReproTests
{
    [Fact]
    public void Enum_DefaultValueAttribute_With_Integer_Default_Does_Not_Downgrade_To_Int()
    {
        SchemaBuilder.New()
            .AddInputObjectType<InputWithEnumIntDefault>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .MatchSnapshot();
    }

    public class InputWithEnumIntDefault
    {
        // The F# compiler boxes enum values as their underlying type when passed
        // to attribute constructors that accept object. So [<DefaultValue(MyEnum.Value1)>]
        // in F# arrives at runtime as [DefaultValue(0)] rather than [DefaultValue(MyEnum.Value1)].
        // We use the integer literal here to reproduce that behavior in C#.
        [DefaultValue(0)]
        public Issue7040Enum Enum { get; set; }
    }

    public enum Issue7040Enum
    {
        Value1 = 0,
        Value2 = 1
    }
}
