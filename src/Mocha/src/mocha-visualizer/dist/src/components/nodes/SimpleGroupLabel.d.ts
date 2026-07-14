export interface SimpleGroupLabelData {
  label: string;
  type: "service" | "transport";
  [key: string]: unknown;
}
export declare function SimpleGroupLabel({
  data,
}: {
  data: SimpleGroupLabelData;
}): import("react/jsx-runtime").JSX.Element;
