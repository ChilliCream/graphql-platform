export interface CompactNodeData {
  label: string;
  nodeType: "consumer" | "saga" | "message" | "endpoint" | "entity";
  subType?: "receive" | "dispatch";
  /** The kind of entity from the transport topology (e.g., 'exchange', 'queue', 'topic') */
  entityKind?: string;
  /** Whether this consumer is a batch handler */
  isBatch?: boolean;
  fullData?: Record<string, unknown>;
  [key: string]: unknown;
}
export declare function CompactNode({
  data,
}: {
  data: CompactNodeData;
}): import("react/jsx-runtime").JSX.Element;
