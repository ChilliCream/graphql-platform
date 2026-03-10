import { Point } from "../../utils/pathfinding";
export interface ElkRoutedEdgeData {
  /** Pre-computed SVG path string from ELK edge sections */
  elkPath?: string;
  /** Raw absolute points from ELK (start → bendpoints → end) */
  elkPoints?: Point[];
}
interface ElkRoutedEdgeProps {
  id: string;
  style?: React.CSSProperties;
  markerStart?: string;
  markerEnd?: string;
  data?: ElkRoutedEdgeData;
  sourceX: number;
  sourceY: number;
  targetX: number;
  targetY: number;
}
export declare function ElkRoutedEdge({
  id,
  style,
  markerStart,
  markerEnd,
  data,
  sourceX,
  sourceY,
  targetX,
  targetY,
}: ElkRoutedEdgeProps): import("react/jsx-runtime").JSX.Element;
export {};
