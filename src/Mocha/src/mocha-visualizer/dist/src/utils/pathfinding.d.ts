/**
 * =============================================================================
 * SMART EDGE PATHFINDING (OPTIMIZED)
 * =============================================================================
 *
 * High-performance A* pathfinding for routing edges around nodes.
 *
 * OPTIMIZATIONS:
 * - Binary heap priority queue: O(log n) insert/extract vs O(n) linear search
 * - Map-based open set tracking: O(1) lookup vs O(n) findIndex
 * - Larger grid cells (25px): Fewer nodes to explore
 * - Iteration limit: Prevents hanging on complex layouts
 * - Early termination: Simple paths skip A* entirely
 */
/** A 2D point in canvas coordinates */
export interface Point {
  x: number;
  y: number;
}
/**
 * Minimal node interface for pathfinding.
 */
export interface PathfindingNode {
  id: string;
  type?: string;
  position: {
    x: number;
    y: number;
  };
  width?: number | null;
  height?: number | null;
}
/** Configuration for the pathfinding algorithm */
export interface PathfindingConfig {
  gridCellSize: number;
  nodePadding: number;
  borderRadius: number;
  graphPadding: number;
}
export declare const DEFAULT_CONFIG: PathfindingConfig;
/**
 * Finds a path from source to target that avoids all obstacle nodes.
 */
export declare function findSmartPath(
  source: Point,
  target: Point,
  nodes: PathfindingNode[],
  config?: PathfindingConfig
): Point[] | null;
export declare function generateSmoothStepPath(
  points: Point[],
  borderRadius?: number
): string;
export declare function generateFallbackPath(
  source: Point,
  target: Point
): string;
export declare function applyLaneOffset(
  point: Point,
  handlePosition: "left" | "right" | "top" | "bottom",
  offset: number
): Point;
