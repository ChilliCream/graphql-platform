import { createContext, useContext } from "react";

// The scrollable element that wraps the entire page (the styled `Container`
// in main.tsx). Stored as the resolved element (not a RefObject) so callback
// refs in the provider re-render consumers when it mounts — otherwise
// `ref.current` would stay null after first render and never wake the
// listeners attached in descendant effects.
export const ScrollRootContext = createContext<HTMLElement | null>(null);

export const useScrollRoot = (): HTMLElement | null =>
  useContext(ScrollRootContext);
