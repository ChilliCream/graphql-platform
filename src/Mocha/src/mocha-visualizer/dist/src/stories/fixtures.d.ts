import { CompactNodeData } from "../components/nodes/CompactNode";
import { SimpleRouteNodeData } from "../components/nodes/SimpleRouteNode";
import { SimpleGroupLabelData } from "../components/nodes/SimpleGroupLabel";
import { SimpleSectionLabelData } from "../components/nodes/SimpleSectionLabel";
import { SummaryServiceNodeData } from "../components/nodes/SummaryServiceNode";
import { SummaryTransportNodeData } from "../components/nodes/SummaryTransportNode";
import { FocusStateNodeData } from "../utils/sagaFocusLayout";
import { SearchableEntry } from "../utils/searchEngine";
import { MessageTrace } from "../types/trace";
import { Saga, DiagramData } from "../types/diagram";
export declare const compactConsumer: CompactNodeData;
export declare const compactSaga: CompactNodeData;
export declare const compactMessage: CompactNodeData;
export declare const compactEndpoint: CompactNodeData;
export declare const compactEntityExchange: CompactNodeData;
export declare const compactEntityQueue: CompactNodeData;
export declare const compactBatchConsumer: CompactNodeData;
export declare const compactWithTrace: CompactNodeData;
export declare const routeSubscribe: SimpleRouteNodeData;
export declare const routePublish: SimpleRouteNodeData;
export declare const routeSend: SimpleRouteNodeData;
export declare const routeBinding: SimpleRouteNodeData;
export declare const routeWithTrace: SimpleRouteNodeData;
export declare const groupService: SimpleGroupLabelData;
export declare const groupTransport: SimpleGroupLabelData;
export declare const sectionLabel: SimpleSectionLabelData;
export declare const summaryService: SummaryServiceNodeData;
export declare const summaryServiceMultiTransport: SummaryServiceNodeData;
export declare const summaryTransport: SummaryTransportNodeData;
export declare const focusInitial: FocusStateNodeData;
export declare const focusIntermediate: FocusStateNodeData;
export declare const focusFinal: FocusStateNodeData;
export declare const orderSaga: Saga;
export declare const searchIndex: SearchableEntry[];
export declare const sampleTrace: MessageTrace;
export declare const traceWithError: MessageTrace;
export declare const detailConsumerNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    fullData: {
      name: string;
      identity: string;
    };
  };
};
export declare const detailEndpointNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    fullData: {
      name: string;
      address: string;
      transportName: string;
    };
  };
};
export declare const detailEntityNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    entityKind: string;
    fullData: {
      name: string;
      address: string;
      kind: string;
      flow: string;
      properties: {
        type: string;
        durable: boolean;
      };
    };
  };
};
export declare const detailRouteNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    fullData: {
      kind: string;
      direction: string;
      messageTypeIdentity: string;
      consumerName: string;
      endpointName: string;
    };
  };
};
export declare const detailSagaNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    fullData: {
      name: string;
      states: import("..").SagaState[];
    };
  };
};
export declare const minimalDiagram: DiagramData;
export declare const emptyDiagram: DiagramData;
/** Endpoint with dispatch subType - different icon color from receive */
export declare const compactEndpointDispatch: CompactNodeData;
/** Entity with no entityKind - falls back to generic cube icon */
export declare const compactEntityNoKind: CompactNodeData;
/** Entity with topic entityKind */
export declare const compactEntityTopic: CompactNodeData;
/** Very long label - tests CSS text-overflow truncation */
export declare const compactLongLabel: CompactNodeData;
/** Route with no direction - tests default rendering */
export declare const routeNoDirection: SimpleRouteNodeData;
/** Route with reply kind */
export declare const routeReply: SimpleRouteNodeData;
/** All counts zero - bottom row should be hidden */
export declare const summaryServiceZeroCounts: SummaryServiceNodeData;
/** Only consumers, no messages/sagas/transports */
export declare const summaryServiceConsumersOnly: SummaryServiceNodeData;
/** No entity kind breakdown - falls back to totalEntityCount */
export declare const summaryTransportNoKinds: SummaryTransportNodeData;
/** Single entity kind */
export declare const summaryTransportOneKind: SummaryTransportNodeData;
/** Bare state - no response, no sendActions, not initial or final */
export declare const focusBare: FocusStateNodeData;
/** State with only a single send action, no response */
export declare const focusSendOnly: FocusStateNodeData;
/** State with response but no send actions */
export declare const focusResponseOnly: FocusStateNodeData;
/** Single-state saga - only an initial+final state with no transitions */
export declare const singleStateSaga: Saga;
/** Saga with no outbound transitions from any state */
export declare const noTransitionsSaga: Saga;
/** Empty trace - no activities at all */
export declare const emptyTrace: MessageTrace;
/** Single-activity trace */
export declare const singleActivityTrace: MessageTrace;
/** Trace with request/reply + saga transition activities */
export declare const requestReplySagaTrace: MessageTrace;
/** Binding node with properties - tests binding detail section */
export declare const detailBindingNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    fullData: {
      kind: string;
      direction: string;
      source: string;
      target: string;
      address: string;
      properties: {
        routingKey: string;
        arguments: {};
      };
    };
  };
};
/** Entity node with no properties - tests conditional rendering */
export declare const detailEntityNoPropsNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    entityKind: string;
    fullData: {
      name: string;
      address: string;
      kind: string;
      flow: string;
      properties: {};
    };
  };
};
/** Node with no fullData - falls back to node.data */
export declare const detailNoFullDataNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
  };
};
/** Message node - tests the default info-only path */
export declare const detailMessageNode: {
  id: string;
  type: string;
  data: {
    label: string;
    nodeType: string;
    fullData: {
      name: string;
      identity: string;
      runtimeType: string;
    };
  };
};
/** Service with consumers but no inbound/outbound routes */
export declare const noRoutesDiagram: DiagramData;
/** Service with only outbound routes, no inbound */
export declare const outboundOnlyDiagram: DiagramData;
/** Service with only inbound routes, no outbound */
export declare const inboundOnlyDiagram: DiagramData;
