# Execute_CostQuery_ReturnsExpectedResult

## Schema

```text
# Weights used:
# 1.0 = (default for composite and list types)
# 2.0 = ArgumentDefinition
# 3.0 = FieldDefinition
# 4.0 = Object
# 5.0 = InputFieldDefinition
# 6.0 = Scalar
# 7.0 = Enum

type Query {
    examples(limit: Int! @cost(weight: "2.0")): [Example1!]!
        @cost(weight: "3.0") @listSize(slicingArguments: ["limit"])
}

type Example1 @cost(weight: "4.0") {
    example1Field1(arg1: String!, arg2: String!): Boolean!
    example1Field2(arg1Input1: Input1!): Example2!
    example1Field3: Example3!
}

type Example2 {
    example2Field1(arg1: String!, arg2: String!): Boolean!
    example2Field2(arg1Input2: [Input2!]!): Int!
}

type Example3 {
    example3Field1: Scalar1!
    example3Field2: Enum1!
}

input Input1 { input1Field1: String! @cost(weight: "5.0"), input1Field2: Input2! }
input Input2 { input2Field1: String!, input2Field2: String! }

scalar Scalar1 @cost(weight: "6.0")

enum Enum1 @cost(weight: "7.0") { ENUM_VALUE1 }

directive @example(dirArg1: Int!, dirArg2: Input1!) on
    | FIELD
    | FRAGMENT_DEFINITION
    | FRAGMENT_SPREAD
    | INLINE_FRAGMENT
    | QUERY
    | VARIABLE_DEFINITION
```

## Query

```text
query($limit: Int! = 10 @example(dirArg1: 1, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } })) @example(dirArg1: 2, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } }) {
    examples(limit: $limit) @example(dirArg1: 3, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } }) {
        ... @example(dirArg1: 4, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } }) {
            example1Field1(arg1: "", arg2: "") @example(dirArg1: 5, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } })
        }

        # Repeated to test indexed paths (f.e. "query.examples.on~Example1[1]").
        ... { example1Field1(arg1: "", arg2: "") }

        example1Field2(
            arg1Input1: {
                input1Field1: ""
                input1Field2: { input2Field1: "", input2Field2: "" }
            }
        ) {
            ...fragment1 @example(dirArg1: 6, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } })
        }

        example1Field3 { example3Field1, aliasField2: example3Field2 }

        # Repeated to test indexed paths (f.e. "query.examples.example1Field3[1]")
        example1Field3 { aliasField1: example3Field1, example3Field2 }
    }

    __cost {
    requestCosts {
        exampleDirectiveOnQuery:
            fieldCostByLocation(regexPath: "query\\.@example.*")
                { path, cost }
        fragment1:
            fieldCostByLocation(regexPath: ".*~fragment1.*")
                { path, cost }
        example1Field3:
            fieldCostByLocation(regexPath: ".*\\.example1Field3\\[\\d+\\]")
                { path, cost }
    }
}
}

fragment fragment1 on Example2 @example(dirArg1: 7, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } }) {
    example2Field1(arg1: "", arg2: "") @example(dirArg1: 8, dirArg2: { input1Field1: "", input1Field2: { input2Field1: "", input2Field2: "" } })
    example2Field2(arg1Input2: [
        { input2Field1: "", input2Field2: "" }
        { input2Field1: "", input2Field2: "" }
    ])
}
```

## Result

```text
{
  "data": {
    "examples": [
      {
        "example1Field1": true,
        "example1Field2": {
          "example2Field1": true,
          "example2Field2": 1
        },
        "example1Field3": {
          "example3Field1": "",
          "aliasField2": "ENUM_VALUE1",
          "aliasField1": "",
          "example3Field2": "ENUM_VALUE1"
        }
      }
    ],
    "__cost": {
      "requestCosts": {
        "exampleDirectiveOnQuery": [
          {
            "path": "query.@example",
            "cost": 5
          },
          {
            "path": "query.@example(dirArg1:)",
            "cost": 0
          },
          {
            "path": "query.@example(dirArg2:)",
            "cost": 5
          },
          {
            "path": "query.@example(dirArg2:).input1Field1",
            "cost": 5
          },
          {
            "path": "query.@example(dirArg2:).input1Field2",
            "cost": 0
          },
          {
            "path": "query.@example(dirArg2:).input1Field2.input2Field1",
            "cost": 0
          },
          {
            "path": "query.@example(dirArg2:).input1Field2.input2Field2",
            "cost": 0
          }
        ],
        "fragment1": [
          {
            "path": "query.examples.example1Field2.~fragment1.@example",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1.@example(dirArg1:)",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1.@example(dirArg2:)",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1.@example(dirArg2:).input1Field1",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1.@example(dirArg2:).input1Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1.@example(dirArg2:).input1Field2.input2Field1",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1.@example(dirArg2:).input1Field2.input2Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1",
            "cost": 160
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example(dirArg1:)",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example(dirArg2:)",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example(dirArg2:).input1Field1",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example(dirArg2:).input1Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example(dirArg2:).input1Field2.input2Field1",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.@example(dirArg2:).input1Field2.input2Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1",
            "cost": 110
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1(arg1:)",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1(arg2:)",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example(dirArg1:)",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example(dirArg2:)",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example(dirArg2:).input1Field1",
            "cost": 50
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example(dirArg2:).input1Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example(dirArg2:).input1Field2.input2Field1",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field1.@example(dirArg2:).input1Field2.input2Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2",
            "cost": 10
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)",
            "cost": 10
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)[0].input2Field1",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)[0]",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)[0].input2Field2",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)[1].input2Field1",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)[1]",
            "cost": 0
          },
          {
            "path": "query.examples.example1Field2.~fragment1~~fragment1.example2Field2(arg1Input2:)[1].input2Field2",
            "cost": 0
          }
        ],
        "example1Field3": [
          {
            "path": "query.examples.example1Field3[0]",
            "cost": 10
          },
          {
            "path": "query.examples.example1Field3[1]",
            "cost": 10
          }
        ]
      }
    }
  }
}
```

