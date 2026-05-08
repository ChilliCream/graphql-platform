"use client";

import React, {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

export type AnchorKind =
  | "pinch"
  | "prism"
  | "merge"
  | "act-top"
  | "act-bottom"
  | "service-exit"
  | "service-entry"
  | "cup-spout"
  | "adapter"
  | "pour-exit";

export interface Anchor {
  // Position in page-relative canvas coordinates. The ConnectorLayer is sized
  // to the page; each act publishes anchors with their absolute (page-level)
  // y so the connector can stitch acts together.
  x: number;
  y: number;
  kind?: AnchorKind;
  // Optional per-anchor data (e.g. line opacity for active/inactive product
  // tabs in Act 2). Keep this small — it's just a tunnel for state that the
  // connector layer needs to render correctly.
  meta?: { opacity?: number; dx?: number };
}

export interface AnchorMap {
  [id: string]: Anchor;
}

interface AnchorContextValue {
  register: (id: string, anchor: Anchor) => void;
  unregister: (id: string) => void;
  anchors: AnchorMap;
}

const AnchorContext = createContext<AnchorContextValue>({
  register: () => {},
  unregister: () => {},
  anchors: {},
});

interface AnchorProviderProps {
  children: ReactNode;
}

export const AnchorProvider: React.FC<AnchorProviderProps> = ({ children }) => {
  const [anchors, setAnchors] = useState<AnchorMap>({});

  const register = useCallback((id: string, anchor: Anchor) => {
    setAnchors((prev) => {
      const existing = prev[id];
      if (
        existing &&
        existing.x === anchor.x &&
        existing.y === anchor.y &&
        existing.kind === anchor.kind &&
        existing.meta?.opacity === anchor.meta?.opacity &&
        existing.meta?.dx === anchor.meta?.dx
      ) {
        return prev;
      }
      return { ...prev, [id]: anchor };
    });
  }, []);

  const unregister = useCallback((id: string) => {
    setAnchors((prev) => {
      if (!(id in prev)) {
        return prev;
      }
      const next = { ...prev };
      delete next[id];
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ register, unregister, anchors }),
    [register, unregister, anchors]
  );

  return (
    <AnchorContext.Provider value={value}>{children}</AnchorContext.Provider>
  );
};

export const useAnchorContext = () => useContext(AnchorContext);

// Convenience hook: register a fixed anchor at canvas-coord (x, y).
// Intended for declared geometry — for DOM-measured anchors, callers should
// use useAnchorContext().register directly inside a useLayoutEffect.
export const useAnchor = (id: string, anchor: Anchor | null) => {
  const { register, unregister } = useAnchorContext();
  React.useEffect(() => {
    if (!anchor) {
      return;
    }
    register(id, anchor);
    return () => unregister(id);
  }, [id, anchor?.x, anchor?.y, anchor?.kind, register, unregister]);
};
