import { Node, Edge } from "@xyflow/react";
export declare function layoutTopologyWithElk(
  nodes: Node[],
  edges: Edge[]
): Promise<{
  nodes: Node[];
  edges: Edge[];
}>;
