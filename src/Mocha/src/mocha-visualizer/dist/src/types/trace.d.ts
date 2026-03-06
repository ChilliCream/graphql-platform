/** Base fields shared by all activity types */
export interface MessageActivityBase {
  /** Unique activity ID */
  id: string;
  /** Parent activity ID (null for root) */
  parentId: string | null;
  /** Wall-clock start time (ISO 8601) */
  startTime: string;
  /** Duration in milliseconds (0 for instantaneous) */
  durationMs: number;
  /** Outcome of this activity */
  status: "ok" | "error";
  /** Error details (present when status is 'error') */
  error?: {
    type: string;
    message: string;
  };
}
/** Application code published an event */
export interface PublishActivity extends MessageActivityBase {
  operation: "publish";
  /** Message type short name (e.g., "BarEvent") */
  messageType: string;
  /** Message type URN identity */
  messageTypeIdentity: string;
  /** Transport system used (e.g., "rabbitmq") */
  transport: string;
}
/** Application code sent a command */
export interface SendActivity extends MessageActivityBase {
  operation: "send";
  messageType: string;
  messageTypeIdentity: string;
  transport: string;
}
/** Message handed to the transport for delivery to the broker */
export interface DispatchActivity extends MessageActivityBase {
  operation: "dispatch";
  messageType: string;
  messageTypeIdentity: string;
  /** Dispatch endpoint name (e.g., "e/my-company.project-name.bar") */
  endpointName: string;
  /** Full endpoint address (e.g., "rabbitmq://localhost/e/my-company.project-name.bar") */
  endpointAddress: string;
  transport: string;
}
/** Message received from the transport (pulled from a queue) */
export interface ReceiveActivity extends MessageActivityBase {
  operation: "receive";
  messageType: string;
  messageTypeIdentity: string;
  /** Receive endpoint name (e.g., "explorer.simple") */
  endpointName: string;
  /** Full endpoint address (e.g., "rabbitmq://localhost/explorer.simple") */
  endpointAddress: string;
  transport: string;
}
/** Consumer handled the message */
export interface ConsumeActivity extends MessageActivityBase {
  operation: "consume";
  messageType: string;
  messageTypeIdentity: string;
  /** Consumer class name (e.g., "SimpleHandler") */
  consumerName: string;
}
/** Saga state machine transitioned */
export interface SagaTransitionActivity extends MessageActivityBase {
  operation: "saga-transition";
  /** Saga name (e.g., "order-saga") */
  sagaName: string;
  /** Saga instance ID */
  sagaInstanceId: string;
  /** State before transition */
  fromState: string;
  /** State after transition */
  toState: string;
  /** Event that triggered the transition */
  eventType: string;
}
/** Application code sent a request (request/reply pattern) */
export interface RequestActivity extends MessageActivityBase {
  operation: "request";
  messageType: string;
  messageTypeIdentity: string;
  transport: string;
  /** Conversation ID linking request and reply */
  conversationId: string;
}
/** Reply message sent back (request/reply pattern) */
export interface ReplyActivity extends MessageActivityBase {
  operation: "reply";
  messageType: string;
  messageTypeIdentity: string;
  transport: string;
  /** Conversation ID linking request and reply */
  conversationId: string;
}
/** Consumer subscribed to an event */
export interface SubscribeActivity extends MessageActivityBase {
  operation: "subscribe";
  messageType: string;
  messageTypeIdentity: string;
  transport: string;
}
/**
 * Discriminated union of all activity types.
 * Each variant carries exactly the data relevant to that operation.
 */
export type MessageActivity =
  | PublishActivity
  | SendActivity
  | RequestActivity
  | DispatchActivity
  | ReceiveActivity
  | ConsumeActivity
  | SagaTransitionActivity
  | ReplyActivity
  | SubscribeActivity;
/**
 * A single message trace passed to the visualizer.
 * Contains ALL activities for one end-to-end message flow.
 */
export interface MessageTrace {
  /** Trace identifier */
  traceId: string;
  /** Flat list of activities (tree structure encoded via parentId) */
  activities: MessageActivity[];
}
