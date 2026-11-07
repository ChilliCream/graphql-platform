# DeclareStream_Should_AbsorbConventionSubjects_When_ItSharesTheDerivedName

```text
stream SHAPE_SERVICE (declared) captures [mocha.shape.contracts.>, mocha.transport.nats.tests.shape-booking_error, mocha.transport.nats.tests.shape-booking_skipped, mocha.transport.nats.tests.shape-commands_error, mocha.transport.nats.tests.shape-commands_skipped, mocha.transport.nats.tests.shape-pool_error, mocha.transport.nats.tests.shape-pool_skipped, mocha.transport.nats.tests.shape-taken_error, mocha.transport.nats.tests.shape-taken_skipped]
consumer mocha_transport_nats_tests_shape-booking reads SHAPE_SERVICE
consumer mocha_transport_nats_tests_shape-pool reads SHAPE_SERVICE
consumer mocha_transport_nats_tests_shape-taken reads SHAPE_SERVICE
consumer shape-commands reads SHAPE_SERVICE
```
