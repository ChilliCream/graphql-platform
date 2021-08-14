### DropRight
**Directive**

```graphql
directive @dropRight(count: Int!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@dropRight* can be applied a list any depth. 
*@dropRight* removes the last `count` elements of the list. 
If there are less elements than `count` a empty list is returned.

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
  list @dropRight(count: 2){
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
    }
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteDropRight(node):
* Let {size} be the value of the argument `size` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Remove {size} elements from the end of {node}
  * Return {node}
