import { Node, Edge } from "@xyflow/react";
import { MessageTrace, MessageActivity } from "../types/trace";
import { DiagramData } from "../types/diagram";
export interface TraceTopologyMapping {
  /** Node IDs directly observed in trace activities */
  observedNodeIds: Set<string>;
  /** Node IDs inferred from transport topology (exchanges, bindings, queues between dispatch and receive) */
  inferredNodeIds: Set<string>;
  /** All node IDs on the trace path (observed + inferred) */
  nodeIds: Set<string>;
  /** Edge IDs on the trace path */
  edgeIds: Set<string>;
  /** Maps activity ID to its corresponding topology node ID */
  activityToNodeId: Map<string, string>;
  /** Maps topology node ID to its activities */
  nodeIdToActivities: Map<string, MessageActivity[]>;
  /** Ordered list of node IDs representing the trace sequence */
  sequenceNodeIds: string[];
  /** Nodes with errors */
  errorNodeIds: Set<string>;
}
/**
 * Maps a MessageTrace's activities to topology node IDs.
 * Returns the set of node IDs and edge IDs on the trace path,
 * including inferred transport-internal steps.
 */
export declare function mapTraceToTopology(
  trace: MessageTrace,
  data: DiagramData,
  nodes: Node[],
  edges: Edge[]
): TraceTopologyMapping;
