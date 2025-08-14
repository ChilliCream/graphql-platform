using System.Buffers;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion
{
    public class SettingsComposerTests
    {
        private readonly SettingsComposer _composer = new();

        [Fact]
        public void Compose_WithNullGatewaySettings_ThrowsArgumentNullException()
        {
            // Arrange
            var sourceSchema = CreateSimpleSourceSchema("TestService", "http://test.com/graphql");
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => _composer.Compose(null!, sourceSchemas, "development"));
        }

        [Fact]
        public void Compose_WithNullOrEmptyEnvironment_ThrowsArgumentException()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var sourceSchema = CreateSimpleSourceSchema("TestService", "http://test.com/graphql");
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => _composer.Compose(buffer, sourceSchemas, null!));

            Assert.Throws<ArgumentException>(
                () => _composer.Compose(buffer, sourceSchemas, ""));
        }

        [Fact]
        public void Compose_WithEmptySourceSchemas_ThrowsArgumentException()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            JsonElement[] sourceSchemas = [];

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => _composer.Compose(buffer, sourceSchemas, "development"));

            Assert.Contains("At least one source schema settings document is required", exception.Message);
        }

        [Fact]
        public void Compose_WithMissingNameProperty_ThrowsInvalidOperationException()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "transports": {
                    "http": {
                        "url": "http://test.com/graphql"
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _composer.Compose(buffer, sourceSchemas, "development"));

            Assert.Contains("Source schema missing required 'name' property", exception.Message);
        }

        [Fact]
        public void Compose_WithEmptyNameProperty_ThrowsInvalidOperationException()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "",
                "transports": {
                    "http": {
                        "url": "http://test.com/graphql"
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _composer.Compose(buffer, sourceSchemas, "development"));

            Assert.Contains("Source schema 'name' property cannot be empty", exception.Message);
        }

        [Fact]
        public void Compose_SimpleSchemaWithoutVariables_ProducesValidGatewaySettings()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var sourceSchema = CreateSimpleSourceSchema("UserService", "http://users.api.com/graphql");
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            Assert.True(gatewaySettings.RootElement.TryGetProperty("sourceSchemas", out var sourceSchemasProp));
            Assert.True(sourceSchemasProp.TryGetProperty("UserService", out var userService));
            Assert.True(userService.TryGetProperty("transports", out var transports));
            Assert.True(transports.TryGetProperty("http", out var http));
            Assert.True(http.TryGetProperty("url", out var url));
            Assert.Equal("http://users.api.com/graphql", url.GetString());

            // Ensure name and environments are not included
            Assert.False(userService.TryGetProperty("name", out _));
            Assert.False(userService.TryGetProperty("environments", out _));
        }

        [Fact]
        public void Compose_MultipleSchemas_ProducesValidGatewaySettings()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            var userService = CreateSimpleSourceSchema("UserService", "http://users.api.com/graphql");
            var productService = CreateSimpleSourceSchema("ProductService", "http://products.api.com/graphql");
            JsonElement[] sourceSchemas = [userService, productService];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            Assert.True(gatewaySettings.RootElement.TryGetProperty("sourceSchemas", out var sourceSchemasProp));
            Assert.True(sourceSchemasProp.TryGetProperty("UserService", out _));
            Assert.True(sourceSchemasProp.TryGetProperty("ProductService", out _));
        }

        [Fact]
        public void Compose_WithStringInterpolation_ResolvesVariables()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "{{API_URL}}/graphql"
                    }
                },
                "environments": {
                    "development": {
                        "API_URL": "https://dev-api.example.com"
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            var url = gatewaySettings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("UserService")
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("url");

            Assert.Equal("https://dev-api.example.com/graphql", url.GetString());
        }

        [Fact]
        public void Compose_WithPureVariableReference_PreservesOriginalType()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "http://test.com/graphql",
                        "capabilities": {
                            "subscriptions": {
                                "supported": "{{HTTP_SUBSCRIPTIONS_ENABLED}}"
                            }
                        }
                    }
                },
                "environments": {
                    "development": {
                        "HTTP_SUBSCRIPTIONS_ENABLED": true
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            var supported = gatewaySettings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("UserService")
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("capabilities")
                .GetProperty("subscriptions")
                .GetProperty("supported");

            Assert.Equal(JsonValueKind.True, supported.ValueKind);
            Assert.True(supported.GetBoolean());
        }

        [Fact]
        public void Compose_WithNumberVariable_PreservesNumericType()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "http://test.com/graphql"
                    }
                },
                "extensions": {
                    "timeout": "{{TIMEOUT_MS}}"
                },
                "environments": {
                    "development": {
                        "TIMEOUT_MS": 5000
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            var timeout = gatewaySettings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("UserService")
                .GetProperty("extensions")
                .GetProperty("timeout");

            Assert.Equal(JsonValueKind.Number, timeout.ValueKind);
            Assert.Equal(5000, timeout.GetInt32());
        }

        [Fact]
        public void Compose_WithMissingVariable_ThrowsInvalidOperationException()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "{{API_URL}}/graphql"
                    }
                },
                "environments": {
                    "development": {
                        "OTHER_VAR": "value"
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _composer.Compose(buffer, sourceSchemas, "development"));

            Assert.Contains("Variable 'API_URL' not found in environment", exception.Message);
        }

        [Fact]
        public void Compose_WithMissingEnvironmentSection_WorksForSchemaWithoutVariables()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "http://test.com/graphql"
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert - Should not throw
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            Assert.True(gatewaySettings.RootElement.TryGetProperty("sourceSchemas", out var sourceSchemasProp));
            Assert.True(sourceSchemasProp.TryGetProperty("UserService", out _));
        }

        [Fact]
        public void Compose_WithComplexNestedStructure_PreservesStructure()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "{{API_URL}}/graphql",
                        "capabilities": {
                            "variableBatching": {
                                "supported": "{{VARIABLE_BATCHING_ENABLED}}",
                                "formats": ["application/graphql-response+jsonl"]
                            },
                            "subscriptions": {
                                "supported": true,
                                "formats": ["text/event-stream"]
                            }
                        }
                    },
                    "websockets": {
                        "url": "{{WS_URL}}/graphql",
                        "subscriptions": {
                            "supported": true
                        }
                    }
                },
                "extensions": {
                    "customTimeout": 30000,
                    "retryPolicy": {
                        "maxAttempts": "{{MAX_RETRY_ATTEMPTS}}"
                    }
                },
                "environments": {
                    "development": {
                        "API_URL": "https://dev-api.example.com",
                        "WS_URL": "wss://dev-api.example.com",
                        "VARIABLE_BATCHING_ENABLED": false,
                        "MAX_RETRY_ATTEMPTS": 3
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            var userService = gatewaySettings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("UserService");

            // Check HTTP transport
            var httpUrl = userService.GetProperty("transports").GetProperty("http").GetProperty("url");
            Assert.Equal("https://dev-api.example.com/graphql", httpUrl.GetString());

            // Check boolean variable preservation
            var variableBatchingSupported = userService
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("capabilities")
                .GetProperty("variableBatching")
                .GetProperty("supported");
            Assert.Equal(JsonValueKind.False, variableBatchingSupported.ValueKind);

            // Check WebSocket transport
            var wsUrl = userService.GetProperty("transports").GetProperty("websockets").GetProperty("url");
            Assert.Equal("wss://dev-api.example.com/graphql", wsUrl.GetString());

            // Check extensions
            var maxAttempts = userService
                .GetProperty("extensions")
                .GetProperty("retryPolicy")
                .GetProperty("maxAttempts");
            Assert.Equal(JsonValueKind.Number, maxAttempts.ValueKind);
            Assert.Equal(3, maxAttempts.GetInt32());

            // Ensure name and environments are excluded
            Assert.False(userService.TryGetProperty("name", out _));
            Assert.False(userService.TryGetProperty("environments", out _));
        }

        [Fact]
        public void Compose_WithArraysAndObjects_PreservesStructure()
        {
            // Arrange
            var buffer = new ArrayBufferWriter<byte>();
            const string sourceSchemaJson = """
            {
                "name": "UserService",
                "transports": {
                    "http": {
                        "url": "http://test.com/graphql",
                        "capabilities": {
                            "subscriptions": {
                                "formats": ["text/event-stream", "application/json"]
                            }
                        }
                    }
                },
                "extensions": {
                    "metadata": {
                        "tags": ["user", "authentication"],
                        "config": {
                            "nested": {
                                "value": true
                            }
                        }
                    }
                }
            }
            """;

            var sourceSchema = JsonDocument.Parse(sourceSchemaJson).RootElement;
            JsonElement[] sourceSchemas = [sourceSchema];

            // Act
            _composer.Compose(buffer, sourceSchemas, "development");

            // Assert
            var result = Encoding.UTF8.GetString(buffer.WrittenSpan);
            var gatewaySettings = JsonDocument.Parse(result);

            var userService = gatewaySettings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("UserService");

            // Check array preservation
            var formats = userService
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("capabilities")
                .GetProperty("subscriptions")
                .GetProperty("formats");

            Assert.Equal(JsonValueKind.Array, formats.ValueKind);
            Assert.Equal(2, formats.GetArrayLength());

            // Check nested object preservation
            var nestedValue = userService
                .GetProperty("extensions")
                .GetProperty("metadata")
                .GetProperty("config")
                .GetProperty("nested")
                .GetProperty("value");

            Assert.Equal(JsonValueKind.True, nestedValue.ValueKind);
        }

        private static JsonElement CreateSimpleSourceSchema(string name, string url)
        {
            var json = $$"""
            {
                "name": "{{name}}",
                "transports": {
                    "http": {
                        "url": "{{url}}"
                    }
                }
            }
            """;

            return JsonDocument.Parse(json).RootElement;
        }
    }
}
