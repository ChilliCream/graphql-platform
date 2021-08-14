### CountBy
**Directive**

```graphql
directive @countBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@countBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@countBy* returns an object where the keys are the values of the field selected with `key`. 
The values are the number of times the key was found.

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
      "string": "c"
    }
  ]
}
```

The execution of the following query:

```graphql example
{
  list @countBy(key: "string"){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": {
    "a": 2,
    "c": 1
  }
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteCountBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteCountByArray(node)}

RewriteCountByArray(value):
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
          * Set value of {convertedValue} in {result} to 0
        * Increase {convertedValue} in {result} by 1
* Return {result}
