export interface SimpleRouteNodeData {
  label: string;
  kind: string;
  direction?: "inbound" | "outbound";
  /** For binding nodes in topology */
  nodeType?: "route" | "binding";
  fullData?: Record<string, unknown>;
  [key: string]: unknown;
}
export declare function SimpleRouteNode({
  data,
}: {
  data: SimpleRouteNodeData;
}): import("react/jsx-runtime").JSX.Element;
