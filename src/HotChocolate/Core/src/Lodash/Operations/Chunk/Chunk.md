### Chunk
**Directive**

```graphql
directive @chunk(size: Int! = 1) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@chunk* can be applied on list values of any depth. 
*@chunk* splits the list into groups the length of `size`. 
If the list cannot be split evenly, the final chunk will contain the remaining elements.

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
  list @chunk(size: 2){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": [
    [
      {
        "string": "a"
      },
      {
        "string": "b"
      }
    ],
    [
      {
        "string": "c"
      }
    ]
  ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteChunk(node):
* Let {size} be the value of the argument `size` of the directive
* If {size} is < 1
  * Assert: *AG0005* 
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise
  * Let {chunks} be an empty list
  * Let {currentChunk} be an empty list
  * While count of {node} is NOT 0
    * Let {element} be the value of the first element of {node}
    * Remove the first element of {node}
    * Add {element} to {currentChunk}
    * If count of {currentChunk} is {size}
      * Add {currentChunk} to {chunks}
      * Let {currentChunk} be an empty list
  * If count of {currentChunk} is NOT 0
    * Add {currentChunk} to {chunks}
  * Retrun {chunks}
