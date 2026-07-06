/**
 * TabReel, a railway.com-style auto-advancing tab reel. A row of tabs sits above a stage;
 * the ACTIVE tab's background is a progress bar that fills L→R over that tab's duration. When
 * it completes, the reel advances to the next tab and crossfades to that tab's screen. Loops.
 *
 * One master clock runs 0→1 over Σ(tab durations). From it we derive the active tab index
 * (React state) and, per rendered screen, a LOCAL progress 0→1 scoped to that tab's window,
 * so a screen that is crossfading out holds its own final frame instead of snapping back.
 *
 * Verification hooks: `staticTab` + `staticProgress` freeze a specific tab at a specific local
 * progress (no clock), so Playwright can screenshot any beat deterministically.
 */
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  animate,
  AnimatePresence,
  motion,
  useInView,
  useMotionValue,
  useMotionValueEvent,
  useTransform,
  type MotionValue,
} from "motion/react";
import { ease, useReducedMotionPreference } from "../../lib/motion";
import { token } from "../../lib/tokens";

/** The shared design canvas every tab screen is authored in (sidebar+content, with footer). */
export const TABREEL_CANVAS = { w: 1504, h: 940 } as const;

export interface TabScreenProps {
  /** local progress 0→1 across this tab's window (holds at 1 once past). */
  progress: MotionValue<number>;
  active: boolean;
}

export interface TabReelTab {
  id: string;
  label: string;
  icon?: ReactNode;
  /** benefit headline shown (as marketing chrome) while this tab is active */
  headline?: string;
  /** one-line supporting benefit copy under the headline */
  subhead?: string;
  /** how long this tab plays before advancing, ms */
  durationMs: number;
  Screen: (props: TabScreenProps) => ReactNode;
}

export interface TabReelProps {
  tabs: TabReelTab[];
  /** freeze on a specific tab (verification/debug) */
  staticTab?: number;
  /** local progress 0..1 for the frozen tab */
  staticProgress?: number;
  ariaLabel?: string;
  /**
   * Float the phase nav as a pill straddling the bottom edge of the stage
   * (overlapping it ~50%) instead of sitting below it in flow.
   */
  tabsOverlay?: boolean;
}

