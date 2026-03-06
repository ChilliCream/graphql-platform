import { Node, Edge } from "@xyflow/react";
import { SagaState } from "../types/diagram";
export interface FocusStateNodeData extends Record<string, unknown> {
  label: string;
  isInitial: boolean;
  isFinal: boolean;
  response?: string;
  sendActions: string[];
}
export interface FocusTransitionEdgeData extends Record<string, unknown> {
  eventType: string;
  transitionKind: "request" | "reply" | "event";
  sendActions: string[];
  sourceStateName: string;
  transitionIndex: number;
}
export declare function layoutSagaFocus(states: SagaState[]): Promise<{
  nodes: Node[];
  edges: Edge[];
}>;
