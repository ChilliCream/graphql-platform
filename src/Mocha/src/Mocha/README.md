# Pipeline

## Receive

Transport sets body, content type and cancellation token.

- ContextInitialization
  - Sets endpoint, transport, services, host
- MessageEnvelopeParsing

  - This is done by a transport middleware as it is different per transport. it leaves open the possibility to add other middleware before this one.

- Instrumentation

  - Adds tracing information to the context. REQUIRES the headers to be already there

- MessageTypeSelection

  - Selects the message type based on the content type

- ReceiveConsumerSelection
  - Selects the consumers based on the message type (recusrively)

## Consume

- Instrumentation
  - Adds instrumentation for the handler

## Dispatch

- MessageTypeSelection

  - Selects the message type based on the content type

- MessageFormatting
  - Formats the message based on the message type
