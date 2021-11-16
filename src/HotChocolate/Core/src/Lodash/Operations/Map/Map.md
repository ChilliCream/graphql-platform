### Map
**Directive**

```graphql
directive @map(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@map* can be applied to objects or list of objects of any depth. 
If *@map* is applied on an object, it pulls the field `key` up.  
If *@map* is applied on a list, the algorithm is applied for each element of this list.

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
  list @map(key: "string"){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": [
    "a",
    "b",
    "c"
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteMap(node):
* Let {key} be the value of the argument `key` of the directive
* If {node} is *ObjectValue*
  * If {Exists(node, key)}
    * Let {valueOfKey} be {Get(node, key)}
    * Retrun {valueOfKey}
  * Otherwise
    * Return {null}
* Otherwise If {node} is *ListValue*
  * Let {listResult} be a empty list
  * For each {element} in {node}
    * Let {rewritten} be the result of {RewriteMap(element)}
    * If {rewritten} is NOT {null}
      * Add {rewritten} to {listResult}
  * Return {listResult}
* Otherwise 
  * Assert: *AG0002*

