# StartAsync_Should_StillCoverItsOwnSubjects_When_ItDeclaresAnotherServicesStream

```text
stream DECLARED_UPSTREAM captures [mocha.declared.contracts.>]
stream SHARED_DECLARING captures [mocha.transport.nats.tests.behaviors.pallet-loaded, mocha.transport.nats.tests.declared-stream_error, mocha.transport.nats.tests.declared-stream_skipped]
consumer mocha_transport_nats_tests_declared-stream reads SHARED_DECLARING filtered on [mocha.transport.nats.tests.behaviors.pallet-loaded]
```
