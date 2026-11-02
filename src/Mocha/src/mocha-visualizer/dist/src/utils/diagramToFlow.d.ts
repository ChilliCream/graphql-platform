import { Node, Edge } from "@xyflow/react";
import { DiagramData } from "../types/diagram";
export interface FlowElements {
  nodes: Node[];
  edges: Edge[];
}
/**
 * Transforms diagram data into React Flow nodes and edges.
 * This is transport-agnostic - it works with any transport type.
 *
 * Data structure:
 * - services[]: application config (host, messageTypes, consumers, routes, sagas)
 * - transports[]: infrastructure config (endpoints, topology)
 */
export declare function diagramToFlow(data: DiagramData): FlowElements;
/**
 * Parses a transport address to create a unique entity ID.
 *
 * Addresses follow the pattern: scheme://host/prefix/name
 * Common prefixes include:
 * - /e/ for exchanges
 * - /q/ for queues
 * - /t/ for topics
 *
 * This function is transport-agnostic - it extracts the prefix and name
 * without making assumptions about what they represent.
 */
export declare function addressToEntityId(address: string): string | null;
