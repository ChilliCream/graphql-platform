# StartAsync_Should_BindToTheOwningStream_When_AnotherServiceAlreadyCapturesTheSubject

## FirstService

```text
stream SHARED_BILLING captures [mocha.shared.contracts.widget-shipped, mocha.transport.nats.tests.billing_error, mocha.transport.nats.tests.billing_skipped]
consumer mocha_transport_nats_tests_billing reads SHARED_BILLING filtered on [mocha.shared.contracts.widget-shipped]
```

## SecondService

```text
stream SHARED_ANALYTICS captures [mocha.transport.nats.tests.analytics_error, mocha.transport.nats.tests.analytics_skipped]
consumer mocha_transport_nats_tests_analytics reads SHARED_BILLING filtered on [mocha.shared.contracts.widget-shipped]
```
