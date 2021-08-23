### UniqBy
**Directive**

```graphql
directive @uniqBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@uniqBy* can be applied to a list of objects of depth 1.
*@uniqBy* returns a duplicate-free version of the list based on the the values of the field selected with `key`. 

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
  list @uniqBy(key: "string"){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": {
    {
      "string": "a"
    },
    {
      "string": "c"
    }
  }
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteUniqBy(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteUniqByArray(node)}

RewriteUniqByArray(value):
* Let {result} be a empty list 
* Let {values} be a ordered set
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, key)} 
      * Let {fieldValue} be {Get(element, key)}
      * If {IsConvertibleToComparable(element)}
        * Let {convertedValue} be {ConvertToComparable(fieldValue)}
        * If {convertedValue} does NOT exist in {values}
          * Add {element} to {result}
  * Otherwise
    * Add {element} to {result}
* Return {result}
