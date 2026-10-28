# Endpoint_Should_ScopeTheSagaTimeout_When_TwoServicesHostSagas

## AlphaService

```text
timeout durables: alpha-service_saga-timed-out
timeout fault subjects: alpha-service.saga-timed-out_error, alpha-service.saga-timed-out_skipped, mocha.sagas.saga-timed-out
```

## BetaService

```text
timeout durables: beta-service_saga-timed-out
timeout fault subjects: beta-service.saga-timed-out_error, beta-service.saga-timed-out_skipped, mocha.sagas.saga-timed-out
```
