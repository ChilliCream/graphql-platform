using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion;

public sealed class SettingsComposerUrlResolutionTests
{
    private const string LocalUrl = "http://localhost:5001/graphql";

    [Fact]
    public void Compose_Should_UseLocalUrlAndStripDevUrl_When_SchemaHasLocalUrlOverride()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.internal.example.com/graphql",
                  "devUrl": "https://products.dev.example.com/graphql",
                  "capabilities": {
                    "subscriptions": {
                      "supported": true
                    }
                  }
                }
              },
              "extensions": {
                "vendor": {
                  "mode": "test"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], LocalOverride("Products"), preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "transports": {
                    "http": {
                      "url": "http://localhost:5001/graphql",
                      "capabilities": {
                        "subscriptions": {
                          "supported": true
                        }
                      }
                    }
                  },
                  "extensions": {
                    "vendor": {
                      "mode": "test"
                    }
                  }
                }
              }
            }
            """);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Compose_Should_UseLocalUrlWithoutWarning_When_OverriddenUrlsContainUnresolvableVariables()
    {
        // arrange
        // the configured URLs of a source schema with a URL override are ignored, so their
        // variables never resolve and never warn.
        var settings = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "{{API_URL}}/graphql",
                  "devUrl": "{{DEV_API_URL}}/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], LocalOverride("Products"), preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "transports": {
                    "http": {
                      "url": "http://localhost:5001/graphql"
                    }
                  }
                }
              }
            }
            """);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Compose_Should_ComposeNormalizedAbsoluteUri_When_OverrideOmitsThePath()
    {
        // arrange
        // an override without a path composes as its normalized absolute URI, which carries a
        // trailing slash.
        var settings = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.internal.example.com/graphql"
                }
              }
            }
            """);
        var overrides = new Dictionary<string, Uri>
        {
            ["Products"] = new Uri("http://localhost:5001")
        };

        // act
        var composed = Compose([settings], overrides, preferDevUrls: false, new CompositionLog());

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "transports": {
                    "http": {
                      "url": "http://localhost:5001/"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Compose_Should_CreateHttpTransport_When_LocalOverrideAndSettingsHaveNoTransports()
    {
        // arrange
        var settings = Parse(
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

        // act
        var composed = Compose([settings], LocalOverride("Products"), preferDevUrls: true, new CompositionLog());

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "extensions": {
                    "vendor": {
                      "mode": "test"
                    }
                  },
                  "transports": {
                    "http": {
                      "url": "http://localhost:5001/graphql"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Compose_Should_AddHttpTransport_When_LocalOverrideAndTransportsHaveNoHttp()
    {
        // arrange
        // the websockets transport of a local source schema keeps its configured URL, only the
        // HTTP transport is redirected to the local resource.
        var settings = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "websockets": {
                  "url": "wss://products.internal.example.com/graphql",
                  "devUrl": "ws://localhost:5001/graphql"
                }
              }
            }
            """);

        // act
        var composed = Compose([settings], LocalOverride("Products"), preferDevUrls: true, new CompositionLog());

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "transports": {
                    "websockets": {
                      "url": "wss://products.internal.example.com/graphql"
                    },
                    "http": {
                      "url": "http://localhost:5001/graphql"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Compose_Should_UseDevUrl_When_ExternalSchemaAndDevUrlsArePreferred()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql",
                  "devUrl": "https://reviews.dev.example.com/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.dev.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Compose_Should_UseUrlAndWarn_When_ExternalSchemaHasNoDevUrl()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.internal.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
              "message": "The source schema 'Reviews' does not specify a 'devUrl' for its HTTP transport. The composed configuration uses its 'url', which might not be reachable from the local development environment.",
              "code": "SOURCE_SCHEMA_DEV_URL_MISSING",
              "severity": "Warning",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_UseUrlAndWarn_When_ExternalSchemaHasEmptyDevUrl()
    {
        // arrange
        // a blank development URL counts as not defined.
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql",
                  "devUrl": ""
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.internal.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
              "message": "The source schema 'Reviews' does not specify a 'devUrl' for its HTTP transport. The composed configuration uses its 'url', which might not be reachable from the local development environment.",
              "code": "SOURCE_SCHEMA_DEV_URL_MISSING",
              "severity": "Warning",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_ComposeConfiguredUrl_When_OptionsAreDefault()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: false, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.internal.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Compose_Should_StripDevUrlFromBothTransports_When_OptionsAreDefault()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql",
                  "devUrl": "https://reviews.dev.example.com/graphql"
                },
                "websockets": {
                  "url": "wss://reviews.internal.example.com/graphql",
                  "devUrl": "ws://localhost:5002/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: false, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.internal.example.com/graphql"
                    },
                    "websockets": {
                      "url": "wss://reviews.internal.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Compose_Should_UseDevUrlForWebsockets_When_ExternalSchemaAndDevUrlsArePreferred()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql",
                  "devUrl": "https://reviews.dev.example.com/graphql"
                },
                "websockets": {
                  "url": "wss://reviews.internal.example.com/graphql",
                  "devUrl": "ws://localhost:5002/graphql",
                  "subscriptions": {
                    "supported": true
                  }
                }
              }
            }
            """);

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, new CompositionLog());

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.dev.example.com/graphql"
                    },
                    "websockets": {
                      "url": "ws://localhost:5002/graphql",
                      "subscriptions": {
                        "supported": true
                      }
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Compose_Should_ResolveVariables_When_DevUrlContainsVariables()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "{{API_URL}}/graphql",
                  "devUrl": "{{DEV_API_URL}}/graphql"
                }
              },
              "environments": {
                "Development": {
                  "API_URL": "https://reviews.internal.example.com",
                  "DEV_API_URL": "http://localhost:5002"
                }
              }
            }
            """);

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, new CompositionLog());

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "http://localhost:5002/graphql"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Compose_Should_FallBackToUrlAndWarn_When_DevUrlVariableIsUnresolvable()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "{{API_URL}}/graphql",
                  "devUrl": "{{DEV_API_URL}}/graphql"
                }
              },
              "environments": {
                "Development": {
                  "API_URL": "https://reviews.internal.example.com"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.internal.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
              "message": "The 'devUrl' of the source schema 'Reviews' contains variables that are not defined for the environment 'Development'. The 'url' is used instead.",
              "code": "SOURCE_SCHEMA_URL_VARIABLE_UNRESOLVED",
              "severity": "Warning",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_KeepRawUrlAndWarn_When_UrlVariableIsUnresolvable()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "{{API_URL}}/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([settings], s_noOverrides, preferDevUrls: true, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "{{API_URL}}/graphql"
                    }
                  }
                }
              }
            }
            """);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
              "message": "The source schema 'Reviews' does not specify a 'devUrl' for its HTTP transport. The composed configuration uses its 'url', which might not be reachable from the local development environment.",
              "code": "SOURCE_SCHEMA_DEV_URL_MISSING",
              "severity": "Warning",
              "extensions": {}
            }
            """,
            """
            {
              "message": "The 'url' of the source schema 'Reviews' contains variables that are not defined for the environment 'Development'. The configured value is composed as it is.",
              "code": "SOURCE_SCHEMA_URL_VARIABLE_UNRESOLVED",
              "severity": "Warning",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_ResolveUrlsPerSchema_When_OnlySomeSchemasHaveLocalOverrides()
    {
        // arrange
        var products = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "https://products.internal.example.com/graphql",
                  "devUrl": "https://products.dev.example.com/graphql"
                }
              }
            }
            """);
        var reviews = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "https://reviews.internal.example.com/graphql",
                  "devUrl": "https://reviews.dev.example.com/graphql"
                }
              }
            }
            """);

        // act
        var composed = Compose(
            [products, reviews],
            LocalOverride("Products"), preferDevUrls: true,
            new CompositionLog());

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "transports": {
                    "http": {
                      "url": "http://localhost:5001/graphql"
                    }
                  }
                },
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.dev.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Compose_Should_Throw_When_LocalSchemaHasUnresolvableVariable()
    {
        // arrange
        // source schemas with a local URL override keep the strict variable semantics.
        var settings = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "websockets": {
                  "url": "{{WS_URL}}/graphql"
                }
              }
            }
            """);

        // act
        void Act() => Compose([settings], LocalOverride("Products"), preferDevUrls: true, new CompositionLog());

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("Variable 'WS_URL' not found in environment", exception.Message);
    }

    [Fact]
    public void TryResolveVariables_Should_ReturnResolvedValue_When_VariablesAreDefined()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "environments": {
                "Development": {
                  "API_URL": "http://localhost:5002"
                }
              }
            }
            """);

        // act
        var resolved = SettingsComposer.TryResolveVariables(
            "{{API_URL}}/graphql",
            settings,
            "Development",
            out var resolvedValue);

        // assert
        Assert.True(resolved);
        Assert.Equal("http://localhost:5002/graphql", resolvedValue);
    }

    [Fact]
    public void TryResolveVariables_Should_ReturnFalse_When_VariableIsNotDefined()
    {
        // arrange
        var settings = Parse(
            """
            {
              "name": "Reviews",
              "environments": {
                "Production": {
                  "API_URL": "https://reviews.internal.example.com"
                }
              }
            }
            """);

        // act
        var resolved = SettingsComposer.TryResolveVariables(
            "{{API_URL}}/graphql",
            settings,
            "Development",
            out var resolvedValue);

        // assert
        Assert.False(resolved);
        Assert.Null(resolvedValue);
    }

    [Fact]
    public void Compose_Should_ResolveEverySchemaAgainstTheEnvironment_When_SchemasDefineMultipleEnvironments()
    {
        // arrange
        var products = Parse(
            """
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "{{API_URL}}"
                }
              },
              "environments": {
                "Development": {
                  "API_URL": "https://products.dev.example.com/graphql"
                },
                "Production": {
                  "API_URL": "https://products.example.com/graphql"
                }
              }
            }
            """);
        var reviews = Parse(
            """
            {
              "name": "Reviews",
              "transports": {
                "http": {
                  "url": "{{API_URL}}"
                }
              },
              "environments": {
                "Development": {
                  "API_URL": "https://reviews.dev.example.com/graphql"
                },
                "Production": {
                  "API_URL": "https://reviews.example.com/graphql"
                }
              }
            }
            """);
        var log = new CompositionLog();

        // act
        var composed = Compose([products, reviews], s_noOverrides, preferDevUrls: false, log);

        // assert
        composed.MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Products": {
                  "transports": {
                    "http": {
                      "url": "https://products.dev.example.com/graphql"
                    }
                  }
                },
                "Reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.dev.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
        Assert.True(log.IsEmpty);
    }

    private static readonly Dictionary<string, Uri> s_noOverrides = [];

    private static Dictionary<string, Uri> LocalOverride(string schemaName)
        => new() { [schemaName] = new Uri(LocalUrl) };

    private static string Compose(
        JsonElement[] sourceSchemaSettings,
        IReadOnlyDictionary<string, Uri> urlOverrides,
        bool preferDevUrls,
        CompositionLog compositionLog)
    {
        var buffer = new ArrayBufferWriter<byte>();

        new SettingsComposer().Compose(
            buffer,
            sourceSchemaSettings,
            "Development",
            urlOverrides,
            preferDevUrls,
            compositionLog);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;
}
