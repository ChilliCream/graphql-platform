import { Edge, Node } from "@xyflow/react";
export interface BundleInfo {
  /** Unique identifier for this bundle (based on corridor) */
  bundleId: string;
  /** This edge's lane index within the bundle (0-based) */
  laneIndex: number;
  /** Total number of lanes in this bundle */
  totalLanes: number;
  /** Offset in pixels for this lane (can be negative for centering) */
  laneOffset: number;
}
export interface BundledEdgeData {
  bundle?: BundleInfo;
  [key: string]: unknown;
}
/**
 * Analyzes edges and assigns bundle/lane information to each edge.
 *
 * @param edges - All edges in the diagram
 * @param nodes - All nodes in the diagram (used for position-based lane ordering)
 * @returns Edges with bundle information added to their data
 */
export declare function assignEdgeBundles(
  edges: Edge[],
  nodes: Node[]
): Edge<BundledEdgeData>[];
/**
 * Gets the lane spacing configuration.
 * Useful for components that need to know the spacing.
 */
export declare function getLaneSpacing(): number;
/**
 * Checks if an edge has bundle information.
 */
export declare function hasBundleInfo(edge: Edge<BundledEdgeData>): boolean;
