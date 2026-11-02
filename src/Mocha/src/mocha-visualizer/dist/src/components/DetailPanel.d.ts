import { Saga } from "../types/diagram";
interface DetailPanelProps {
  node: {
    id: string;
    type?: string;
    data: Record<string, unknown>;
  } | null;
  onClose: () => void;
  onFocusSaga?: (saga: Saga) => void;
  /** When false, the panel is hidden even if a node is selected. Default: true when node is set. */
  open?: boolean;
  /** When true, renders inline without absolute positioning, shadow, or animation. */
  embedded?: boolean;
}
export declare function DetailPanel({
  node,
  onClose,
  onFocusSaga,
  open,
  embedded,
}: DetailPanelProps): import("react/jsx-runtime").JSX.Element | null;
export {};
