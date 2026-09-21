# Pipelines

This document shows the various pre-configured pipelines.

Document normalization is a lazy service resolved by the first stage that
needs it, now variable coercion, not a pipeline stage. Its result is cached
inside the normalizer by operation id, so a later stage or a later request
for the same operation reuses it instead of normalizing again.

## Default Pipeline

```mermaid
sequenceDiagram
    Diagnostics->>Exceptions Handing: track { next(context) }
    Exceptions Handing->>Cache Document: try { next(context) }
    Cache Document->>Parse Document: Document?
    Parse Document->>Validation: Document?
    Validation->>Coerce Variables: Document! and IsValid
    Coerce Variables->>Cost Analysis: IVariableCollection
    Cost Analysis->>Cache Operation: OperationCost
    Cache Operation->>Compile Operation: IPreparedOperation?
    Compile Operation->>Skip Warmup: IPreparedOperation
    Skip Warmup->>Execute Operation: IPreparedOperation

    Execute Operation-->>Skip Warmup: IExecutionResult
    Skip Warmup-->>Compile Operation: IExecutionResult
    Compile Operation-->>Cache Operation: IExecutionResult
    Cache Operation-->>Cache Operation: Cache Operation
    Cache Operation-->>Cost Analysis: IExecutionResult
    Cost Analysis-->>Coerce Variables: IExecutionResult
    Coerce Variables-->>Validation: IExecutionResult
    Validation-->>Parse Document: IExecutionResult
    Parse Document-->>Cache Document: IExecutionResult
    Cache Document-->>Cache Document: Cache Document
    Cache Document-->>Exceptions Handing: IExecutionResult
    Exceptions Handing-->>Diagnostics: IExecutionResult
```

## Persisted Operation Pipeline

```mermaid
sequenceDiagram
    Diagnostics->>Exceptions Handing: track { next(context) }
    Exceptions Handing->>Cache Document: try { next(context) }
    Cache Document->>Load Persisted Document: Document?
    Load Persisted Document->>Parse Document: Document?
    Parse Document->>Validation: Document?
    Validation->>Coerce Variables: Document! and IsValid
    Coerce Variables->>Cost Analysis: IVariableCollection
    Cost Analysis->>Cache Operation: OperationCost
    Cache Operation->>Compile Operation: IPreparedOperation?
    Compile Operation->>Skip Warmup: IPreparedOperation
    Skip Warmup->>Execute Operation: IPreparedOperation

    Execute Operation-->>Skip Warmup: IExecutionResult
    Skip Warmup-->>Compile Operation: IExecutionResult
    Compile Operation-->>Cache Operation: IExecutionResult
    Cache Operation-->>Cache Operation: Cache Operation
    Cache Operation-->>Cost Analysis: IExecutionResult
    Cost Analysis-->>Coerce Variables: IExecutionResult
    Coerce Variables-->>Validation: IExecutionResult
    Validation-->>Parse Document: IExecutionResult
    Parse Document-->>Load Persisted Document: IExecutionResult
    Load Persisted Document-->>Cache Document: IExecutionResult
    Cache Document-->>Cache Document: Cache Document
    Cache Document-->>Exceptions Handing: IExecutionResult
    Exceptions Handing-->>Diagnostics: IExecutionResult
```
