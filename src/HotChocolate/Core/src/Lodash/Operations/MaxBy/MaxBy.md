### MaxBy
**Directive**

```graphql
directive @maxBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@maxBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@maxBy* returns the object where the value of the field with name `key` is the highest.
If no value was found, *@maxBy* retruns the first element of the list.

Given the following data:

```json example
{
  "list": [
    {
      "string": "a",
      "int": 1
    },
    {
      "string": "b",
      "int": 2
    },
    {
      "string": "c",
      "int": 3
    }
  ]
}
```

The execution of the following query:

```graphql example
{
  list @maxBy(key: "int"){
    string
    int
  }
}
```

will retrun the following result:

```json example
{
  "list": {
    "string": "c",
    "int": 3
  }
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteMaxBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteMaxByArray(node)}

RewriteMaxByArray(value):
* If {value} has 0 elements
  * Return {null}
* Let {lastValue} be a {null}
* Let {result} be a {null}
* If the first element of {value} is *ObjectValue*
  * Set {result} to the first element of {value}
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, key)} 
      * Let {fieldValue} be {Get(element, key)}
      * If {IsConvertibleToComparable(fieldValue)}
        * Let {convertedValue} be {ConvertToComparable(fieldValue)}
        * If {result} is {null} OR {lastValue} is {null} OR {convertedValue} is greater than {lastValue}
          * Set value of {result} to {element}
          * Set value of {lastValue} to {convertedValue}
* Return {result}
