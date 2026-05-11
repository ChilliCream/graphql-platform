---
title: Error codes reference
---

Hot Chocolate uses error codes as the machine-readable component of errors. When a code is present in a GraphQL response, it appears as `errors[].extensions.code`. On the server, you can access the same value through `IError.Code`.

Use error codes for diagnostics, support runbooks, and client-side branching. Avoid branching on the `message` text, as messages may change, be redacted, or replaced by error filters.

This reference documents source-verified Hot Chocolate server codes. It does not cover application-specific codes, Strawberry Shake, Nitro, Mocha, or placeholder constants.

```json
{
  "errors": [
    {
      "message": "PersistedQueryNotFound",
      "extensions": {
        "code": "HC0020"
      }
    }
  ]
}
```

# Code boundaries

| Boundary            | Meaning                                                                                            | Client guidance                                                                                       |
| ------------------- | -------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| Public response     | The code can appear in a GraphQL response or operation result.                                     | Safe to use for troubleshooting. Branch on it only when your client contract documents that behavior. |
| Startup diagnostic  | The code can appear while building or validating the schema.                                       | Use it in tests, CI output, and runbooks. It is not normally returned from an operation.              |
| Internal diagnostic | The code is used by schema validation logging or diagnostics.                                      | Treat it as diagnostic output, not as a response contract.                                            |
| Framework constant  | The code is source-verified, but the exact response boundary depends on the feature that emits it. | Refer to feature documentation and tests before making it part of a client contract.                  |

Some GraphQL validation errors do not have a Hot Chocolate code. These errors may still include standard GraphQL fields such as `message`, `locations`, `path`, or extensions like `coordinate`, `field`, and `specifiedBy`.

# Finding an error code

The following table lists the built-in server code families, verified from `HotChocolate.ErrorCodes` and schema validation log constants. Placeholder constants are intentionally omitted.

