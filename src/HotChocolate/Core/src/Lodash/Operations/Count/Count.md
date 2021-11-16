### Count
**Directive**

```graphql
directive @count(by: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@count* counts the amount of items in a list.
If `by` is specified and the directive is applied to a list of objects of depth 1, it returns an object where the 
keys are the values of the field selected with `by`. The values are the number of times the by was found.
Nested lists can be flattened with *@flatten* first.

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
  ],
  "intList": [1, 2, 3]
}
```

The execution of the following query:

```graphql example
{
  list @count(by: "string"){
    string
  }
  intList @count
}
```

will retrun the following result:

```json example
{
  "list": {
    "a": 2,
    "c": 1
  },
  "intList": 3
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteCount(node):
* Let {by} be the value of the argument `by` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * If {by} is NOT specified
    * Return size of {node}
  * Otherwise
    * Return {RewriteCountArrayBy(node, by)}

RewriteCountArrayBy(value):
* Let {result} be a unordered map
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, by)} 
      * Let {fieldValue} be {Get(element, by)}
      * If {IsConvertibleToString(fieldValue)}
        * Let {convertedValue} be {ConvertToString(fieldValue)}
        * If NOT {Exists(result, convertedValue)}
          * Set value of {convertedValue} in {result} to 0
        * Increase {convertedValue} in {result} by 1
* Return {result}
