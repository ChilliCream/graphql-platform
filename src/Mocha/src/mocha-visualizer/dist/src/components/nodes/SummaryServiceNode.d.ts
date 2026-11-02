export interface SummaryServiceNodeData {
  label: string;
  consumerCount: number;
  messageCount: number;
  sagaCount: number;
  transportNames: string[];
  /** The ID of the service group this summarizes */
  serviceGroupId: string;
  [key: string]: unknown;
}
export declare function SummaryServiceNode({
  data,
}: {
  data: SummaryServiceNodeData;
}): import("react/jsx-runtime").JSX.Element;
