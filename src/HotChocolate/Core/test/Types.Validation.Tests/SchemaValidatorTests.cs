using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Logging;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Types.Validation;

public sealed class SchemaValidatorTests
{
    private readonly ValidationLog _log = new();

    [Fact]
    public void Validate_WithDefaultRules()
    {
        // arrange
        var schema = SchemaParser.Parse("type Foo");
        var validator = new SchemaValidator();

        // act
        var success = validator.Validate(schema, _log);

        // assert
        Assert.False(success);
        _log.Select(e => e.ToString()).MatchInlineSnapshots(
            [
                // lang=json
                """
                {
                    "message": "The Object type 'Foo' must define one or more fields.",
                    "code": "HCV0001",
                    "severity": "Error",
                    "coordinate": "Foo",
                    "member": "Foo",
                    "extensions": {
                        "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                    }
                }
                """
            ]);
    }

    [Fact]
    public void Validate_WithCustomRule()
    {
        // arrange
        var schema = SchemaParser.Parse("type Foo");
        var validator = new SchemaValidator([new CustomRule()]);

        // act
        var success = validator.Validate(schema, _log);

        // assert
        Assert.True(success);
        _log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            // lang=json
            """
            {
                "message": "Avoid naming object types 'Foo'.",
                "code": "C0001",
                "severity": "Warning",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_WithCustomAndDefaultRules()
    {
        // arrange
        var schema = SchemaParser.Parse("type Foo");
        var validator = new SchemaValidator([new CustomRule()]);
        validator.AddDefaultRules();

        // act
        var success = validator.Validate(schema, _log);

        // assert
        Assert.False(success);
        _log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            // lang=json
            """
            {
                "message": "Avoid naming object types 'Foo'.",
                "code": "C0001",
                "severity": "Warning",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {}
            }
            """,
            // lang=json
            """
            {
                "message": "The Object type 'Foo' must define one or more fields.",
                "code": "HCV0001",
                "severity": "Error",
                "coordinate": "Foo",
                "member": "Foo",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Objects.Type-Validation"
                }
            }
            """
        ]);
    }

    private sealed class CustomRule : IValidationEventHandler<ObjectTypeEvent>
    {
        public void Handle(ObjectTypeEvent @event, ValidationContext context)
        {
            var objectType = @event.ObjectType;

            if (objectType.Name == "Foo")
            {
                context.Log.Write(
                    LogEntryBuilder.New()
                        .SetMessage("Avoid naming object types 'Foo'.")
                        .SetCode("C0001")
                        .SetSeverity(LogSeverity.Warning)
                        .SetTypeSystemMember(objectType)
                        .Build());
            }
        }
    }
}
