### Take
**Directive**

```graphql
directive @take(count: Int!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@take* can be applied a list any depth. 
*@take* returns the first `count` elements of the list. 
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
  list @take(count: 2){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": [
    {
      "string": "a"
    },
    {
      "string": "b"
    },
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteTake(node):
* Let {size} be the value of the argument `size` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Let {result} be the a list of the first {size} elements from the beginning of {node}
  * Return {result}
