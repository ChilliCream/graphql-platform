# GraphQL Aggregation Directives
## Operations
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
* If {node} is {ObjectValue}
  * If Exists(node, key)
    * Let {valueOfKey} be Get(node, key)
    * Retrun {valueOfKey}
  * Otherwise
    * Return {null}
* Otherwise If {node} is {ListValue}
  * Let {listResult} be a empty list
  * For each {element} in {node}
    * Let {rewritten} be the result of {Rewrite(element)}
    * If {rewritten} is NOT {null}
      * Add {rewritten} to {listResult}
  * Return {listResult}
* Otherwise 
  * Assert: {AG0002}

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
  * Assert: {AG0005} 
* If {node} is {ObjectValue}
  * Assert: {AG0001}
* If {node} is {ScalarValue}
  * Assert: {AG0004}
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

### CountBy
**Directive**

```graphql
directive @countBy(key: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@countBy* can only be applied list of objects of depth 1. 
Nested lists can be flattened with *@flatten* first.
*@countBy* returns an object where the keys are the values of the field selected with `key`. 
The values are the number of times the key was found.

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
  list @countBy(key: "string"){
    string
  }
}
```

will retrun the following result:

```json example
{
  "list": {
    "a": 2,
    "c": 1
  }
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteCountBy(node):
* Let {node} be the value node where the directive is applied
* Let {key} be the value of the argument `key` of the directive
* If {node} is {ObjectValue}
  * Assert: {AG0001}
* Otherwise If {node} is {ScalarValue}
  * Assert: {AG0004}
* Otherwise 
  * Return {RewriteCountByArray(node)}

RewriteCountByArray(value):
* Let {result} be a unordered map
* For each {element} in {value}
  * If {element} is {ListValue}
    * Assert: {AG0003}
  * Otherwise If {element} is {ScalarValue}
    * Assert: {AG0002}
  * Otherwise If {element} is {ObjectValue}
    * If {Exists(element, key)} 
      * Let {fieldValue} be {Get(element, key)}
      * If {IsConvertibleToString(fieldValue)}
        * Let {convertedValue} be {ConvertToString(fieldValue)}
        * If NOT Exists(result, convertedValue)
          * Set value of {convertedValue} in {result} to 0
        * Increase {convertedValue} in {result} by 1
* Return {result}

## Transformations
Exists(node, key):
  * If {node} is {ObjectValue} AND field {key} exists in {node}
    * return {true}
  * Otherwise
    * return {false}

Get(node, key):
  * If {Exists(node, key)}
    * return value of field {key} in {node}
  * Otherwise
    * Assert: Field {key} is not present in {node}

ConvertToString(node):
  * If {node} is {string}
    * return {node}
  * Otherwise If {node} is {number}
    * return {number} as string
  * Otherwise If {node} is {boolean}
    * return {boolean} as string
  * Otherwise If {node} is {null}
    * return `"null"`
  * Otherwise  
    * return {null}

IsConvertibleToString(node):
  * If {node} is {string}
    * return {true}
  * Otherwise If {node} is {number}
    * return {true}
  * Otherwise If {node} is {boolean}
    * return {true}
  * Otherwise If {node} is {null}
    * return {true}
  * Otherwise  
    * return {false}



## Definitions
ObjectValue
: Key Value pair

```json example
{
   "string": "string",
   "int": 1,
   "float": 2,
}
```

ScalarValue
: A primitive leaf value

```json example
"string"
```
```json example
1
```

ListValue
: A collection of either  *ObjectValue*, *ScalarValue* or *ListValue*

```json example
[ 
  {
   "string": "string",
  },
  1,
  [
    {
     "string": "string",
    },
  ]
]
```


## Errors 
AG0001 
: The field *path* expects a list but received an object

AG0002 
: The field *path* expects a object but received a scalar

AG0003 
: The field *path* expects a object but received an list

AG0004 
: The field *path* expects a list but received a scalar

AG0005 
: The argument size of chunk on field *path* must be greater than 0
