export interface Host {
  serviceName: string;
  assemblyName: string;
  instanceId: string;
}
export interface MessageType {
  identity: string;
  runtimeType: string;
  runtimeTypeFullName: string;
  isInterface: boolean;
  isInternal: boolean;
  enclosedMessageIdentities?: string[];
}
export interface Consumer {
  name: string;
  identityType: string;
  identityTypeFullName: string;
  sagaName?: string;
  isBatch?: boolean;
}
export interface Endpoint {
  name: string;
  address: string;
  transportName: string;
}
export interface InboundRoute {
  kind: "subscribe" | "request" | "reply";
  messageTypeIdentity: string;
  consumerName: string;
  endpoint: Endpoint;
}
export interface OutboundRoute {
  kind: "publish" | "send";
  messageTypeIdentity: string;
  endpoint: Endpoint;
}
export interface Routes {
  inbound: InboundRoute[];
  outbound: OutboundRoute[];
}
/** Generic properties bag - transport-specific, not typed */
export type TopologyEntityProperties = Record<string, unknown>;
export interface TopologyEntity {
  kind: string;
  name: string;
  address: string;
  /** Message flow direction from transport's perspective */
  flow: "inbound" | "outbound";
  properties: TopologyEntityProperties;
}
/** Generic properties bag - transport-specific, not typed */
export type TopologyLinkProperties = Record<string, unknown>;
export interface TopologyLink {
  kind: string;
  address: string;
  source: string;
  target: string;
  /** Flow direction: forward (source→target), reverse (target→source), bidirectional */
  direction: "forward" | "reverse" | "bidirectional";
  properties: TopologyLinkProperties;
}
export interface Topology {
  address: string;
  entities: TopologyEntity[];
  links: TopologyLink[];
}
export interface ReceiveEndpoint {
  name: string;
  kind: "reply" | "default";
  address: string;
  source: {
    address: string;
  };
}
export interface DispatchEndpoint {
  name: string;
  kind: "reply" | "default";
  address: string;
  destination: {
    address: string;
  };
}
export interface Transport {
  identifier: string;
  name: string;
  schema: string;
  transportType: string;
  receiveEndpoints: ReceiveEndpoint[];
  dispatchEndpoints: DispatchEndpoint[];
  topology: Topology;
}
export interface SagaTransitionSend {
  messageType: string;
  messageTypeFullName: string;
}
export interface SagaTransition {
  eventType: string;
  eventTypeFullName: string;
  transitionTo: string;
  transitionKind: "request" | "reply" | "event";
  autoProvision: boolean;
  send?: SagaTransitionSend[];
}
export interface SagaResponse {
  eventType: string;
  eventTypeFullName: string;
}
export interface SagaState {
  name: string;
  isInitial: boolean;
  isFinal: boolean;
  onEntry: Record<string, unknown>;
  transitions: SagaTransition[];
  response?: SagaResponse;
}
export interface Saga {
  name: string;
  stateType: string;
  stateTypeFullName: string;
  consumerName: string;
  states: SagaState[];
}
/** Service definition - application configuration */
export interface Service {
  host: Host;
  messageTypes: MessageType[];
  consumers: Consumer[];
  routes: Routes;
  sagas: Saga[];
}
/** Root diagram data with services and transports separated */
export interface DiagramData {
  services: Service[];
  transports: Transport[];
}
