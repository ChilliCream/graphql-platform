"use client";

import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from "react";

// The landing root element (the scroll-relative coordinate origin every Act's
// anchors are expressed in). Stored as state so a callback-ref assignment
// re-renders consumers — a plain RefObject wouldn't, leaving Acts stuck with
// a null root on first render.
const LandingRootContext = createContext<HTMLElement | null>(null);

export const useLandingRoot = (): HTMLElement | null =>
  useContext(LandingRootContext);

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
  root: HTMLElement | null;
  children: ReactNode;
}

export const AnchorProvider: React.FC<AnchorProviderProps> = ({
  root,
  children,
}) => {
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
    <LandingRootContext.Provider value={root}>
      <AnchorContext.Provider value={value}>{children}</AnchorContext.Provider>
    </LandingRootContext.Provider>
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

// useMeasureEffect — runs `measure` once on mount and again whenever the
// geometry can shift:
//   - ResizeObserver on each of the supplied refs (and the landing root)
//   - resize on the window (covers viewport changes ResizeObserver may miss
//     when the root is full-width and the body shrinks behind a media query)
//   - one delayed re-measure after mount to absorb font-load shift
//
// Note: there is intentionally NO scroll listener. Anchor positions are
// expressed in landing-root-relative coordinates; scrolling shifts the
// element and the root by the same delta, so the difference is invariant.
// The ConnectorLayer's SVG lives inside the same root, so it scrolls along
// with the anchors and doesn't need to be re-drawn on scroll either.
//
// The latest `measure` closure is held in a ref so callers don't need to
// memoise it; the effect itself re-runs only when `deps` changes.
export const useMeasureEffect = (
  measure: () => void,
  observedRefs: ReadonlyArray<RefObject<Element | null>>,
  deps: ReadonlyArray<unknown> = []
) => {
  const root = useContext(LandingRootContext);
  const measureRef = useRef(measure);
  measureRef.current = measure;

  useLayoutEffect(() => {
    const run = () => measureRef.current();
    run();

    const observers: ResizeObserver[] = [];
    if (typeof ResizeObserver !== "undefined") {
      for (const ref of observedRefs) {
        const el = ref.current;
        if (el) {
          const ro = new ResizeObserver(run);
          ro.observe(el);
          observers.push(ro);
        }
      }
      if (root) {
        const ro = new ResizeObserver(run);
        ro.observe(root);
        observers.push(ro);
      }
    }
    window.addEventListener("resize", run);
    const fontShiftTimer = window.setTimeout(run, 250);

    return () => {
      for (const ro of observers) {
        ro.disconnect();
      }
      window.removeEventListener("resize", run);
      window.clearTimeout(fontShiftTimer);
    };
    // The provided `deps` plus `root` together control when measurement is
    // (re)wired up — `root` flips from null to the actual element when its
    // callback ref runs, so the effect must re-run to register observers.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [root, ...deps]);
};

// useMeasuredAnchor — measures `ref.current.getBoundingClientRect()` relative
// to the landing root and registers the result under `name`. Unregisters on
// unmount.
export const useMeasuredAnchor = <T extends Element>(
  name: string,
  ref: RefObject<T | null>,
  kind?: AnchorKind,
  opts?: UseMeasuredAnchorOptions
) => {
  const { register, unregister } = useAnchorContext();
  const root = useContext(LandingRootContext);
  const optsRef = useRef(opts);
  optsRef.current = opts;

  const enabled = opts?.enabled !== false;
  const depsKey = (opts?.deps ?? []).join("|");

  useMeasureEffect(
    () => {
      if (!enabled) {
        return;
      }
      const node = ref.current;
      if (!node || !root) {
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
    },
    [ref],
    [name, kind, ref, register, enabled, depsKey]
  );

  useEffect(() => () => unregister(name), [name, unregister]);
};
