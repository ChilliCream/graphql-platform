namespace HotChocolate.Types;

public class Issue7040ReproTests
{
    [Fact]
    public void Enum_DefaultValueAttribute_With_Integer_Default_Does_Not_Downgrade_To_Int()
    {
        var schema = SchemaBuilder.New()
            .AddInputObjectType<InputWithEnumIntDefault>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var sdl = schema.ToString();

        Assert.Contains("enum: Issue7040Enum! = VALUE1", sdl, StringComparison.Ordinal);
        Assert.DoesNotContain("enum: Int! = 0", sdl, StringComparison.Ordinal);
    }

    public class InputWithEnumIntDefault
    {
        [DefaultValue(0)]
        public Issue7040Enum Enum { get; set; }
    }

    public enum Issue7040Enum
    {
        Value1 = 0,
        Value2 = 1
    }
}