| Code                                   | Category                | Phase                      | Boundary            | Meaning                                                                 | Start here                                                      |
| -------------------------------------- | ----------------------- | -------------------------- | ------------------- | ----------------------------------------------------------------------- | --------------------------------------------------------------- |
| `AUTH_NOT_AUTHENTICATED`               | Authorization           | Authorization              | Public response     | The current user is anonymous for a protected resource.                 | [Authorization codes](#authorization-codes)                     |
| `AUTH_NOT_AUTHORIZED`                  | Authorization           | Authorization              | Public response     | The current user is authenticated but not allowed.                      | [Authorization codes](#authorization-codes)                     |
| `AUTH_NO_DEFAULT_POLICY`               | Authorization           | Authorization              | Public response     | Authorization was requested but no default policy exists.               | [Authorization codes](#authorization-codes)                     |
| `AUTH_POLICY_NOT_FOUND`                | Authorization           | Authorization              | Public response     | A named policy could not be found.                                      | [Authorization codes](#authorization-codes)                     |
| `EXEC_BATCH_AUTO_MAP_VAR_TYPE`         | Execution               | Variable coercion          | Framework constant  | A batched variable could not be mapped automatically.                   | [Execution codes](#execution-and-variable-codes)                |
| `EXEC_BATCH_VAR_SERIALIZE`             | Execution               | Variable coercion          | Framework constant  | A batched variable could not be serialized.                             | [Execution codes](#execution-and-variable-codes)                |
| `EXEC_INVALID_LEAF_VALUE`              | Execution               | Result completion          | Framework constant  | A leaf value could not be serialized.                                   | [Execution codes](#execution-and-variable-codes)                |
| `EXEC_LIST_TYPE_NOT_SUPPORTED`         | Execution               | Result completion          | Framework constant  | A list result type is not supported.                                    | [Execution codes](#execution-and-variable-codes)                |
| `EXEC_UNABLE_TO_RESOLVE_ABSTRACT_TYPE` | Execution               | Result completion          | Framework constant  | A runtime value could not be resolved to an abstract GraphQL type.      | [Execution codes](#execution-and-variable-codes)                |
| `FILTER_FIELD_DESCRIPTOR_TYPE`         | Filtering               | Schema building            | Startup diagnostic  | A filter field descriptor type is invalid.                              | [Feature codes](#feature-and-provider-codes)                    |
| `FILTER_OBJECT_TYPE`                   | Filtering               | Schema building            | Startup diagnostic  | A filter object type is invalid.                                        | [Feature codes](#feature-and-provider-codes)                    |
| `HC0001`                               | Scalars                 | Value parsing              | Framework constant  | A scalar received an unsupported runtime type.                          | [Feature codes](#feature-and-provider-codes)                    |
| `HC0002`                               | Scalars                 | Literal parsing            | Framework constant  | A scalar literal has an invalid syntax node or format.                  | [Feature codes](#feature-and-provider-codes)                    |
| `HC0008`                               | Execution               | Execution                  | Framework constant  | A task processing error occurred.                                       | [Execution codes](#execution-and-variable-codes)                |
| `HC0009`                               | HTTP transport          | Request parsing            | Public response     | The request is invalid.                                                 | [HTTP transport codes](#http-transport-codes)                   |
| `HC0010`                               | HTTP transport          | Request parsing            | Public response     | The maximum request size was exceeded.                                  | [HTTP transport codes](#http-transport-codes)                   |
| `HC0011`                               | HTTP transport          | Request parsing            | Public response     | The HTTP request parser found invalid syntax.                           | [HTTP transport codes](#http-transport-codes)                   |
| `HC0012`                               | HTTP transport          | Request parsing            | Public response     | The HTTP request parser failed unexpectedly.                            | [HTTP transport codes](#http-transport-codes)                   |
| `HC0013`                               | HTTP transport          | Request parsing            | Public response     | The request did not contain a query document or persisted operation id. | [HTTP transport codes](#http-transport-codes)                   |
| `HC0014`                               | Document                | Parsing                    | Public response     | The GraphQL document has a syntax error.                                | [Validation and security codes](#validation-and-security-codes) |
| `HC0015`                               | Execution               | Operation lookup           | Framework constant  | An operation document could not be found.                               | [Execution codes](#execution-and-variable-codes)                |
| `HC0016`                               | Execution               | Variable coercion          | Framework constant  | A value has an invalid type.                                            | [Execution codes](#execution-and-variable-codes)                |
| `HC0017`                               | Execution               | Variable coercion          | Framework constant  | A value must be an input type.                                          | [Execution codes](#execution-and-variable-codes)                |
| `HC0018`                               | Execution               | Result completion          | Public response     | A non-null field completed as `null`.                                   | [Execution codes](#execution-and-variable-codes)                |
| `HC0019`                               | Execution               | Root value creation        | Framework constant  | A root value could not be created.                                      | [Execution codes](#execution-and-variable-codes)                |
| `HC0020`                               | Persisted operations    | Persisted operation lookup | Public response     | A persisted operation was not found.                                    | [Persisted operation codes](#persisted-operation-codes)         |
| `HC0021`                               | Data                    | Projection or provider     | Framework constant  | A list shape is not supported by the data provider.                     | [Feature codes](#feature-and-provider-codes)                    |
| `HC0022`                               | Data                    | Projection or provider     | Framework constant  | More than one element was returned where one was expected.              | [Feature codes](#feature-and-provider-codes)                    |
| `HC0023`                               | Data                    | Projection or provider     | Framework constant  | Filtering projection failed.                                            | [Feature codes](#feature-and-provider-codes)                    |
| `HC0024`                               | Data                    | Projection or provider     | Framework constant  | Sorting projection failed.                                              | [Feature codes](#feature-and-provider-codes)                    |
| `HC0025`                               | Data                    | Pagination provider        | Startup diagnostic  | No pagination provider was found.                                       | [Feature codes](#feature-and-provider-codes)                    |
| `HC0026`                               | Data                    | Projection or provider     | Framework constant  | A non-null data value could not be produced.                            | [Feature codes](#feature-and-provider-codes)                    |
| `HC0028`                               | Data                    | Relay node field           | Startup diagnostic  | A type does not contain a valid `items` or `nodes` field.               | [Feature codes](#feature-and-provider-codes)                    |
| `HC0029`                               | Spatial                 | Spatial conversion         | Framework constant  | The coordinate reference system is not supported.                       | [Feature codes](#feature-and-provider-codes)                    |
| `HC0030`                               | Spatial                 | Spatial conversion         | Framework constant  | Coordinates with M values cannot be reprojected.                        | [Feature codes](#feature-and-provider-codes)                    |
| `HC0031`                               | Paging                  | Schema building            | Startup diagnostic  | The pagination element type could not be inferred.                      | [Feature codes](#feature-and-provider-codes)                    |
| `HC0032`                               | Paging                  | Schema building            | Startup diagnostic  | The pagination schema type is not a valid output type.                  | [Feature codes](#feature-and-provider-codes)                    |
| `HC0033`                               | Multipart upload        | Request parsing            | Public response     | The multipart form could not be read.                                   | [HTTP transport codes](#http-transport-codes)                   |
| `HC0034`                               | Multipart upload        | Request parsing            | Public response     | The multipart request has no `operations` field.                        | [HTTP transport codes](#http-transport-codes)                   |
| `HC0035`                               | Multipart upload        | Request parsing            | Public response     | Multipart fields are in the wrong order.                                | [HTTP transport codes](#http-transport-codes)                   |
| `HC0037`                               | Multipart upload        | Request parsing            | Public response     | A `map` entry has no object paths.                                      | [HTTP transport codes](#http-transport-codes)                   |
| `HC0038`                               | Multipart upload        | Request parsing            | Public response     | A `map` entry refers to a missing file.                                 | [HTTP transport codes](#http-transport-codes)                   |
| `HC0039`                               | Multipart upload        | Request parsing            | Public response     | A variable path refers to a missing variable.                           | [HTTP transport codes](#http-transport-codes)                   |
| `HC0040`                               | Multipart upload        | Request parsing            | Public response     | The variable structure referenced by `map` is invalid.                  | [HTTP transport codes](#http-transport-codes)                   |
| `HC0041`                               | Multipart upload        | Request parsing            | Public response     | A path in `map` is invalid.                                             | [HTTP transport codes](#http-transport-codes)                   |
| `HC0042`                               | Multipart upload        | Request parsing            | Public response     | A path in `map` does not start with `variables`.                        | [HTTP transport codes](#http-transport-codes)                   |
| `HC0043`                               | Multipart upload        | Request parsing            | Public response     | The `map` field is not valid JSON for the expected shape.               | [HTTP transport codes](#http-transport-codes)                   |
| `HC0044`                               | Multipart upload        | Request parsing            | Public response     | The multipart request has no `map` field.                               | [HTTP transport codes](#http-transport-codes)                   |
| `HC0045`                               | Execution               | Execution timeout          | Public response     | The request exceeded the configured timeout.                            | [Execution codes](#execution-and-variable-codes)                |
| `HC0046`                               | Validation and security | Validation                 | Public response     | Introspection is not allowed for the current request.                   | [Validation and security codes](#validation-and-security-codes) |
| `HC0047`                               | Cost analysis           | Validation                 | Public response     | The configured operation cost limit was exceeded.                       | [Cost analysis codes](#cost-analysis-codes)                     |
| `HC0048`                               | Cost analysis           | Validation                 | Framework constant  | The cost analyzer state is incomplete.                                  | [Cost analysis codes](#cost-analysis-codes)                     |
| `HC0049`                               | Execution               | Cancellation               | Public response     | The request was canceled.                                               | [Execution codes](#execution-and-variable-codes)                |
| `HC0050`                               | Schema                  | Schema building            | Startup diagnostic  | Field middleware is in an invalid order.                                | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0051`                               | Paging                  | Execution                  | Public response     | The requested page size exceeded the maximum.                           | [Feature codes](#feature-and-provider-codes)                    |
| `HC0052`                               | Paging                  | Execution                  | Public response     | Paging boundaries are missing.                                          | [Feature codes](#feature-and-provider-codes)                    |
| `HC0053`                               | Execution               | Resolver invocation        | Framework constant  | The parent value cannot be cast to the resolver type.                   | [Execution codes](#execution-and-variable-codes)                |
| `HC0054`                               | OneOf input             | Variable coercion          | Public response     | A OneOf input object has no field set.                                  | [Execution codes](#execution-and-variable-codes)                |
| `HC0055`                               | OneOf input             | Variable coercion          | Public response     | A OneOf input object has more than one field set.                       | [Execution codes](#execution-and-variable-codes)                |
| `HC0056`                               | OneOf input             | Variable coercion          | Public response     | A OneOf input object field is set to `null`.                            | [Execution codes](#execution-and-variable-codes)                |
| `HC0057`                               | OneOf input             | Variable coercion          | Public response     | A OneOf input object field must be non-null.                            | [Execution codes](#execution-and-variable-codes)                |
| `HC0058`                               | SDL endpoint            | Request parsing            | Public response     | The `type` parameter is empty.                                          | [HTTP transport codes](#http-transport-codes)                   |
| `HC0059`                               | SDL endpoint            | Request parsing            | Public response     | The requested SDL type name is invalid.                                 | [HTTP transport codes](#http-transport-codes)                   |
| `HC0060`                               | SDL endpoint            | Request parsing            | Public response     | The requested SDL type does not exist.                                  | [HTTP transport codes](#http-transport-codes)                   |
| `HC0061`                               | Types                   | Schema building            | Startup diagnostic  | A schema type name is reserved.                                         | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0063`                               | HTTP transport          | Content negotiation        | Public response     | No supported accept media type was requested.                           | [HTTP transport codes](#http-transport-codes)                   |
| `HC0064`                               | HTTP transport          | Content negotiation        | Public response     | The `Accept` header value is invalid.                                   | [HTTP transport codes](#http-transport-codes)                   |
| `HC0065`                               | Schema                  | Schema building            | Startup diagnostic  | A schema type name is duplicated.                                       | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0066`                               | Mutation conventions    | Schema building            | Startup diagnostic  | A mutation error type name is duplicated.                               | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0067`                               | Persisted operations    | Request validation         | Public response     | Only persisted operations are allowed.                                  | [Persisted operation codes](#persisted-operation-codes)         |
| `HC0068`                               | Relay                   | Schema building            | Startup diagnostic  | A node type does not provide a node resolver.                           | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0069`                               | Mutation conventions    | Schema building            | Startup diagnostic  | A mutation payload type is not an object type.                          | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0070`                               | Mutation conventions    | Schema building            | Startup diagnostic  | The `@mutation` directive is on an invalid location.                    | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0071`                               | Schema directives       | Schema building            | Startup diagnostic  | A schema building directive argument has an unexpected value.           | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0072`                               | Schema directives       | Schema building            | Startup diagnostic  | A specified directive argument does not exist.                          | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0073`                               | Schema                  | Schema building            | Startup diagnostic  | Type system members cannot be used as runtime types.                    | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0076`                               | Execution               | Node lookup                | Public response     | Too many nodes were requested at once.                                  | [Execution codes](#execution-and-variable-codes)                |
| `HC0077`                               | Multipart upload        | Request parsing            | Public response     | The request is missing the GraphQL preflight header.                    | [HTTP transport codes](#http-transport-codes)                   |
| `HC0078`                               | Paging                  | Execution                  | Public response     | The cursor format is invalid.                                           | [Feature codes](#feature-and-provider-codes)                    |
| `HC0079`                               | Paging                  | Execution                  | Public response     | The requested page size is below the minimum.                           | [Feature codes](#feature-and-provider-codes)                    |
| `HC0082`                               | Cost analysis           | Validation                 | Public response     | A list field requires exactly one slicing argument.                     | [Cost analysis codes](#cost-analysis-codes)                     |
| `HC0086`                               | Validation and security | Validation                 | Public response     | The maximum introspection depth was exceeded.                           | [Validation and security codes](#validation-and-security-codes) |
| `HC0087`                               | Validation and security | Validation                 | Public response     | The maximum coordinate cycle depth was exceeded.                        | [Validation and security codes](#validation-and-security-codes) |
| `HC0089`                               | Mutation conventions    | Schema building            | Startup diagnostic  | A mutation field must return a value.                                   | [Schema and startup codes](#schema-and-startup-codes)           |
| `HC0090`                               | Paging                  | Execution                  | Public response     | A required `first` value is missing.                                    | [Feature codes](#feature-and-provider-codes)                    |
| `HC0107`                               | Validation and security | Validation                 | Public response     | The field merge validation budget was exhausted.                        | [Validation and security codes](#validation-and-security-codes) |
| `HCV0001` to `HCV0026`                 | Schema validation       | Startup validation         | Internal diagnostic | Schema validation log entries.                                          | [Schema validation diagnostics](#schema-validation-diagnostics) |
| `SCHEMA_INTERFACE_NO_IMPL`             | Schema                  | Schema building            | Startup diagnostic  | An object type does not implement an interface correctly.               | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_INVALID_ARG`                       | Schema                  | Schema building            | Startup diagnostic  | A schema argument is invalid.                                           | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_MISSING_TYPE`                      | Schema                  | Schema building            | Startup diagnostic  | A referenced schema type is missing.                                    | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_NO_ENUM_VALUES`                    | Schema                  | Schema building            | Startup diagnostic  | An enum type has no values.                                             | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_NO_FIELD_RESOLVER`                 | Schema                  | Schema building            | Startup diagnostic  | A field has no resolver.                                                | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_NO_FIELD_TYPE`                     | Schema                  | Schema building            | Startup diagnostic  | A field has no type.                                                    | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_NO_NAME_DEFINED`                   | Schema                  | Schema building            | Startup diagnostic  | A type system member has no name.                                       | [Schema and startup codes](#schema-and-startup-codes)           |
| `TS_UNRESOLVED_TYPES`                  | Schema                  | Schema building            | Startup diagnostic  | One or more schema types could not be resolved.                         | [Schema and startup codes](#schema-and-startup-codes)           |

# HTTP transport codes

These codes are produced before normal operation execution. They usually point to an invalid HTTP request, malformed GraphQL-over-HTTP payload, upload protocol issue, SDL endpoint issue, or content negotiation failure.

## Request parsing

| Code     | Meaning                                                | What to check                                                                        |
| -------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------ |
| `HC0009` | Request invalid.                                       | Confirm the request method, URL, body shape, and GraphQL-over-HTTP format.           |
| `HC0010` | Maximum request size exceeded.                         | Reduce request body size or change the configured request size limit.                |
| `HC0011` | HTTP request parser syntax error.                      | Check JSON syntax, query string encoding, and content type.                          |
| `HC0012` | Unexpected request parser error.                       | Inspect server logs because this is not a normal validation failure.                 |
| `HC0013` | Query document and persisted operation id are missing. | Send a `query`, a stored operation id, or a supported persisted operation extension. |
| `HC0063` | No supported accept media type.                        | Send an `Accept` value supported by the GraphQL endpoint.                            |
| `HC0064` | Invalid `Accept` header value.                         | Fix malformed media type syntax.                                                     |

See [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport) for endpoint behavior and status-code customization.

## Multipart upload

| Code     | Meaning                                      | What to check                                                              |
| -------- | -------------------------------------------- | -------------------------------------------------------------------------- |
| `HC0033` | Multipart form could not be read.            | Check the `multipart/form-data` body and boundary.                         |
| `HC0034` | Missing `operations`.                        | Include the GraphQL operation payload first.                               |
| `HC0035` | Fields out of order.                         | Send `operations`, then `map`, then files.                                 |
| `HC0037` | Missing object paths in `map`.               | Add at least one object path for each file key.                            |
| `HC0038` | Missing file referenced by `map`.            | Ensure every mapped file key exists in the multipart body.                 |
| `HC0039` | Variable path references a missing variable. | Align `map` paths with operation variables.                                |
| `HC0040` | Variable structure invalid.                  | Ensure a mapped variable path points to a supported upload value location. |
| `HC0041` | Invalid path syntax.                         | Fix the object path syntax in `map`.                                       |
| `HC0042` | Path does not start with `variables`.        | Upload paths must target operation variables.                              |
| `HC0043` | Invalid `map` JSON.                          | Send a JSON object mapping file keys to path arrays.                       |
| `HC0044` | Missing `map`.                               | Include the multipart `map` field.                                         |
| `HC0077` | Missing GraphQL preflight header.            | Include the configured GraphQL preflight header for multipart requests.    |

# Persisted operation codes

Persisted operations can reject requests before resolvers run.

## `HC0020`

`HC0020` means the active persisted operation pipeline could not find the requested operation. It is used by automatic persisted operations and trusted documents.

For automatic persisted operations, the first hash-only request returns the protocol message `PersistedQueryNotFound`:

```json
{
  "errors": [
    {
      "message": "PersistedQueryNotFound",
      "extensions": { "code": "HC0020" }
    }
  ]
}
```

What to do:

- For automatic persisted operations, retry with both the operation document and the hash so the server can store it.
- For trusted documents, publish the operation to the store before clients send it.
- Verify that the client uses the same hash algorithm and operation text as the server expects.

See [automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations) and [trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents).

## `HC0067`

`HC0067` means the endpoint only accepts persisted operations, but the request tried to execute a normal operation document.

```json
{
  "errors": [
    {
      "message": "Only persisted operations are allowed.",
      "extensions": { "code": "HC0067" }
    }
  ]
}
```

What to do:

- Send a trusted document id or supported persisted operation extension.
- If non-persisted operations should be allowed in development, change the persisted operation options for that environment.
- If you customize `OperationNotAllowedError`, keep its code stable for clients and tooling.

# Authorization codes

Authorization codes can appear as field errors or request errors, depending on where the policy is applied. Field authorization can return partial data and set the protected field to `null`.

| Code                     | Meaning                                          | What to check                                                                    |
| ------------------------ | ------------------------------------------------ | -------------------------------------------------------------------------------- |
| `AUTH_NOT_AUTHENTICATED` | The user is anonymous.                           | Confirm authentication middleware, tokens, cookies, and endpoint order.          |
| `AUTH_NOT_AUTHORIZED`    | The user does not satisfy the policy.            | Check roles, claims, resource handlers, and policy logic.                        |
| `AUTH_NO_DEFAULT_POLICY` | A default policy is required but not configured. | Configure ASP.NET Core authorization options.                                    |
| `AUTH_POLICY_NOT_FOUND`  | A named policy is missing.                       | Register the policy with the exact name used by `[Authorize]` or `.Authorize()`. |

Example field-level shape:

```json
{
  "data": {
    "viewerBasket": null
  },
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": ["viewerBasket"],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ]
}
```

See [authorization](/docs/hotchocolate/v16/build/security/authorization) and the [`Authorize` attribute](/docs/hotchocolate/v16/build/attributes/authorize).

# Validation and security codes

These codes are produced while parsing or validating a GraphQL document. The operation is rejected before resolvers run.

| Code     | Meaning                                  | What to check                                                               |
| -------- | ---------------------------------------- | --------------------------------------------------------------------------- |
| `HC0014` | GraphQL document syntax error.           | Fix the GraphQL document syntax.                                            |
| `HC0046` | Introspection is not allowed.            | Enable introspection for the request or stop sending introspection queries. |
| `HC0086` | Maximum introspection depth exceeded.    | Reduce the introspection query depth or change the configured limit.        |
| `HC0087` | Maximum coordinate cycle depth exceeded. | Reduce recursive coordinate traversal or change the configured limit.       |
| `HC0107` | Field merge validation budget exhausted. | Reduce overlapping field selections or increase the validation budget.      |

## `HC0046`

When introspection is disabled, introspection fields such as `__schema` are rejected:

```json
{
  "errors": [
    {
      "message": "Introspection is not allowed for the current request.",
      "extensions": {
        "field": "__schema",
        "code": "HC0046"
      }
    }
  ]
}
```

See [introspection](/docs/hotchocolate/v16/build/security/introspection) and [execution depth and limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits).

# Cost analysis codes

Cost analysis runs during validation. Rejected operations do not execute resolvers.

| Code     | Meaning                                   | What to check                                                                            |
| -------- | ----------------------------------------- | ---------------------------------------------------------------------------------------- |
| `HC0047` | The maximum allowed cost was exceeded.    | Reduce selected fields, add paging limits, or adjust cost options.                       |
| `HC0082` | Exactly one slicing argument is required. | Provide one of the configured slicing arguments, such as `first` or `last`.              |
| `HC0048` | Cost analyzer state is incomplete.        | Treat this as framework diagnostic output unless your tests verify it for your scenario. |

Example `HC0047` response:

```json
{
  "errors": [
    {
      "message": "The maximum allowed type cost was exceeded.",
      "extensions": {
        "code": "HC0047",
        "maxTypeCost": 1,
        "typeCost": 2
      }
    }
  ]
}
```

See [cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis), [`@cost`](/docs/hotchocolate/v16/build/attributes/cost), and [`@listSize`](/docs/hotchocolate/v16/build/attributes/listsize).

# Execution and variable codes

Execution codes can come from operation lookup, variable coercion, resolver invocation, timeout handling, cancellation, result completion, and node lookup.

| Code                                   | Meaning                                    | What to check                                                       |
| -------------------------------------- | ------------------------------------------ | ------------------------------------------------------------------- |
| `HC0008`                               | Task processing error.                     | Inspect server logs and resolver failures.                          |
| `HC0015`                               | Operation document not found.              | Check document storage, request ids, and persisted operation setup. |
| `HC0016`                               | Invalid type.                              | Check variable types and input coercion.                            |
| `HC0017`                               | Value must be an input type.               | Check schema input definitions and variable values.                 |
| `HC0018`                               | Non-null value completed as `null`.        | Fix the resolver, schema nullability, or upstream data.             |
| `HC0019`                               | Root value could not be created.           | Check root value registration and request services.                 |
| `HC0045`                               | Request timeout.                           | Reduce operation work or change request timeout options.            |
| `HC0049`                               | Request canceled.                          | Check client disconnects, cancellation tokens, and server timeouts. |
| `HC0053`                               | Parent value cannot be cast.               | Check resolver parent type and schema binding.                      |
| `HC0054`                               | OneOf input has no field set.              | Send exactly one non-null field.                                    |
| `HC0055`                               | OneOf input has more than one field set.   | Send exactly one non-null field.                                    |
| `HC0056`                               | OneOf input field is `null`.               | Send a non-null value for the selected field.                       |
| `HC0057`                               | OneOf input field must be non-null.        | Fix the input value.                                                |
| `HC0076`                               | Too many nodes were fetched at once.       | Lower node batch size or change the configured limit.               |
| `EXEC_BATCH_AUTO_MAP_VAR_TYPE`         | Batched variable type could not be mapped. | Check batching variable definitions and values.                     |
| `EXEC_BATCH_VAR_SERIALIZE`             | Batched variable could not be serialized.  | Check variable serializers and input values.                        |
| `EXEC_INVALID_LEAF_VALUE`              | Leaf value could not be serialized.        | Check resolver return types and scalar serializers.                 |
| `EXEC_LIST_TYPE_NOT_SUPPORTED`         | List type is not supported.                | Return a supported enumerable shape.                                |
| `EXEC_UNABLE_TO_RESOLVE_ABSTRACT_TYPE` | Abstract type resolution failed.           | Configure interface or union runtime type resolution.               |

See [execution pipeline](/docs/hotchocolate/v16/build/execution-engine/pipeline), [resolver result handling](/docs/hotchocolate/v16/build/resolvers/resolver-result-handling), and [request context](/docs/hotchocolate/v16/build/execution-engine/request-context).

# Schema and startup codes

Startup diagnostics are usually thrown or logged while the schema is built. Fix them in schema configuration, type definitions, middleware order, or feature registration.

| Code                       | Meaning                                     | What to check                                                                        |
| -------------------------- | ------------------------------------------- | ------------------------------------------------------------------------------------ |
| `HC0050`                   | Field middleware order is invalid.          | Use the documented middleware order for paging, projections, filtering, and sorting. |
| `HC0061`                   | Reserved type name.                         | Rename the type.                                                                     |
| `HC0065`                   | Duplicate type name.                        | Rename or bind schema types explicitly.                                              |
| `HC0066`                   | Duplicate mutation error type name.         | Rename generated or explicit mutation error types.                                   |
| `HC0068`                   | Node resolver missing.                      | Configure a node resolver for Relay node types.                                      |
| `HC0069`                   | Mutation payload must be an object type.    | Return an object payload type for mutation conventions.                              |
| `HC0070`                   | `@mutation` directive location is invalid.  | Apply the directive only to supported object fields.                                 |
| `HC0071`                   | Directive argument has an unexpected value. | Fix the schema building directive argument.                                          |
| `HC0072`                   | Unknown directive argument.                 | Remove or rename the directive argument.                                             |
| `HC0073`                   | Type system member used as runtime type.    | Use CLR runtime types for values.                                                    |
| `HC0089`                   | Mutation field must return a value.         | Return a payload value from the mutation.                                            |
| `SCHEMA_INTERFACE_NO_IMPL` | Interface implementation is incomplete.     | Implement required interface fields and arguments.                                   |
| `TS_INVALID_ARG`           | Invalid argument.                           | Check argument type, name, and default value.                                        |
| `TS_MISSING_TYPE`          | Missing type.                               | Register or bind the referenced type.                                                |
| `TS_NO_ENUM_VALUES`        | Enum has no values.                         | Add enum values.                                                                     |
| `TS_NO_FIELD_RESOLVER`     | Field has no resolver.                      | Add a resolver or bind the member correctly.                                         |
| `TS_NO_FIELD_TYPE`         | Field has no type.                          | Specify or infer the field type.                                                     |
| `TS_NO_NAME_DEFINED`       | Member has no name.                         | Provide a valid GraphQL name.                                                        |
| `TS_UNRESOLVED_TYPES`      | Unresolved types remain.                    | Register missing types or fix type references.                                       |

See [type system](/docs/hotchocolate/v16/build/type-system), [middleware order](/docs/hotchocolate/v16/build/execution-engine/well-known-middleware-keys), [Relay global identifiers](/docs/hotchocolate/v16/build/type-system/relay/global-identifiers), and [mutations](/docs/hotchocolate/v16/build/type-system/operations-mutations).

# Schema validation diagnostics

`HCV0001` through `HCV0026` are internal schema validation log codes. They can appear in test snapshots, schema validation diagnostics, or logs. They are not normal GraphQL response codes.

| Range                  | Examples                                                                        | Meaning                             |
| ---------------------- | ------------------------------------------------------------------------------- | ----------------------------------- |
| `HCV0001` to `HCV0004` | Empty object type, invalid member name, invalid deprecation usage.              | Basic type and member validation.   |
| `HCV0005` to `HCV0013` | Interface implementation and interface shape errors.                            | Interface validation.               |
| `HCV0014` to `HCV0019` | Empty union, enum, or input object, invalid OneOf field, input cycles.          | Composite type validation.          |
| `HCV0020` to `HCV0026` | Directive location and undefined type, argument, enum, or directive references. | Directive and reference validation. |

Use these codes to find the failing rule in validation output. Do not use them as client response contracts.

# Feature and provider codes

These codes come from scalars, data providers, paging, filtering, and spatial packages. Some appear during schema building, while others can surface during execution or provider translation.

| Code                           | Feature       | Meaning                                   | What to check                                                   |
| ------------------------------ | ------------- | ----------------------------------------- | --------------------------------------------------------------- |
| `HC0001`                       | Scalars       | Unsupported scalar runtime type.          | Check resolver return values and scalar configuration.          |
| `HC0002`                       | Scalars       | Invalid scalar literal syntax or format.  | Check literal values and custom scalar parsing.                 |
| `HC0021`                       | Data          | List shape not supported.                 | Check projections and provider support.                         |
| `HC0022`                       | Data          | More than one element returned.           | Check provider query shape.                                     |
| `HC0023`                       | Data          | Filtering projection failed.              | Check projection and filtering configuration.                   |
| `HC0024`                       | Data          | Sorting projection failed.                | Check projection and sorting configuration.                     |
| `HC0025`                       | Paging        | Pagination provider missing.              | Register the provider for the backing data source.              |
| `HC0026`                       | Data          | Non-null data value failed.               | Check provider data and schema nullability.                     |
| `HC0028`                       | Relay or data | Valid node field missing.                 | Ensure the type exposes `items` or `nodes` where required.      |
| `HC0029`                       | Spatial       | Unknown coordinate reference system.      | Configure or convert to a supported CRS.                        |
| `HC0030`                       | Spatial       | M-value coordinate cannot be reprojected. | Remove the M value or avoid reprojection.                       |
| `HC0031`                       | Paging        | Node type could not be inferred.          | Specify the element type or return an iterable resolver result. |
| `HC0032`                       | Paging        | Pagination schema type invalid.           | Use a valid GraphQL output type.                                |
| `HC0051`                       | Paging        | Maximum page size exceeded.               | Lower `first` or `last`, or change paging options.              |
| `HC0052`                       | Paging        | Paging boundaries missing.                | Send `first` or `last` as required by the field.                |
| `HC0078`                       | Paging        | Invalid cursor format.                    | Use cursors returned by the server.                             |
| `HC0079`                       | Paging        | Page size is below the minimum.           | Send a non-negative page size.                                  |
| `HC0090`                       | Paging        | Required `first` value missing.           | Send `first` for fields that require it.                        |
| `FILTER_FIELD_DESCRIPTOR_TYPE` | Filtering     | Filter field descriptor type invalid.     | Check custom filter configuration.                              |
| `FILTER_OBJECT_TYPE`           | Filtering     | Filter object type invalid.               | Check custom filter types and conventions.                      |

See [pagination](/docs/hotchocolate/v16/build/pagination), [filtering, sorting, and projections](/docs/hotchocolate/v16/build/filtering-sorting-projections), and [custom scalars](/docs/hotchocolate/v16/build/type-system/scalars/custom-scalars).

# Custom application codes

Use your own stable code namespace for application errors. Keep built-in Hot Chocolate codes separate from product-domain codes.

```csharp
IError error = ErrorBuilder.New()
    .SetMessage("Product image is temporarily unavailable.")
    .SetCode("PRODUCT_IMAGE_UNAVAILABLE")
    .SetExtension("retryAfterSeconds", 60)
    .Build();
```

Guidelines:

- Use one consistent casing convention, such as `UPPER_SNAKE_CASE`.
- Document codes that clients may branch on.
- Keep messages safe for clients and put sensitive details in server logs.
- Use error filters to map exceptions to stable application codes.
- Prefer typed schema results for expected business outcomes that clients should query as normal data.

See [Errors](/docs/hotchocolate/v16/build/errors), [Error builder](/docs/hotchocolate/v16/build/errors/error-builder), and [Error filters](/docs/hotchocolate/v16/build/errors/error-filters).

# Find the source of a code

For framework troubleshooting, search the source for the code value and for the `ErrorCodes` constant name.

```bash
rg -n 'HC0047|CostExceeded|SetCode\(|extensions\["code"\]' src/HotChocolate
```

Search tests and snapshots when you need the exact response shape:

```bash
rg -n '"code": "HC0047"|HC0047' src/HotChocolate --glob '*.{cs,snap}'
```

Useful source anchors:

- `src/HotChocolate/Primitives/src/Primitives/ErrorCodes.cs` lists public framework constants.
- `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline` contains HTTP transport and multipart upload errors.
- `src/HotChocolate/Core/src/Validation` contains document validation and security errors.
- `src/HotChocolate/CostAnalysis/src/CostAnalysis` contains cost analysis errors.
- `src/HotChocolate/PersistedOperations/src` contains persisted operation errors.
- `src/HotChocolate/Core/src/Authorization` contains authorization errors.
- `src/HotChocolate/Core/src/Types.Validation/Logging/LogEntryCodes.cs` contains `HCV` schema validation diagnostics.

# Troubleshooting by symptom

| Symptom                                                     | Likely codes                                     | First action                                                                        |
| ----------------------------------------------------------- | ------------------------------------------------ | ----------------------------------------------------------------------------------- |
| Hash-only persisted query returns `PersistedQueryNotFound`. | `HC0020`                                         | Retry with the document and hash, or publish the trusted document.                  |
| Normal query is rejected in production.                     | `HC0067`                                         | Send a persisted operation id or change persisted-only configuration.               |
| Schema tooling cannot introspect the server.                | `HC0046`, `HC0086`                               | Check introspection settings and depth limits.                                      |
| Query is rejected before resolvers run.                     | `HC0014`, `HC0047`, `HC0082`, `HC0087`, `HC0107` | Fix syntax, validation, cost, or security-limit failures.                           |
| A protected field is `null` with an error.                  | `AUTH_NOT_AUTHENTICATED`, `AUTH_NOT_AUTHORIZED`  | Check authentication and authorization configuration.                               |
| Upload request fails before execution.                      | `HC0033` to `HC0044`, `HC0077`                   | Check multipart field order, `operations`, `map`, file keys, and preflight header.  |
| Field becomes `null` because a non-null value failed.       | `HC0018`                                         | Fix resolver data or schema nullability.                                            |
| Schema fails at startup.                                    | `HC0050`, `HC0065`, `TS_*`, `HCV*`               | Inspect the schema exception or validation log and fix schema configuration.        |
| Validation error has no code.                               | none                                             | Use `message`, `locations`, and related extensions such as `coordinate` or `field`. |

# Related pages

- [Errors](/docs/hotchocolate/v16/build/errors)
- [Error builder](/docs/hotchocolate/v16/build/errors/error-builder)
- [Error filters](/docs/hotchocolate/v16/build/errors/error-filters)
- [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport)
- [Authorization](/docs/hotchocolate/v16/build/security/authorization)
- [Introspection](/docs/hotchocolate/v16/build/security/introspection)
- [Execution depth and limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits)
- [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis)
- [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations)
- [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents)
- [Execution pipeline](/docs/hotchocolate/v16/build/execution-engine/pipeline)
- [Pagination](/docs/hotchocolate/v16/build/pagination)
- [Filtering, sorting, and projections](/docs/hotchocolate/v16/build/filtering-sorting-projections)
- [OpenTelemetry](/docs/hotchocolate/v16/build/observability/opentelemetry)
