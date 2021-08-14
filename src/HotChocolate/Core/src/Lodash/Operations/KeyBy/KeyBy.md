### KeyBy
**Directive**

```graphql
directive @keyBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@keyBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@keyBy* returns an object where the keys are the values of the field selected with `key`. 
The values are the first element with the corresponding key.

Given the following data:

```json example
{
  "list": [
    {
      "id": 1,
      "string": "a"
    },
    {
      "id": 2,
      "string": "a"
    },
    {
      "id": 3,
      "string": "b"
    }
  ]
}
```

The execution of the following query:

```graphql example
{
  list @keyBy(key: "string"){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": {
    "a": {
      "id": 1,
      "string": "a"
    },
    "b": {
      "id": 3,
      "string": "b"
    }
  }
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteKeyBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteKeyByArray(node)}

RewriteKeyByArray(value):
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
          * Set value of {convertedValue} in {result} to {element}
* Return {result}
