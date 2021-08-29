### Unique
**Directive**

```graphql
directive @unique(by: String) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@unique* returns a duplicate-free version of the list based on the the values of the field selected with `by`.
*@unique* can be applied to a list of objects of depth 1 if a `by` is specified.
If no `by` is specified, unqiue can be applied to list of scalars.

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
  list @unique(by: "string"){
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

RewriteUnique(node):
* Let {by} be the value of the argument `by` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * If {by} is not specified
    * Return {RewriteUniqueArray(node)}
  * Otherwise
    * Return {RewriteUniqueArrayBy(node, by)}

RewriteUniqueqArray(value):
* Let {result} be a ordered set
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0007*
  * Otherwise If {element} is *ObjectValue*
    * Assert: *AG0008*
  * Otherwise If {element} is {null}
    * If {null} does NOT exist in {result}
      * Add {null} to {result}
  * Otherwise 
    * If {IsConvertibleToComparable(element)}
      * Let {convertedValue} be {ConvertToComparable(fieldValue)}
      * If {convertedValue} does NOT exist in {result}
        * Add {convertedValue} to {result}
* Return {result}

RewriteUniqueArrayBy(value, by):
* Let {result} be a empty list 
* Let {values} be a ordered set
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is {null}
    * If {null} does NOT exist in {result}
      * Add {null} to {result}
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, by)} 
      * Let {fieldValue} be {Get(element, by)}
      * If {IsConvertibleToComparable(element)}
        * Let {convertedValue} be {ConvertToComparable(fieldValue)}
        * If {convertedValue} does NOT exist in {values}
          * Add {element} to {result}
  * Otherwise
    * Add {element} to {result}
* Return {result}
