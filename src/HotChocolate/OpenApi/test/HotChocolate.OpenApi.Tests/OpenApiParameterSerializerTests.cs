using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi.Tests;

public sealed class OpenApiParameterSerializerTests
{
    [Theory]
    [MemberData(nameof(StringParameters))]
    public void SerializeParameter_String_ReturnsExpectedResult(
        OpenApiParameter parameter,
        string? value,
        string result)
    {
        Assert.Equal(result, OpenApiParameterSerializer.SerializeParameter(parameter, value));
    }

    [Theory]
    [MemberData(nameof(ListParameters))]
    public void SerializeParameter_List_ReturnsExpectedResult(
        OpenApiParameter parameter,
        List<object?> values,
        string result)
    {
        Assert.Equal(result, OpenApiParameterSerializer.SerializeParameter(parameter, values));
    }

    [Theory]
    [MemberData(nameof(ObjectParameters))]
    public void SerializeParameter_Object_ReturnsExpectedResult(
        OpenApiParameter parameter,
        Dictionary<string, object?> value,
        string result)
    {
        Assert.Equal(result, OpenApiParameterSerializer.SerializeParameter(parameter, value));
    }

    public static TheoryData<OpenApiParameter, string?, string> StringParameters()
    {
        return new TheoryData<OpenApiParameter, string?, string>
        {
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Matrix,
                },
                "blue",
                ";color=blue"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Label,
                },
                "blue",
                ".blue"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Form,
                },
                "blue",
                "color=blue"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Simple,
                },
                "blue",
                "blue"
            },
            // Null values
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Matrix,
                },
                null,
                ";color"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Label,
                },
                null,
                "."
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Form,
                },
                null,
                "color="
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Style = ParameterStyle.Simple,
                },
                null,
                ""
            },
        };
    }

    public static TheoryData<OpenApiParameter, List<object?>, string> ListParameters()
    {
        return new TheoryData<OpenApiParameter, List<object?>, string>
        {
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Matrix,
                },
                ["blue", "black", "brown"],
                ";color=blue,black,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Matrix,
                },
                ["blue", "black", "brown"],
                ";color=blue;color=black;color=brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Label,
                },
                ["blue", "black", "brown"],
                ".blue.black.brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Label,
                },
                ["blue", "black", "brown"],
                ".blue.black.brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Form,
                },
                ["blue", "black", "brown"],
                "color=blue,black,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Form,
                },
                ["blue", "black", "brown"],
                "color=blue&color=black&color=brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Simple,
                },
                ["blue", "black", "brown"],
                "blue,black,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Simple,
                },
                ["blue", "black", "brown"],
                "blue,black,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.SpaceDelimited,
                },
                ["blue", "black", "brown"],
                "blue%20black%20brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.PipeDelimited,
                },
                ["blue", "black", "brown"],
                "blue|black|brown"
            },
            // Null values
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Matrix,
                },
                ["blue", null, "brown"],
                ";color=blue,,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Matrix,
                },
                ["blue", null, "brown"],
                ";color=blue;color;color=brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Label,
                },
                ["blue", null, "brown"],
                ".blue..brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Label,
                },
                ["blue", null, "brown"],
                ".blue..brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Form,
                },
                ["blue", null, "brown"],
                "color=blue,,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Form,
                },
                ["blue", null, "brown"],
                "color=blue&color=&color=brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Simple,
                },
                ["blue", null, "brown"],
                "blue,,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Simple,
                },
                ["blue", null, "brown"],
                "blue,,brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.SpaceDelimited,
                },
                ["blue", null, "brown"],
                "blue%20%20brown"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.PipeDelimited,
                },
                ["blue", null, "brown"],
                "blue||brown"
            },
        };
    }

    public static TheoryData<OpenApiParameter, Dictionary<string, object?>, string> ObjectParameters()
    {
        return new TheoryData<OpenApiParameter, Dictionary<string, object?>, string>
        {
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Matrix,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                ";color=R,100,G,200,B,150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Matrix,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                ";R=100;G=200;B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Label,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                ".R.100.G.200.B.150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Label,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                ".R=100.G=200.B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Form,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "color=R,100,G,200,B,150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Form,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "R=100&G=200&B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Simple,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "R,100,G,200,B,150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Simple,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "R=100,G=200,B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.SpaceDelimited,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "R%20100%20G%20200%20B%20150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.PipeDelimited,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "R|100|G|200|B|150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.DeepObject,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", 200 }, { "B", 150 } },
                "color[R]=100&color[G]=200&color[B]=150"
            },
            // Null values
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Matrix,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                ";color=R,100,G,,B,150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Matrix,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                ";R=100;G;B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Label,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                ".R.100.G..B.150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Label,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                ".R=100.G=.B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Form,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "color=R,100,G,,B,150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Form,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "R=100&G=&B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.Simple,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "R,100,G,,B,150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.Simple,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "R=100,G=,B=150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.SpaceDelimited,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "R%20100%20G%20%20B%20150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = false,
                    Style = ParameterStyle.PipeDelimited,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "R|100|G||B|150"
            },
            {
                new OpenApiParameter()
                {
                    Name = "color",
                    Explode = true,
                    Style = ParameterStyle.DeepObject,
                },
                new Dictionary<string, object?>() { { "R", 100 }, { "G", null }, { "B", 150 } },
                "color[R]=100&color[G]=&color[B]=150"
            },
        };
    }
}
