### GroupBy
**Directive**

```graphql
directive @groupBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@groupBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@groupBy* returns an object where the keys are the values of the field selected with `key`. 
The values of the object are lists with the corresponding elements.

Given the following data:

```json example
{
  "list": [
    {
      "string": "a"
    },
    {
      "string": "a"
    },
    {
      "string": "b"
    }
  ]
}
```

The execution of the following query:

```graphql example
{
  list @groupBy(key: "string"){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": {
    "a": [
      {
        "string": "a"
      },
      {
        "string": "a"
      }
    ],
    "b": [
      {
        "string": "b"
      }
    ]
  }
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteGroupBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteGroupByArray(node)}

RewriteGroupByArray(value):
* Let {result} be a unordered map
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, key)} 
      * Let {fieldValue} be {Get(element, key)}
      * If {IsConvertibleToString(fieldValue)}
        * Let {convertedValue} be {ConvertToString(fieldValue)}
        * If NOT {Exists(result, convertedValue)}
          * Set value of {convertedValue} in {result} to a empty list
        * Add {element} to the list of {convertedValue} in {result}
* Return {result}
