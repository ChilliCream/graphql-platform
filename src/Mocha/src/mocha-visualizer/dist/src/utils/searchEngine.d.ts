import { Node } from "@xyflow/react";
export interface SearchableEntry {
  nodeId: string;
  label: string;
  description?: string;
  category: "consumer" | "saga" | "message" | "endpoint" | "entity" | "route";
  nodeType: string;
  subType?: string;
}
/**
 * Build a flat search index from topology nodes.
 * Produces per-node entries (no dedup by value), includes `entity-*` nodes,
 * and carries node IDs for navigation.
 */
export declare function buildSearchIndex(nodes: Node[]): SearchableEntry[];
