| Code   | Category          | Description                                                                                                |
| ------ | ----------------- | ---------------------------------------------------------------------------------------------------------- |
| HC0001 | Scalars           | The runtime type is not supported by the scalars ParseValue method.                                        |
| HC0002 | Scalars           | Either the syntax node is invalid when parsing the literal or the syntax node value has an invalid format. |
| HC0003 | Apollo Federation | The key attribute is used on the type level without specifying the fieldset.                               |
| HC0004 | Apollo Federation | The provides attribute is used and the fieldset is set to `null` or `string.Empty`.                        |
| HC0005 | Apollo Federation | The requires attribute is used and the fieldset is set to `null` or `string.Empty`.                        |
| HC0006 | Schema Stitching  | The HTTP request failed.                                                                                   |
| HC0007 | Schema Stitching  | Unknown error happened while fetching from a downstream service.                                           |
| HC0008 | Execution         | An unexpected error happened during execution task processing.                                             |
| HC0009 | Server            | The GraphQL request structure is invalid.                                                                  |
| HC0010 | Server            | The request is larger then maximum allowed request size.                                                   |
| HC0011 | Server            | The GraphQL request has syntax errors.                                                                     |
| HC0012 | Server            | Unexpected request parser error.                                                                           |
| HC0013 | Server            | The query and the id is missing from the GraphQL request.                                                  |
| HC0014 | Execution         | The GraphQL document has syntax errors.                                                                    |
| HC0015 | Execution         | No query document was provided and the provided query id is unknown.                                       |
| HC0016 | Execution         | Variable `xyz` got an invalid value.                                                                       |
| HC0017 | Execution         | Variable `xyz` is not an input type.                                                                       |
| HC0018 | Execution         | Variable `xyz` is required.                                                                                |
| HC0019 | Execution         | Unable to create an instance for the operation type (initial value).                                       |
| HC0020 | Execution         | A persisted query was not found when using the active persisted query pipeline.                            |
| HC0021 | Data              | List are not supported in sorting                                                                          |
| HC0022 | Data              | The requested list contained more than one element                                                         |
| HC0023 | Data              | Filtering could not be projected                                                                           |
| HC0024 | Data              | Sorting could not be projected                                                                             |
| HC0025 | Data              | No paging provider for the source was found                                                                |
| HC0026 | Data              | The requested field does not support null values                                                           |
