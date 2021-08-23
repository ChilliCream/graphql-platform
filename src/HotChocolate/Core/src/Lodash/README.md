# GraphQL Aggregation Directives
## Operations
### [Map](Operations/Map/Map.md)
### [Chunk](Operations/Chunk/Chunk.md)
### [CountBy](Operations/CountBy/CountBy.md)
### [Drop](Operations/Drop/Drop.md)
### [DropRight](Operations/Drop/DropRight.md)
### [Flatten](Operations/Flatten/Flatten.md)
### [GroupBy](Operations/GroupBy/GroupBy.md)
### [KeyBy](Operations/KeyBy/KeyBy.md)
### [Keys](Operations/Keys/Keys.md)
### [MaxBy](Operations/MaxBy/MaxBy.md)
### [MeanBy](Operations/MeanBy/MeanBy.md)
### [MinBy](Operations/MinBy/MinBy.md)

## Transformations
Exists(node, key):
  * If {node} is *ObjectValue* AND field {key} exists in {node}
    * return {true}
  * Otherwise
    * return {false}

Get(node, key):
  * If {Exists(node, key)}
    * return value of field {key} in {node}
  * Otherwise
    * Assert: Field {key} is not present in {node}

ConvertToString(node):
  * If {node} is *string*
    * return {node}
  * Otherwise If {node} is *number*
    * return {number} as string
  * Otherwise If {node} is *boolean*
    * return {boolean} as string
  * Otherwise If {node} is {null}
    * return `"null"`
  * Otherwise  
    * return {null}

IsConvertibleToString(node):
  * If {node} is *string*
    * return {true}
  * Otherwise If {node} is *number*
    * return {true}
  * Otherwise If {node} is *boolean*
    * return {true}
  * Otherwise If {node} is *null*
    * return {true}
  * Otherwise  
    * return {false}

ConvertToComparable(node):
  * If {node} is *number*
    * return {number} 
  * Otherwise If {node} is *boolean*
    * return {boolean} 
  * Otherwise  
    * return {null}

NOTE: TODO We might want to add support for datetime strings

IsConvertibleToComparable(node):
  * If {node} is *number*
    * return {true}
  * Otherwise If {node} is *boolean*
    * return {true}
  * Otherwise  
    * return {false}

IsNumber(node):
  * If {node} is *number*
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

AG0006
: The argument size of flatten on field *path* must be greater than 0,