export function TabReel({
  tabs,
  staticTab,
  staticProgress,
  ariaLabel = "Nitro product reel",
  tabsOverlay = false,
}: TabReelProps) {
  const total = useMemo(
    () => tabs.reduce((s, t) => s + t.durationMs, 0),
    [tabs],
  );
  // cumulative END fraction of each tab in [0,1]
  const ends = useMemo(() => {
    const out: number[] = [];
    let acc = 0;
    for (const t of tabs) {
      acc += t.durationMs / total;
      out.push(acc);
    }
    return out;
  }, [tabs, total]);

  const indexAt = useMemo(
    () => (p: number) => {
      for (let i = 0; i < ends.length; i++) if (p < ends[i] - 1e-6) return i;
      return ends.length - 1;
    },
    [ends],
  );

  const staticMode = staticTab != null;

  // A seekable master clock: 0→1 over Σ(durations). `play(from)` (re)starts it
  // from an arbitrary fraction and then loops, which is what makes the phase tabs
  // clickable, a click seeks the clock to that tab's start.
  const reduced = useReducedMotionPreference();
  const clockRef = useRef<HTMLDivElement>(null);
  const inView = useInView(clockRef, { amount: 0.25 });
  const clockProgress = useMotionValue(reduced ? 1 : 0);
  const controlsRef = useRef<ReturnType<typeof animate> | null>(null);

  const play = useCallback(
    (from: number) => {
      controlsRef.current?.stop();
      if (reduced) {
        clockProgress.set(1);
        return;
      }
      const start = from >= 1 || from < 0 ? 0 : from;
      clockProgress.set(start);
      controlsRef.current = animate(clockProgress, 1, {
        duration: Math.max(0.001, (total / 1000) * (1 - start)),
        ease: "linear",
        onComplete: () => {
          controlsRef.current = animate(clockProgress, [0, 1], {
            duration: total / 1000,
            ease: "linear",
            repeat: Infinity,
            repeatType: "loop",
          });
        },
      });
    },
    [reduced, total, clockProgress],
  );

  useEffect(() => {
    if (staticMode) {
      return;
    }
    if (reduced) {
      clockProgress.set(1);
      return;
    }
    if (!inView) {
      controlsRef.current?.stop();
      return;
    }
    play(clockProgress.get());
    return () => controlsRef.current?.stop();
  }, [staticMode, reduced, inView, play, clockProgress]);

  const [liveIndex, setLiveIndex] = useState(0);
  useMotionValueEvent(clockProgress, "change", (p) => {
    const i = indexAt(p);
    setLiveIndex((prev) => (prev === i ? prev : i));
  });

  const activeIndex = staticMode
    ? Math.min(staticTab!, tabs.length - 1)
    : liveIndex;
  const frozenLocal = useMotionValue(staticProgress ?? 0);

  const selectTab = useCallback(
    (i: number) => {
      if (staticMode) {
        return;
      }
      setLiveIndex(i);
      play(i === 0 ? 0 : ends[i - 1]);
    },
    [staticMode, ends, play],
  );

  return (
    <div
      ref={clockRef}
      role="group"
      aria-label={ariaLabel}
      style={{
        width: "100%",
        display: "flex",
        flexDirection: "column",
        gap: 12,
      }}
    >
      {/* ── benefit headline (marketing chrome, communicates the value of the active tab) ── */}
      {tabs.some((t) => t.headline) && (
        <div style={{ position: "relative", height: 64, textAlign: "center" }}>
          <AnimatePresence>
            <motion.div
              key={activeIndex}
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -6 }}
              transition={{ duration: 0.4, ease: ease.inOut }}
              style={{
                position: "absolute",
                inset: 0,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: 4,
              }}
            >
              <div
                style={{
                  fontSize: 22,
                  fontWeight: 700,
                  color: token.textStrong,
                  letterSpacing: -0.2,
                }}
              >
                {tabs[activeIndex]?.headline}
              </div>
              {tabs[activeIndex]?.subhead && (
                <div
                  style={{
                    fontSize: 14,
                    color: token.textSecondary,
                    maxWidth: 680,
                  }}
                >
                  {tabs[activeIndex]?.subhead}
                </div>
              )}
            </motion.div>
          </AnimatePresence>
        </div>
      )}

      {/* ── stage + phase nav ── */}
      {(() => {
        const stage = (
          <div
            style={{
              position: "relative",
              width: "100%",
              aspectRatio: `${TABREEL_CANVAS.w} / ${TABREEL_CANVAS.h}`,
              borderRadius: 10,
              border: `1px solid ${token.borderStrong}`,
              background: token.bg,
              overflow: "hidden",
              boxShadow: "0 12px 40px rgba(1,4,9,0.45)",
            }}
          >
            <AnimatePresence>
              <motion.div
                key={activeIndex}
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.45, ease: ease.inOut }}
                style={{ position: "absolute", inset: 0 }}
              >
                <ScreenHost
                  tab={tabs[activeIndex]}
                  index={activeIndex}
                  global={clockProgress}
                  ends={ends}
                  staticMode={staticMode}
                  staticLocal={frozenLocal}
                />
              </motion.div>
            </AnimatePresence>
          </div>
        );

        const tabButtons = tabs.map((t, i) => (
          <TabButton
            key={t.id}
            tab={t}
            index={i}
            isActive={i === activeIndex}
            global={clockProgress}
            ends={ends}
            staticMode={staticMode}
            staticLocal={frozenLocal}
            onSelect={() => selectTab(i)}
          />
        ));

        // Overlay: the phase nav floats as a pill straddling the stage's bottom edge.
        if (tabsOverlay) {
          return (
            <div style={{ position: "relative", marginBottom: 26 }}>
              {stage}
              <div
                style={{
                  position: "absolute",
                  left: 0,
                  right: 0,
                  bottom: 0,
                  display: "flex",
                  justifyContent: "center",
                  transform: "translateY(50%)",
                  pointerEvents: "none",
                }}
              >
                <div
                  style={{
                    display: "inline-flex",
                    gap: 8,
                    padding: "6px 8px",
                    borderRadius: 14,
                    background: token.bg,
                    border: `1px solid ${token.borderStrong}`,
                    boxShadow: "0 12px 34px rgba(1,4,9,0.6)",
                    backdropFilter: "blur(6px)",
                    pointerEvents: "auto",
                  }}
                >
                  {tabButtons}
                </div>
              </div>
            </div>
          );
        }

        // Default: the phase nav sits below the stage, in flow (railway.com style).
        return (
          <>
            {stage}
            <div
              style={{
                display: "flex",
                justifyContent: "center",
                gap: 8,
                flexWrap: "wrap",
                padding: "2px 0",
              }}
            >
              {tabButtons}
            </div>
          </>
        );
      })()}
    </div>
  );
}

