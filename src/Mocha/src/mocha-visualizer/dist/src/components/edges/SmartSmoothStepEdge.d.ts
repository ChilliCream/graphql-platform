import { PathfindingConfig } from "../../utils/pathfinding";
import { BundleInfo } from "../../utils/edgeBundling";
export interface SmartSmoothStepEdgeData {
  /** Custom pathfinding configuration */
  pathfindingConfig?: Partial<PathfindingConfig>;
  /** Bundle information for edge grouping/lane offset */
  bundle?: BundleInfo;
}
/**
 * Props passed to edge components by React Flow.
 * We define our own interface to ensure type safety while remaining
 * compatible with React Flow's edge type system.
 */
interface SmartEdgeProps {
  id: string;
  source: string;
  target: string;
  sourceHandleId?: string | null;
  targetHandleId?: string | null;
  style?: React.CSSProperties;
  markerStart?: string;
  markerEnd?: string;
  data?: SmartSmoothStepEdgeData;
  label?: React.ReactNode;
  labelStyle?: React.CSSProperties;
  labelShowBg?: boolean;
  labelBgStyle?: React.CSSProperties;
  labelBgPadding?: number | [number, number];
  labelBgBorderRadius?: number;
}
/**
 * Smart Smooth Step Edge Component
 *
 * This edge uses A* pathfinding to route around nodes while maintaining
 * the smooth step aesthetic (orthogonal lines with rounded corners).
 *
 * RENDERING PIPELINE:
 * 1. Get source and target node information
 * 2. Calculate handle positions based on handle IDs
 * 3. Get all nodes from the store for obstacle detection
 * 4. Run A* pathfinding to find optimal route
 * 5. Generate SVG path from waypoints
 * 6. Render using BaseEdge (handles markers, interactions, etc.)
 */
export declare function SmartSmoothStepEdge(
  props: SmartEdgeProps
): import("react/jsx-runtime").JSX.Element | null;
export default SmartSmoothStepEdge;
