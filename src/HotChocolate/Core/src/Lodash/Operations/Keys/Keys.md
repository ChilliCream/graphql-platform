### Keys
**Directive**

```graphql
directive @keys repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@keys* can only be to objects. 
*@keys* returns a list of all fields in this object. 

Given the following data:

```json example
{
  "single": {
    "id": 1,
    "string": "a"
  }
}
```

The execution of the following query:

```graphql example
{
  single @keys {
    id
    string
  }
}
```

will retrun the following result:

```json example
{
  "single": [ 
    "id", 
    "string", 
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteKeys(node):
* If {node} is *ListValue*
  * Assert: *AG0003*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0002*
* Otherwise 
  * Let {result} be a empty list
  * For each {key} in keys of {value}
    * Add {key} to {result}
  * Return {result}