/** local progress 0→1 for tab `index`, clamped (1 once the global clock is past this tab). */
function localFor(p: number, index: number, ends: number[]): number {
  const start = index === 0 ? 0 : ends[index - 1];
  const end = ends[index];
  const span = end - start || 1;
  const v = (p - start) / span;
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

function TabButton({
  tab,
  index,
  isActive,
  global,
  ends,
  staticMode,
  staticLocal,
  onSelect,
}: {
  tab: TabReelTab;
  index: number;
  isActive: boolean;
  global: MotionValue<number>;
  ends: number[];
  staticMode: boolean;
  staticLocal: MotionValue<number>;
  onSelect?: () => void;
}) {
  // fill tracks THIS tab's local progress only while it is the active tab.
  const zero = useMotionValue(0);
  const fromGlobal = useTransform(global, (p) =>
    isActive ? localFor(p, index, ends) : 0,
  );
  const fill = staticMode ? (isActive ? staticLocal : zero) : fromGlobal;

  return (
    <div
      role="button"
      tabIndex={0}
      aria-label={`Show ${tab.label}`}
      aria-pressed={isActive}
      onClick={onSelect}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onSelect?.();
        }
      }}
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
        gap: 8,
        height: 34,
        padding: "0 16px",
        borderRadius: 8,
        overflow: "hidden",
        cursor: "pointer",
        userSelect: "none",
        background: isActive ? token.surface : "transparent",
        border: `1px solid ${isActive ? token.border : "transparent"}`,
        color: isActive ? token.textStrong : token.textSecondary,
        transition: "color 0.2s ease",
      }}
    >
      {/* progress fill */}
      <motion.div
        aria-hidden
        style={{
          position: "absolute",
          inset: 0,
          background: token.highlight,
          opacity: 0.5,
          transformOrigin: "left",
          scaleX: fill,
        }}
      />
      <span
        style={{
          position: "relative",
          display: "flex",
          alignItems: "center",
          color: isActive ? token.active : "currentColor",
        }}
      >
        {tab.icon}
      </span>
      <span
        style={{
          position: "relative",
          fontSize: 13,
          fontWeight: isActive ? 600 : 500,
          whiteSpace: "nowrap",
        }}
      >
        {tab.label}
      </span>
    </div>
  );
}

function ScreenHost({
  tab,
  index,
  global,
  ends,
  staticMode,
  staticLocal,
}: {
  tab: TabReelTab;
  index: number;
  global: MotionValue<number>;
  ends: number[];
  staticMode: boolean;
  staticLocal: MotionValue<number>;
}) {
  const fromGlobal = useTransform(global, (p) => localFor(p, index, ends));
  const local = staticMode ? staticLocal : fromGlobal;
  const { Screen } = tab;
  return <Screen progress={local} active />;
}
