using System.Text.Json;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaSettingsReaderTests
{
    private static readonly JsonSerializerOptions s_indentedJson = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void TryRead_Should_ClassifyAndRemoveSupportFromRuntimeSettings_When_Version1SettingIsExact()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "1.0"
                  },
                  "other": {
                    "enabled": true
                  }
                },
                "vendor": {
                  "mode": "test"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out var result);

        // assert
        Assert.True(success);
        Assert.True(result.Options.IsApolloFederationV1);
        Assert.True(log.IsEmpty);
        using var runtimeSettings = Assert.IsType<JsonDocument>(result.RuntimeSettings);

        var snapshot = new
        {
            OriginalSettings = settings.RootElement,
            RuntimeSettings = runtimeSettings.RootElement
        };

        JsonSerializer.Serialize(snapshot, s_indentedJson).MatchInlineSnapshot(
            """
            {
              "OriginalSettings": {
                "name": "Products",
                "extensions": {
                  "chillicream": {
                    "apolloFederationSupport": {
                      "version": "1.0"
                    },
                    "other": {
                      "enabled": true
                    }
                  },
                  "vendor": {
                    "mode": "test"
                  }
                }
              },
              "RuntimeSettings": {
                "name": "Products",
                "extensions": {
                  "chillicream": {
                    "other": {
                      "enabled": true
                    }
                  },
                  "vendor": {
                    "mode": "test"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void TryRead_Should_PruneEmptyAncestors_When_SupportIsOnlyExtension()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "1.0"
                  }
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out var result);

        // assert
        Assert.True(success);
        Assert.True(result.Options.IsApolloFederationV1);
        Assert.True(log.IsEmpty);
        using var runtimeSettings = Assert.IsType<JsonDocument>(result.RuntimeSettings);
        Assert.Equal("{\"name\":\"Products\"}", runtimeSettings.RootElement.GetRawText());
    }

    [Fact]
    public void TryRead_Should_RemoveSupportWithoutEnablingLegacyParser_When_Version2SettingIsExact()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "2.0"
                  }
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out var result);

        // assert
        Assert.True(success);
        Assert.False(result.Options.IsApolloFederationV1);
        Assert.True(log.IsEmpty);
        using var runtimeSettings = Assert.IsType<JsonDocument>(result.RuntimeSettings);

        var snapshot = new
        {
            OriginalSettings = settings.RootElement,
            RuntimeSettings = runtimeSettings.RootElement
        };

        JsonSerializer.Serialize(snapshot, s_indentedJson).MatchInlineSnapshot(
            """
            {
              "OriginalSettings": {
                "name": "Products",
                "extensions": {
                  "chillicream": {
                    "apolloFederationSupport": {
                      "version": "2.0"
                    }
                  }
                }
              },
              "RuntimeSettings": {
                "name": "Products"
              }
            }
            """);
    }

    [Fact]
    public void TryRead_Should_LeaveBehaviorUnchanged_When_SupportIsAbsent()
    {
        // arrange
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "Products",
              "extensions": {
                "vendor": {
                  "mode": "test"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out var result);

        // assert
        Assert.True(success);
        Assert.False(result.Options.IsApolloFederationV1);
        Assert.Null(result.RuntimeSettings);
        Assert.True(log.IsEmpty);
    }

    [Theory]
    [InlineData(
        "[]",
        "The source schema setting '$' for source schema 'Products' must be an object, but found 'Array'.")]
    [InlineData(
        "{\"name\":\"Products\",\"extensions\":null}",
        "The source schema setting '$.extensions' for source schema 'Products' must be an object, but found 'Null'.")]
    [InlineData(
        "{\"name\":\"Products\",\"extensions\":{\"chillicream\":[]}}",
        "The source schema setting '$.extensions.chillicream' for source schema 'Products' must be an object, but found 'Array'.")]
    [InlineData(
        "{\"name\":\"Products\",\"extensions\":{\"chillicream\":{\"apolloFederationSupport\":true}}}",
        "The source schema setting '$.extensions.chillicream.apolloFederationSupport' for source schema 'Products' must be an object, but found 'True'.")]
    public void TryRead_Should_RejectWrongContainerKind_When_ExtensionPathIsInvalid(
        string json,
        string expectedMessage)
    {
        // arrange
        using var settings = JsonDocument.Parse(json);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out _);

        // assert
        Assert.False(success);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.InvalidApolloFederationSupportSettings, entry.Code);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(expectedMessage, entry.Message);
    }

    [Theory]
    [InlineData(
        "{}",
        "The source schema setting '$.extensions.chillicream.apolloFederationSupport' for source schema 'Products' must be an object containing only the 'version' property.")]
    [InlineData(
        "{\"version\":\"1.0\",\"enabled\":true}",
        "The source schema setting '$.extensions.chillicream.apolloFederationSupport' for source schema 'Products' must be an object containing only the 'version' property.")]
    [InlineData(
        "{\"version\":null}",
        "The source schema setting '$.extensions.chillicream.apolloFederationSupport.version' for source schema 'Products' must be a string, but found 'Null'.")]
    [InlineData(
        "{\"version\":1}",
        "The source schema setting '$.extensions.chillicream.apolloFederationSupport.version' for source schema 'Products' must be a string, but found 'Number'.")]
    public void TryRead_Should_RejectInvalidVersionSetting_When_SupportShapeIsInvalid(
        string supportJson,
        string expectedMessage)
    {
        // arrange
        using var settings = JsonDocument.Parse(
            $$"""
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {{supportJson}}
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out _);

        // assert
        Assert.False(success);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.InvalidApolloFederationSupportSettings, entry.Code);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(expectedMessage, entry.Message);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("1.0.0")]
    [InlineData("2.0.0")]
    [InlineData(" 1.0")]
    [InlineData("2.0 ")]
    [InlineData("3.0")]
    public void TryRead_Should_RejectVersion_When_VersionIsNotExact(string version)
    {
        // arrange
        using var settings = JsonDocument.Parse(
            $$"""
            {
              "name": "Products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "{{version}}"
                  }
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var success = SourceSchemaSettingsReader.TryRead(
            "Products",
            settings,
            log,
            out _);

        // assert
        Assert.False(success);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.InvalidApolloFederationSupportSettings, entry.Code);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(
            "The source schema setting "
                + "'$.extensions.chillicream.apolloFederationSupport.version' "
                + $"for source schema 'Products' has unsupported version '{version}'. "
                + "The supported versions are '1.0' and '2.0'.",
            entry.Message);
    }
}
