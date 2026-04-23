using System.Text.Json;
using HotChocolate.Fusion.Connectors.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Configuration;

public sealed class ParsersTests
{
    [Fact]
    public void TryParse_Should_Produce_Configuration_For_FlatScalarLookup()
    {
        // arrange
        const string settingsJson =
            """
            {
              "products": {
                "transports": { "http": { "url": "http://products/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productById": {
                        "entityType": "Product",
                        "arguments": { "id": "id" }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        Assert.Equal("products", federationConfig.Name);
        Assert.Equal("http://products/graphql", federationConfig.BaseAddress.ToString());
        Assert.True(federationConfig.Lookups.TryGetValue("productById", out var lookup));
        Assert.Equal("Product", lookup.EntityTypeName);
        Assert.Equal("id", Assert.Single(lookup.ArgumentToKeyFieldMap).Value);
    }

    [Fact]
    public void TryParse_Should_Accept_FlatCompositeKey_StringMap()
    {
        // arrange
        const string settingsJson =
            """
            {
              "products": {
                "transports": { "http": { "url": "http://products/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productBySkuAndPackage": {
                        "entityType": "Product",
                        "arguments": {
                          "sku": "sku",
                          "package": "package"
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        var lookup = federationConfig.Lookups["productBySkuAndPackage"];
        Assert.Equal("sku", lookup.ArgumentToKeyFieldMap["sku"]);
        Assert.Equal("package", lookup.ArgumentToKeyFieldMap["package"]);
    }

    [Fact]
    public void TryParse_Should_Accept_NestedKey_EmptyStringSplatMarker()
    {
        // arrange: an empty string for the argument's path signals the
        // connector to splat the variable's object fields into the
        // '_entities' representation root (used for wrapper-shape arguments
        // on nested/list '@key' lookups).
        const string settingsJson =
            """
            {
              "list": {
                "transports": { "http": { "url": "http://list/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productListByProductsAndIdAndPid": {
                        "entityType": "ProductList",
                        "arguments": { "key": "" }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        var lookup = federationConfig.Lookups["productListByProductsAndIdAndPid"];
        Assert.Equal("ProductList", lookup.EntityTypeName);
        Assert.Equal(string.Empty, lookup.ArgumentToKeyFieldMap["key"]);
    }

    [Fact]
    public void TryParse_Should_Accept_NestedKey_ObjectFormWithPathProperty()
    {
        // arrange: an object argument entry with a 'path' string property is
        // the richer shorthand for expressing the same mapping. Additional
        // metadata properties may be layered on later without breaking the
        // string form.
        const string settingsJson =
            """
            {
              "price": {
                "transports": { "http": { "url": "http://price/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productListByKey": {
                        "entityType": "ProductList",
                        "arguments": {
                          "key": { "path": "" }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        var lookup = federationConfig.Lookups["productListByKey"];
        Assert.Equal(string.Empty, lookup.ArgumentToKeyFieldMap["key"]);
    }

    [Fact]
    public void TryParse_Should_Reject_InvalidArgumentShape()
    {
        // arrange
        const string settingsJson =
            """
            {
              "products": {
                "transports": { "http": { "url": "http://products/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productById": {
                        "entityType": "Product",
                        "arguments": { "id": 42 }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act & assert
        Assert.Throws<InvalidOperationException>(
            () => parser.TryParse(sourceSchema, transport, out _));
    }

    [Fact]
    public void TryParse_Should_Accept_ObjectForm_WithPathSegment()
    {
        // arrange
        const string settingsJson =
            """
            {
              "email": {
                "transports": { "http": { "url": "http://email/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "userById": {
                        "entityType": "User",
                        "arguments": {
                          "id": { "path": "id" }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        var lookup = federationConfig.Lookups["userById"];
        Assert.Equal("id", lookup.ArgumentToKeyFieldMap["id"]);
    }

    [Fact]
    public void TryParse_Should_Accept_EntityRequires_Block()
    {
        // arrange: the composer emits per-entity-type field require metadata
        // under 'extensions.apolloFederation.entityTypes'. For each field
        // with synthetic '@require' arguments, the block carries the
        // argument name to representation field path mapping that the
        // rewriter uses to strip the argument and project the variable
        // value onto the '_entities' representation.
        const string settingsJson =
            """
            {
              "inventory": {
                "transports": { "http": { "url": "http://inventory/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productById": {
                        "entityType": "Product",
                        "arguments": { "id": "id" }
                      }
                    },
                    "entityTypes": {
                      "Product": {
                        "fields": {
                          "shippingEstimate": {
                            "requires": { "price": "price", "weight": "weight" }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        Assert.True(federationConfig.EntityRequires.TryGetValue("Product", out var productRequires));
        Assert.True(productRequires.Fields.TryGetValue("shippingEstimate", out var shippingArgs));
        Assert.Equal("price", shippingArgs["price"]);
        Assert.Equal("weight", shippingArgs["weight"]);
    }

    [Fact]
    public void TryParse_Should_Treat_Missing_EntityRequires_As_Empty()
    {
        // arrange
        const string settingsJson =
            """
            {
              "products": {
                "transports": { "http": { "url": "http://products/graphql" } },
                "extensions": {
                  "apolloFederation": {
                    "lookups": {
                      "productById": {
                        "entityType": "Product",
                        "arguments": { "id": "id" }
                      }
                    }
                  }
                }
              }
            }
            """;

        var (sourceSchema, transport) = ReadSettings(settingsJson);
        var parser = new ApolloFederationClientConfigurationParser();

        // act
        var matched = parser.TryParse(sourceSchema, transport, out var configuration);

        // assert
        Assert.True(matched);
        var federationConfig = Assert.IsType<ApolloFederationSourceSchemaClientConfiguration>(configuration);
        Assert.Empty(federationConfig.EntityRequires);
    }

    private static (JsonProperty SourceSchema, JsonProperty Transport) ReadSettings(string settingsJson)
    {
        var document = JsonDocument.Parse(settingsJson);
        var sourceSchema = document.RootElement.EnumerateObject().First();
        var transport = sourceSchema.Value.GetProperty("transports").EnumerateObject().First();
        return (sourceSchema, transport);
    }
}
