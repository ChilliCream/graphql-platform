"use client";

import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
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

// Options for useMeasuredAnchor — lets callers tweak which point of the
// element's bounding box becomes the anchor position, or override the meta
// payload sent through with each (re-)registration.
export interface UseMeasuredAnchorOptions {
  // Where on the element's bounding box the anchor sits, expressed as
  // fractions of the box. Default: { x: 0.5, y: 0.5 } (center).
  anchorOrigin?: { x: number; y: number };
  // Pixel offsets added to the resolved position (post-origin), useful for
  // tail/tip offsets such as cup spouts.
  offset?: { dx?: number; dy?: number };
  // Optional meta forwarded onto every registration of this anchor.
  meta?: Anchor["meta"];
  // Extra dependencies that should cause a re-measure even when the ref
  // and root haven't resized (e.g. a tab change inside a panel).
  deps?: ReadonlyArray<unknown>;
  // When false, the hook does nothing (used when an anchor element is
  // conditionally rendered).
  enabled?: boolean;
}

// useMeasuredAnchor — measures `ref.current.getBoundingClientRect()` relative
// to the nearest `[data-cc-landing-root]` ancestor and registers the result
// under `name`. Re-measures on:
//   - ResizeObserver on the ref's element
//   - ResizeObserver on `[data-cc-landing-root]`
//   - scroll on `.main__Container-sc-d4365469-0`
//   - scroll/resize on the window
//   - one delayed re-measure after mount (font-load shift)
// Unregisters on unmount.
export const useMeasuredAnchor = <T extends Element>(
  name: string,
  ref: RefObject<T | null>,
  kind?: AnchorKind,
  opts?: UseMeasuredAnchorOptions
) => {
  const { register, unregister } = useAnchorContext();

  // Latch options into a ref so dep churn on inline `opts` objects doesn't
  // restart the effect. Geometry (anchorOrigin/offset/meta) reads fresh on
  // every measure call.
  const optsRef = useRef(opts);
  optsRef.current = opts;

  // Re-run effect on explicit user-supplied deps (e.g. active tab changes
  // that shift a measured DOM node).
  const depsKey = (opts?.deps ?? []).join("|");
  // The kind never changes per call site in practice; include it in deps so
  // a hot-reload that swaps kinds still re-registers cleanly.
  const enabled = opts?.enabled !== false;

  useEffect(() => {
    if (!enabled) {
      return;
    }
    const el = ref.current;
    if (!el) {
      return;
    }

    const findRoot = (): HTMLElement | null => {
      let node: Element | null = el;
      while (node && node !== document.documentElement) {
        if (
          node instanceof HTMLElement &&
          node.hasAttribute("data-cc-landing-root")
        ) {
          return node;
        }
        node = node.parentElement;
      }
      return document.querySelector(
        "[data-cc-landing-root]"
      ) as HTMLElement | null;
    };

    const measure = () => {
      const node = ref.current;
      if (!node) {
        return;
      }
      const root = findRoot();
      if (!root) {
        return;
      }
      const rRect = root.getBoundingClientRect();
      const eRect = node.getBoundingClientRect();
      const o = optsRef.current;
      const ax = o?.anchorOrigin?.x ?? 0.5;
      const ay = o?.anchorOrigin?.y ?? 0.5;
      const dx = o?.offset?.dx ?? 0;
      const dy = o?.offset?.dy ?? 0;
      const x = eRect.left - rRect.left + eRect.width * ax + dx;
      const y = eRect.top - rRect.top + eRect.height * ay + dy;
      register(name, { x, y, kind, meta: o?.meta });
    };

    measure();

    const root = findRoot();
    let elRO: ResizeObserver | null = null;
    let rootRO: ResizeObserver | null = null;
    if (typeof ResizeObserver !== "undefined") {
      elRO = new ResizeObserver(measure);
      elRO.observe(el);
      if (root) {
        rootRO = new ResizeObserver(measure);
        rootRO.observe(root);
      }
    }
    const scrollEl = document.querySelector(
      ".main__Container-sc-d4365469-0"
    ) as HTMLElement | null;
    scrollEl?.addEventListener("scroll", measure, { passive: true });
    window.addEventListener("scroll", measure, { passive: true });
    window.addEventListener("resize", measure);
    const fontShiftTimer = window.setTimeout(measure, 250);

    return () => {
      elRO?.disconnect();
      rootRO?.disconnect();
      scrollEl?.removeEventListener("scroll", measure);
      window.removeEventListener("scroll", measure);
      window.removeEventListener("resize", measure);
      window.clearTimeout(fontShiftTimer);
      unregister(name);
    };
    // depsKey collapses the user-supplied deps array into a single primitive.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [name, kind, ref, register, unregister, enabled, depsKey]);
};
