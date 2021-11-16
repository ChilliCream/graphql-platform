### SumBy
**Directive**

```graphql
directive @sumBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@sumBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@sumBy* returns the sum of all values of the fields with name `key`.
If no value was found, *@sumBy* retruns null.

Given the following data:

```json example
{
  "list": [
    {
      "int": 1
    },
    {
      "int": 2
    },
    {
      "int": 3
    }
  ]
}
```

The execution of the following query:

```graphql example
{
  list @sumBy(key: "int"){
    int
  }
}
```

will retrun the following result:

```json example
{
  "list": 6
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteSumBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteSumByArray(node)}

RewriteSumByArray(value):
* Let {sum} be {null}
* For Each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, key)} 
      * Let {fieldValue} be {Get(element, key)}
      * If {IsNumber(fieldValue)}
          * If {sum} is {null}
            * Set {sum} to 0
          * Add {fieldValue} to {sum}
* Return {sum}
