### Flatten
**Directive**

```graphql
directive @flatten(depth: Int! = 1) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@flatten* can be applied to all value nodes. 
*@flatten* recursivly flattens an array `depth` times. 

Note: If *@flatten* is applied to a *ObjectValue* or a *ScalarValue*, the value will be wrapped in 
an array with length 1

Given the following data:

```json example
{
  "nestedList": [
    [
      {
        "string": "b"
      }
    ],
    [
      {
        "string": "d"
      }
    ],
    [
      {
        "string": "d"
      }
    ],
  ]
}
```

The execution of the following query:

```graphql example
{
  nestedList @flatten(depth: 1){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": [
    {
      "string": "b"
    },
    {
      "string": "d"
    } ,
    {
      "string": "d"
    }
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteFlatten(node):
* Let {depth} be the value of the argument `depth` of the directive
* If {dpeth} is lower than 1
  * Assert: *AG0006*
* Otherwise 
  * Let {result} be a empty list
  * Let {initialDepth} be 0
  * {RewriteFlattenList(node, initialDepth, result)}
  * Return {result}
* Otherwise 
  * Assert: *AG0002*


RewriteFlattenList(node, currentDepth, result):
* If {node} is *ListValue*
  * For each {element} in {node}
    * Let {nextDepth} be {currentDepth} + 1
    * {RewriteFlattenList(node, nextDepth, result)}
* Otherwise 
  * Add {node} to {result}
