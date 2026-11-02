export interface SummaryTransportNodeData {
  label: string;
  /** Entity counts by kind, e.g. { exchange: 4, queue: 8 } */
  entityCounts: Record<string, number>;
  totalEntityCount: number;
  /** The ID of the transport group this summarizes */
  transportGroupId: string;
  [key: string]: unknown;
}
export declare function SummaryTransportNode({
  data,
}: {
  data: SummaryTransportNodeData;
}): import("react/jsx-runtime").JSX.Element;
