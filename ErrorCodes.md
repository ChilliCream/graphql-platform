| Code   | Category          | Description                                                                                                                              |
| ------ | ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| HC0001 | Scalars           | The runtime type is not supported by the scalars ParseValue method or has an invalid value.                                              |
| HC0002 | Scalars           | Either the syntax node is invalid when parsing the literal or the syntax node value has an invalid format.                               |
| HC0003 | Apollo Federation | The key attribute is used on the type level without specifying the fieldset.                                                             |
| HC0004 | Apollo Federation | The provides attribute is used and the fieldset is set to `null` or `string.Empty`.                                                      |
| HC0005 | Apollo Federation | The requires attribute is used and the fieldset is set to `null` or `string.Empty`.                                                      |
| HC0006 | Schema Stitching  | The HTTP request failed.                                                                                                                 |
| HC0007 | Schema Stitching  | Unknown error happened while fetching from a downstream service.                                                                         |
| HC0008 | Execution         | An unexpected error happened during execution task processing.                                                                           |
| HC0009 | Server            | The GraphQL request structure is invalid.                                                                                                |
| HC0010 | Server            | The request is larger then maximum allowed request size.                                                                                 |
| HC0011 | Server            | The GraphQL request has syntax errors.                                                                                                   |
| HC0012 | Server            | Unexpected request parser error.                                                                                                         |
| HC0013 | Server            | The query and the id is missing from the GraphQL request.                                                                                |
| HC0014 | Execution         | The GraphQL document has syntax errors.                                                                                                  |
| HC0015 | Execution         | No query document was provided and the provided query id is unknown.                                                                     |
| HC0016 | Execution         | Variable `xyz` got an invalid value.                                                                                                     |
| HC0017 | Execution         | Variable `xyz` is not an input type.                                                                                                     |
| HC0018 | Execution         | Variable `xyz` is required.                                                                                                              |
| HC0019 | Execution         | Unable to create an instance for the operation type (initial value).                                                                     |
| HC0020 | Execution         | A persisted query was not found when using the active persisted query pipeline.                                                          |
| HC0021 | Data              | List are not supported in sorting                                                                                                        |
| HC0022 | Data              | The requested list contained more than one element                                                                                       |
| HC0023 | Data              | Filtering could not be projected                                                                                                         |
| HC0024 | Data              | Sorting could not be projected                                                                                                           |
| HC0025 | Data              | No paging provider for the source was found                                                                                              |
| HC0026 | Data              | The requested field does not support null values                                                                                         |
| HC0028 | Data              | Type does not contain a valid node field. Only `items` and `nodes` are supported                                                         |
| HC0029 | Spatial           | The coordinate reference system is not supported by this server                                                                          |
| HC0030 | Spatial           | Coordinates with M values cannot be reprojected                                                                                          |
| HC0030 | Spatial           | Coordinates with M values cannot be reprojected                                                                                          |
| HC0031 | Paging            | Unable to infer the element type from the current resolver. This often happens if the resolver is not an iterable type like IEnumerable, IQueryable, IList etc. Ensure that you either explicitly specify the element type or that the return type of your resolver is an iterable type.                                                                                                                                      |
| HC0032 | Paging            | The element schema type for pagination must be a valid GraphQL output type (ObjectType, InterfaceType, UnionType, EnumType, ScalarType). |
| HC0033 | Server            | At least an 'operations' field and a 'map' field need to be present.                                                                     |
| HC0034 | Server            | No 'operations' specified.                                                                                                               |
| HC0035 | Server            | Misordered multipart fields; 'map' should follow 'operations'.                                                                           |
| HC0036 | Server            | Invalid JSON in the ‘map’ multipart field; Expected type of Dictionary<string, string[]>.                                                |
| HC0037 | Server            | No object paths specified for a key in the 'map'.                                                                                        |
| HC0038 | Server            | A key is referring to a file that was not provided.                                                                                      |
| HC0039 | Server            | The variable path is referring to a variable that does not exist.                                                                        |
| HC0040 | Server            | The variable structure is invalid.                                                                                                       |
| HC0041 | Server            | Invalid variable path in `map`.                                                                                                          |
| HC0042 | Server            | The variable path must start with `variables`.                                                                                           |
| HC0043 | Server            | Invalid JSON in the `map` multipart field; Expected type of Dictionary<string, string[]>.                                                |
| HC0044 | Server            | No `map` specified.                                                                                                                      |