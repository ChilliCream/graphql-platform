import { Saga } from "../types/diagram";
interface SagaListBarProps {
  sagas: Saga[];
  onSagaClick: (saga: Saga) => void;
  /** Offset from bottom (e.g. to sit above TraceTimeline) */
  bottomOffset?: number;
}
export declare function SagaListBar({
  sagas,
  onSagaClick,
  bottomOffset,
}: SagaListBarProps): import("react/jsx-runtime").JSX.Element | null;
export {};
