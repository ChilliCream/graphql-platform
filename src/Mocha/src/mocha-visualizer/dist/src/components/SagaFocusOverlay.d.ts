import { Saga } from "../types/diagram";
interface SagaFocusOverlayProps {
  saga: Saga;
  onClose: () => void;
}
export declare function SagaFocusOverlay({
  saga,
  onClose,
}: SagaFocusOverlayProps): import("react/jsx-runtime").JSX.Element;
export {};
