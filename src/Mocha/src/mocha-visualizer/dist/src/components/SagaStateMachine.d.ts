interface SagaState {
  name: string;
  isInitial?: boolean;
  isFinal?: boolean;
  response?: {
    eventType: string;
  };
  transitions: Array<{
    eventType: string;
    transitionTo: string;
    transitionKind?: string;
    send?: Array<{
      messageType: string;
    }>;
  }>;
}
interface SagaStateMachineProps {
  states: SagaState[];
  mode?: "compact" | "focus";
  onStateClick?: (stateName: string) => void;
  onTransitionClick?: (sourceName: string, transitionIndex: number) => void;
}
export declare function SagaStateMachine({
  states,
  mode,
  onStateClick,
  onTransitionClick,
}: SagaStateMachineProps): import("react/jsx-runtime").JSX.Element;
export {};
