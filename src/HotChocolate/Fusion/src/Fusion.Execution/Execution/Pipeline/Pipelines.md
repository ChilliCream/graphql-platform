# Pipelines

This document shows the various pre-configured Fusion gateway pipelines.

"Document Normalization" below refers to the `DocumentNormalizationMiddleware`, registered through
`UseDocumentNormalization()`. It creates the operation id, de-fragmentizes the operation and removes
statically excluded selections, and caches the result in the internal `NormalizedDocumentCache` keyed
by that operation id, so a later request for the same operation reuses it instead of rewriting the
document again.

## Default Pipeline

```mermaid
sequenceDiagram
    Instrumentation->>Exceptions: track { next(context) }
    Exceptions->>Timeout: try { next(context) }
    Timeout->>Document Cache: enforce { next(context) }
    Document Cache->>Document Parser: Document?
    Document Parser->>Document Validation: Document?
    Document Validation->>Document Normalization: Document! and IsValid
    Document Normalization-->>Document Normalization: rewrite or reuse NormalizedDocument
    Document Normalization->>Operation Variable Coercion: NormalizedDocument
    Operation Variable Coercion->>Cost Analysis: IVariableValueCollection
    Cost Analysis->>Operation Plan Cache: CostPlan and CostEstimate
    Operation Plan Cache->>Operation Plan: OperationPlan?
    Operation Plan->>Skip Warmup Execution: OperationPlan
    Skip Warmup Execution->>Concurrency Gate: not Warmup?
    Concurrency Gate->>Operation Execution: slot acquired

    Operation Execution-->>Concurrency Gate: IExecutionResult
    Concurrency Gate-->>Skip Warmup Execution: IExecutionResult
    Skip Warmup Execution-->>Operation Plan: IExecutionResult
    Operation Plan-->>Operation Plan: Cache Operation Plan
    Operation Plan-->>Operation Plan Cache: IExecutionResult
    Operation Plan Cache-->>Cost Analysis: IExecutionResult
    Cost Analysis-->>Operation Variable Coercion: IExecutionResult
    Operation Variable Coercion-->>Document Normalization: IExecutionResult
    Document Normalization-->>Document Validation: IExecutionResult
    Document Validation-->>Document Parser: IExecutionResult
    Document Parser-->>Document Cache: IExecutionResult
    Document Cache-->>Document Cache: Cache Document
    Document Cache-->>Timeout: IExecutionResult
    Timeout-->>Exceptions: IExecutionResult
    Exceptions-->>Instrumentation: IExecutionResult
```

## Persisted Operation Pipeline

```mermaid
sequenceDiagram
    Instrumentation->>Exceptions: track { next(context) }
    Exceptions->>Timeout: try { next(context) }
    Timeout->>Document Cache: enforce { next(context) }
    Document Cache->>Read Persisted Operation: Document?
    Read Persisted Operation->>Persisted Operation Not Found: DocumentId?
    Persisted Operation Not Found->>Only Persisted Operation Allowed: Document?
    Only Persisted Operation Allowed->>Document Parser: Document? or DocumentId?
    Document Parser->>Document Validation: Document?
    Document Validation->>Document Normalization: Document! and IsValid
    Document Normalization-->>Document Normalization: rewrite or reuse NormalizedDocument
    Document Normalization->>Operation Variable Coercion: NormalizedDocument
    Operation Variable Coercion->>Cost Analysis: IVariableValueCollection
    Cost Analysis->>Operation Plan Cache: CostPlan and CostEstimate
    Operation Plan Cache->>Operation Plan: OperationPlan?
    Operation Plan->>Skip Warmup Execution: OperationPlan
    Skip Warmup Execution->>Concurrency Gate: not Warmup?
    Concurrency Gate->>Operation Execution: slot acquired

    Operation Execution-->>Concurrency Gate: IExecutionResult
    Concurrency Gate-->>Skip Warmup Execution: IExecutionResult
    Skip Warmup Execution-->>Operation Plan: IExecutionResult
    Operation Plan-->>Operation Plan: Cache Operation Plan
    Operation Plan-->>Operation Plan Cache: IExecutionResult
    Operation Plan Cache-->>Cost Analysis: IExecutionResult
    Cost Analysis-->>Operation Variable Coercion: IExecutionResult
    Operation Variable Coercion-->>Document Normalization: IExecutionResult
    Document Normalization-->>Document Validation: IExecutionResult
    Document Validation-->>Document Parser: IExecutionResult
    Document Parser-->>Only Persisted Operation Allowed: IExecutionResult
    Only Persisted Operation Allowed-->>Persisted Operation Not Found: IExecutionResult
    Persisted Operation Not Found-->>Read Persisted Operation: IExecutionResult
    Read Persisted Operation-->>Document Cache: IExecutionResult
    Document Cache-->>Document Cache: Cache Document
    Document Cache-->>Timeout: IExecutionResult
    Timeout-->>Exceptions: IExecutionResult
    Exceptions-->>Instrumentation: IExecutionResult
```

## Automatic Persisted Operation Pipeline

```mermaid
sequenceDiagram
    Instrumentation->>Exceptions: track { next(context) }
    Exceptions->>Timeout: try { next(context) }
    Timeout->>Document Cache: enforce { next(context) }
    Document Cache->>Read Persisted Operation: Document?
    Read Persisted Operation->>Automatic Persisted Operation Not Found: DocumentId?
    Automatic Persisted Operation Not Found->>Write Persisted Operation: Document?
    Write Persisted Operation->>Document Parser: Document?
    Document Parser->>Document Validation: Document?
    Document Validation->>Document Normalization: Document! and IsValid
    Document Normalization-->>Document Normalization: rewrite or reuse NormalizedDocument
    Document Normalization->>Operation Variable Coercion: NormalizedDocument
    Operation Variable Coercion->>Cost Analysis: IVariableValueCollection
    Cost Analysis->>Operation Plan Cache: CostPlan and CostEstimate
    Operation Plan Cache->>Operation Plan: OperationPlan?
    Operation Plan->>Skip Warmup Execution: OperationPlan
    Skip Warmup Execution->>Concurrency Gate: not Warmup?
    Concurrency Gate->>Operation Execution: slot acquired

    Operation Execution-->>Concurrency Gate: IExecutionResult
    Concurrency Gate-->>Skip Warmup Execution: IExecutionResult
    Skip Warmup Execution-->>Operation Plan: IExecutionResult
    Operation Plan-->>Operation Plan: Cache Operation Plan
    Operation Plan-->>Operation Plan Cache: IExecutionResult
    Operation Plan Cache-->>Cost Analysis: IExecutionResult
    Cost Analysis-->>Operation Variable Coercion: IExecutionResult
    Operation Variable Coercion-->>Document Normalization: IExecutionResult
    Document Normalization-->>Document Validation: IExecutionResult
    Document Validation-->>Document Parser: IExecutionResult
    Document Parser-->>Write Persisted Operation: IExecutionResult
    Write Persisted Operation-->>Automatic Persisted Operation Not Found: IExecutionResult
    Automatic Persisted Operation Not Found-->>Read Persisted Operation: IExecutionResult
    Read Persisted Operation-->>Document Cache: IExecutionResult
    Document Cache-->>Document Cache: Cache Document
    Document Cache-->>Timeout: IExecutionResult
    Timeout-->>Exceptions: IExecutionResult
    Exceptions-->>Instrumentation: IExecutionResult
```
