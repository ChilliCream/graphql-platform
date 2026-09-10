"use client";

import { createContext, useContext, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

/**
 * Building blocks the Fusion concept prototypes may use or ignore.
 *
 * Stable exports (imported by the ten concept tasks):
 * `useReducedMotionPreference` (re-exported from `src/nitro/lib/motion` so a
 * concept has one import for motion), `Scene`, `useSceneActive`, `ClientChip`,
 * `SubgraphChip`, `SourceChip`, plus the `SubgraphSpec` and `SourceKind`
 * unions.
 *
 * Nothing here is required: a concept that wants a different look draws its
 * own markup. These exist so the shared scene vocabulary (clients -> gateway
 * -> subgraphs with a language and a spec, plus non-GraphQL sources) does not
 * get re-typed ten times.
 */

export { useReducedMotionPreference };

const SceneActiveContext = createContext(false);

/**
 * `true` while the enclosing `Scene` is in the viewport and the tab is
 * visible. For client components rendered inside a `Scene`; outside one it is
 * always `false`.
 */
export function useSceneActive(): boolean {
  return useContext(SceneActiveContext);
}

interface SceneProps {
  /** CSS `aspect-ratio` value for the box, e.g. "16 / 9" or "4 / 3". */
  readonly ratio?: string;
  /** Accessible name; the scene is decorative (`aria-hidden`) without one. */
  readonly label?: string;
  readonly className?: string;
  /**
   * Either plain nodes, or a render function called with `active: true` only
   * while the scene is in the viewport and the tab is visible, so a concept
   * can stop its animation off-screen. `active` stays `false` on the server
   * and on the first client frame.
   *
   * A render function only works when the caller is itself a client component
   * (React cannot pass a function from a server component to a client one). A
   * server component composes `<Scene><MyVisual /></Scene>` instead and reads
   * the flag with `useSceneActive()` inside `MyVisual`.
   */
  readonly children: ReactNode | ((active: boolean) => ReactNode);
}

/**
 * Fixed-ratio, overflow-hidden stage for a concept's animated graphic. It
 * reserves its own height, so the page never shifts while the visual loads,
 * and gates motion with an IntersectionObserver plus the page visibility
 * state. Reduced motion stays the concept's own call: read
 * `useReducedMotionPreference` and render a still frame.
 */
export function Scene({
  ratio = "16 / 9",
  label,
  className,
  children,
}: SceneProps) {
  const ref = useRef<HTMLDivElement>(null);
  const [inView, setInView] = useState(false);
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    const node = ref.current;
    if (!node) return;

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          setInView(entry.isIntersecting);
        }
      },
      { rootMargin: "10% 0px", threshold: 0.15 },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const onChange = () => setVisible(document.visibilityState === "visible");
    onChange();
    document.addEventListener("visibilitychange", onChange);
    return () => document.removeEventListener("visibilitychange", onChange);
  }, []);

  const active = inView && visible;

  return (
    <div
      ref={ref}
      style={{ aspectRatio: ratio }}
      aria-hidden={label ? undefined : true}
      aria-label={label}
      role={label ? "img" : undefined}
      className={`relative w-full overflow-hidden ${className ?? ""}`.trim()}
    >
      <SceneActiveContext.Provider value={active}>
        {typeof children === "function" ? children(active) : children}
      </SceneActiveContext.Provider>
    </div>
  );
}

const CHIP =
  "border-cc-card-border bg-cc-surface text-cc-ink inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs";

const BADGE =
  "text-cc-nav-label font-mono text-[10px] tracking-[0.16em] uppercase";

interface ClientChipProps {
  /** Web, Mobile, Partner API, Agent, ... */
  readonly label: string;
  readonly className?: string;
}

/** One consumer of the composite schema, drawn as a pill. */
export function ClientChip({ label, className }: ClientChipProps) {
  return (
    <span className={`${CHIP} ${className ?? ""}`.trim()}>
      <span
        aria-hidden="true"
        className="bg-cc-ink-dim h-1.5 w-1.5 rounded-full"
      />
      {label}
    </span>
  );
}

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

interface SubgraphChipProps {
  /** Catalog, Billing, Ordering, Shipping, Accounts, ... */
  readonly name: string;
  /** JS/TS, Java, Go, Ruby, Python, C#, ... */
  readonly language: string;
  readonly spec: SubgraphSpec;
  readonly className?: string;
}

/**
 * One GraphQL source schema: its name, the language its server is written in
 * and the federation specification it is written to. Both specs render the
 * same way on purpose - neither is the default.
 */
export function SubgraphChip({
  name,
  language,
  spec,
  className,
}: SubgraphChipProps) {
  return (
    <span className={`${CHIP} ${className ?? ""}`.trim()}>
      <span className="font-medium">{name}</span>
      <span className={BADGE}>{language}</span>
      <span className={BADGE} title={spec}>
        {spec === "Apollo Federation" ? "Apollo Fed" : "GraphQL Fed"}
      </span>
    </span>
  );
}

export type SourceKind = "OpenAPI" | "gRPC";

interface SourceChipProps {
  /** Payments, Inventory, ... */
  readonly name: string;
  readonly kind: SourceKind;
  readonly className?: string;
}

/** A source that is not a GraphQL server but composes into the same schema. */
export function SourceChip({ name, kind, className }: SourceChipProps) {
  return (
    <span
      className={`${CHIP} border-dashed ${className ?? ""}`.trim()}
      data-source-kind={kind}
    >
      <span className="font-medium">{name}</span>
      <span className={BADGE}>{kind}</span>
    </span>
  );
}
