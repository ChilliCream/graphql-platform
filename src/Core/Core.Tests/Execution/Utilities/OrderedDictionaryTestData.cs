using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Execution
{
    internal class OrderedDictionaryTestData
    {

        public static OrderedDictionary BasicErrorQueryResult
        {
            get
            {
                var queryResult = new OrderedDictionary();
                var errors = new List<QueryError>
                {
                    new QueryError("This is an error on the query."),
                    new QueryError("This is another error on the query.", Path.New("Example"))
                };

                queryResult["errors"] = errors;
                return queryResult;
            }
        }

        public static OrderedDictionary ComplexErrorQueryResult
        {
            get
            {
                var queryResult = new OrderedDictionary();
                var errors = new List<QueryError>
                {
                    new QueryError(
                        message: "Name for character with ID 1002 could not be fetched.",
                        locations: new List<Location>
                        {
                            new Location(6, 7)
                        },
                        path: Path.New("hero").Append("heroFriends").Append(1).Append("name")
                    )
                };
                var data = new OrderedDictionary
                {
                    ["hero"] = new OrderedDictionary
                    {
                        ["name"] = "R2-D2",
                        ["heroFriends"] = new List<OrderedDictionary>
                        {
                            new OrderedDictionary
                            {
                                ["id"] = "1000",
                                ["name"] = "Luke Skywalker"
                            },
                            new OrderedDictionary
                            {
                                ["id"] = "1002",
                                ["name"] = null
                            },
                            new OrderedDictionary
                            {
                                ["id"] = "1003",
                                ["name"] = "Leia Organa"
                            }
                        }
                    }
                };
                
                queryResult["errors"] = errors;
                queryResult["data"] = data;
                return queryResult;
            }
        }

        public static OrderedDictionary ComplexQueryResult2
        {
            get
            {
                var queryResult = new OrderedDictionary();
                var data = new OrderedDictionary()
                {
                    ["listOfLists"] = new List<List<List<string>>>
                {
                    new List<List<string>>
                    {
                        new List<string>
                        {
                            "1",
                            "I",
                            "Am",
                            "A",
                            "Long",
                            "Embedded",
                            "List"

                        }
                    },
                    new List<List<string>>
                    {
                        new List<string>
                        {
                            "2",
                            "I",
                            "Am",
                            "A",
                            "Long",
                            "Embedded",
                            "List"
                        }
                    },
                    new List<List<string>>
                    {
                        new List<string>
                        {
                            "3",
                            "I",
                            "Am",
                            "A",
                            "Long",
                            "Embedded",
                            "List"
                        }
                    },
                    new List<List<string>>
                    {
                        new List<string>
                        {
                            "4",
                            "I",
                            "Am",
                            "A",
                            "Long",
                            "Embedded",
                            "List"
                        }
                    }
                },
                    ["dictionary"] = new Dictionary<string, object>
                    {
                        ["listOfDictionary"] = new List<OrderedDictionary>
                    {
                        new OrderedDictionary
                        {
                            ["myList"] = new List<string>
                            {
                                "my",
                                "list"
                            }
                        }
                    }
                    }
                };

                queryResult["data"] = data;
                return queryResult;
            }
        }

        public static OrderedDictionary ComplexQueryResult1
        {
            get
            {
                var queryResult = new OrderedDictionary();
                var data = new OrderedDictionary()
                {
                    ["name"] = "Car",
                    ["age"] = 5,
                    ["price"] = 3.1445m,
                    ["quantity"] = 123123123,
                    ["cars"] = new OrderedDictionary
                    {
                        ["type"] = "Mazda",
                        ["year"] = 1992,
                        ["engineTypes"] = new List<string>
                    {
                        "rotary",
                        "combustion",
                        "plastic"
                    },
                        ["factoryLocations"] = new List<object>
                    {
                        new OrderedDictionary
                        {
                            ["location"] = "tokyo"
                        },
                        new OrderedDictionary
                        {
                            ["location"] = "los angeles"
                        }
                    },
                        ["drivers"] = new List<OrderedDictionary>
                    {
                        new OrderedDictionary
                        {
                            ["name"] = "Bob"
                        },
                        new OrderedDictionary
                        {
                            ["name"] = "Joseph"
                        }
                    }
                    }
                };

                queryResult["data"] = data;
                return queryResult;
            }
        }

        public static OrderedDictionary BasicQueryResult
        {
            get
            {
                var queryResult = new OrderedDictionary();
                var data = new OrderedDictionary();
                var dataObject = new OrderedDictionary()
                {
                    ["name"] = "Car",
                    ["age"] = 5,
                    ["price"] = 3.1445m
                };

                queryResult["data"] = data;
                data["object"] = dataObject;
                return queryResult;
            }
        }
    }
}
