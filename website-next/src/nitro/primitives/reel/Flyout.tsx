/**
 * Flyout — the right-side detail drawer the Nitro IDE slides in when you click a span / node /
 * operation (Span Details, Plan Details, etc.). Slides in from the right + fades, driven by a
 * local `progress` MotionValue with show/hide fractions. Header (title + "{i} of {n}" counter +
 * up/down steppers + close) and an optional underline tab strip; children are the scrollable body.
 */
import { useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { token } from "../../lib/tokens";

const Chev = ({ up }: { up?: boolean }) => (
  <svg
    width={14}
    height={14}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth={1.8}
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d={up ? "M6 15l6-6 6 6" : "M6 9l6 6 6-6"} />
  </svg>
);

/** Animated active-tab transition: slide the underline from `from` to `to` over `range`. */
export interface TabSlide {
  from: string;
  to: string;
  range: [number, number];
}

export interface FlyoutProps {
  progress: MotionValue<number>;
  show: number;
  hide?: number;
  width?: number;
  title: string;
  counter?: string;
  tabs?: string[];
  activeTab?: string;
  /** color of the active-tab underline indicator (defaults to token.active) */
  indicatorColor?: string;
  /** drive the active tab + sliding underline off progress (from → to over range) */
  tabSlide?: TabSlide;
  children: ReactNode;
}

export function Flyout({
  progress,
  show,
  hide = 1.01,
  width = 444,
  title,
  counter,
  tabs,
  activeTab,
  indicatorColor = token.active,
  tabSlide,
  children,
}: FlyoutProps) {
  // A real drawer slides in OPAQUE — so the panel snaps to full opacity almost instantly (its solid
  // background never goes see-through) and the SLIDE is the entrance. Kept short so it appears fast.
  const SLIDE = 0.02;
  const x = useTransform(
    progress,
    [show, show + SLIDE, hide - SLIDE, hide],
    [width, 0, 0, width],
    { clamp: true },
  );
  const opacity = useTransform(
    progress,
    [show, show + 0.004, hide - 0.004, hide],
    [0, 1, 1, 0],
    { clamp: true },
  );

  return (
    <motion.div
      data-testid="reel-flyout"
      data-flyout-title={title}
      style={{
        position: "absolute",
        top: 0,
        right: 0,
        bottom: 0,
        width,
        x,
        opacity,
        background: token.surface,
        borderLeft: `1px solid ${token.border}`,
        boxShadow: "-12px 0 32px rgba(1,4,9,0.45)",
        display: "flex",
        flexDirection: "column",
        zIndex: 30,
      }}
    >
      {/* header */}
      <div
        style={{
          height: 48,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "0 12px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <span
          style={{ fontSize: 15, fontWeight: 600, color: token.textStrong }}
        >
          {title}
        </span>
        {counter && (
          <span style={{ marginLeft: "auto", fontSize: 12, color: token.text }}>
            {counter}
          </span>
        )}
        <span
          style={{
            marginLeft: counter ? 8 : "auto",
            display: "flex",
            gap: 4,
            color: token.textSecondary,
          }}
        >
          <Chev up />
          <Chev />
        </span>
        <span style={{ color: token.textSecondary, marginLeft: 4 }}>
          <svg
            width={14}
            height={14}
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth={1.8}
            strokeLinecap="round"
          >
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </span>
      </div>

      {/* tab strip */}
      {tabs &&
        (tabSlide ? (
          <SlidingTabStrip
            progress={progress}
            tabs={tabs}
            indicatorColor={indicatorColor}
            tabSlide={tabSlide}
          />
        ) : (
          <div
            style={{
              height: 36,
              flex: "0 0 auto",
              display: "flex",
              alignItems: "stretch",
              gap: 0,
              padding: "0 12px",
              borderBottom: `1px solid ${token.border}`,
            }}
          >
            {tabs.map((t) => {
              const on = t === activeTab;
              return (
                <span
                  key={t}
                  style={{
                    position: "relative",
                    display: "flex",
                    alignItems: "center",
                    padding: "0 12px",
                    fontSize: 13,
                    color: on ? token.textStrong : token.textSecondary,
                  }}
                >
                  {t}
                  {on && (
                    <span
                      style={{
                        position: "absolute",
                        left: 6,
                        right: 6,
                        bottom: 0,
                        height: 2,
                        background: indicatorColor,
                      }}
                    />
                  )}
                </span>
              );
            })}
          </div>
        ))}

      {/* body */}
      <div style={{ flex: 1, minHeight: 0, overflow: "hidden", padding: 14 }}>
        {children}
      </div>
    </motion.div>
  );
}

interface Slot {
  left: number;
  width: number;
}

/**
 * Tab strip whose active underline slides from one tab to another over `tabSlide.range`,
 * with the from/to labels crossfading their text color at the range midpoint. Tab geometry is
 * measured from the DOM so the underline travels to the real label positions.
 */
function SlidingTabStrip({
  progress,
  tabs,
  indicatorColor,
  tabSlide,
}: {
  progress: MotionValue<number>;
  tabs: string[];
  indicatorColor: string;
  tabSlide: TabSlide;
}) {
  const refs = useRef<(HTMLSpanElement | null)[]>([]);
  const [slots, setSlots] = useState<Slot[]>([]);

  useLayoutEffect(() => {
    setSlots(
      tabs.map((_, i) => {
        const el = refs.current[i];
        return el
          ? { left: el.offsetLeft, width: el.offsetWidth }
          : { left: 0, width: 0 };
      }),
    );
  }, [tabs]);

  const fromIdx = Math.max(0, tabs.indexOf(tabSlide.from));
  const toIdx = Math.max(0, tabs.indexOf(tabSlide.to));
  const cross = (tabSlide.range[0] + tabSlide.range[1]) / 2;
  const f = useTransform(
    progress,
    [tabSlide.range[0], tabSlide.range[1]],
    [0, 1],
    { clamp: true },
  );

  const a = slots[fromIdx];
  const b = slots[toIdx];
  const ready = !!a && !!b && (a.width > 0 || b.width > 0);
  const indLeft = useTransform(f, (v) =>
    ready ? a.left + 6 + (b.left - a.left) * v : 0,
  );
  const indWidth = useTransform(f, (v) =>
    ready ? a.width - 12 + (b.width - a.width) * v : 0,
  );

  return (
    <div
      style={{
        position: "relative",
        height: 36,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "stretch",
        gap: 0,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      {tabs.map((t, i) => (
        <SlideLabel
          key={t}
          ref={(el) => {
            refs.current[i] = el;
          }}
          progress={progress}
          label={t}
          isFrom={t === tabSlide.from}
          isTo={t === tabSlide.to}
          cross={cross}
        />
      ))}
      {ready && (
        <motion.span
          style={{
            position: "absolute",
            bottom: 0,
            height: 2,
            left: indLeft,
            width: indWidth,
            background: indicatorColor,
          }}
        />
      )}
    </div>
  );
}

const SlideLabel = ({
  ref,
  progress,
  label,
  isFrom,
  isTo,
  cross,
}: {
  ref: (el: HTMLSpanElement | null) => void;
  progress: MotionValue<number>;
  label: string;
  isFrom: boolean;
  isTo: boolean;
  cross: number;
}) => {
  const color = useTransform(progress, (p) => {
    if (isTo) return p >= cross ? token.textStrong : token.textSecondary;
    if (isFrom) return p < cross ? token.textStrong : token.textSecondary;
    return token.textSecondary;
  });
  return (
    <motion.span
      ref={ref}
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
        padding: "0 12px",
        fontSize: 13,
        color,
      }}
    >
      {label}
    </motion.span>
  );
};
