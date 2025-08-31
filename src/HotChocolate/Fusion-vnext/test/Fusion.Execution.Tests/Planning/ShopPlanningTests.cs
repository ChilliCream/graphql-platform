namespace HotChocolate.Fusion.Planning;

public class ShopPlanningTests : FusionTestBase
{
    [Fact]
    public void Medium_Query_With_Aliases()
    {
        // assert
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                nodes {
                  reviews {
                    nodes {
                      stars
                      author {
                        birthdate
                        username
                      }
                      b: product {
                        weight
                      }
                      stars
                      product {
                        weight
                        deliveryEstimate(zip: "Foo")
                        pictureFileName
                        price
                        quantity
                        item {
                          product {
                            weight
                            deliveryEstimate(zip: "Foo")
                            pictureFileName
                            reviews {
                              edges {
                                node {
                                  author {
                                    birthdate
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                  b:reviews {
                    nodes {
                      stars
                      author {
                        birthdate
                        username
                      }
                      b: product {
                        weight
                      }
                      stars
                      product {
                        weight
                        deliveryEstimate(zip: "Foo")
                        pictureFileName
                        price
                        quantity
                        item {
                          product {
                            weight
                            deliveryEstimate(zip: "Foo")
                            pictureFileName
                            reviews {
                              edges {
                                node {
                                  author {
                                    birthdate
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                  weight
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Medium_Query_With_Aliases_1()
    {
        // assert
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query verybig {
              a: products {
                nodes {
                  reviews {
                    nodes {
                      stars
                      author {
                        birthdate
                        username
                      }
                      b: product {
                        weight
                      }
                      stars
                      product {
                        weight
                        deliveryEstimate(zip: "Foo")
                        pictureFileName
                        pictureUrl
                        price
                        quantity
                        item {
                          product {
                            weight
                            deliveryEstimate(zip: "Foo")
                            pictureFileName
                            pictureUrl
                            reviews {
                              edges {
                                node {
                                  author {
                                    birthdate
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                  b: reviews {
                    nodes {
                      stars
                      author {
                        birthdate
                        username
                      }
                      b: product {
                        weight
                      }
                      stars
                      product {
                        weight
                        deliveryEstimate(zip: "Foo")
                        pictureFileName
                        pictureUrl
                        price
                        quantity

                      }
                    }
                  }
                  weight
                }
              }
            }

            """);

        // assert
        MatchSnapshot(plan);
    }
}
