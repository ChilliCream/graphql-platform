### MeanBy
**Directive**

```graphql
directive @meanBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@meanBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@meanBy* returns mean of all values of the fields with name `key`.
If no value was found, *@meanBy* retruns null.

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
  list @meanBy(key: "int"){
    int
  }
}
```

will retrun the following result:

```json example
{
  "list": 2
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteMeanBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteMeanByArray(node)}

RewriteMeanByArray(value):
* Let {divisor} be 0
* Let {sum} be 0
* For Each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, key)} 
      * Let {fieldValue} be {Get(element, key)}
      * If {IsNumber(fieldValue)}
          * Add {fieldValue} to {sum}
          * Increase {divisor} by 1
* If {divisor} is 0
  * Return {null}
* Return {sum} / {divisor}
