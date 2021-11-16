### TakeRight
**Directive**

```graphql
directive @takeRight(count: Int!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@takeRight* can be applied a list any depth. 
*@takeRight* returns the last `count` elements of the list. 
If there are less elements than `count` the list does not change. 

Given the following data:

```json example
{
  "list": [
    {
      "string": "a"
    },
    {
      "string": "b"
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
  list @takeRight(count: 2){
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
      "string": "c"
    }
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteTakeRight(node):
* Let {size} be the value of the argument `size` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Let {result} be the a list of the first {size} elements from the end of {node}
  * Return {node}
