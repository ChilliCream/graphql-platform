import { useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { token } from "../../lib/tokens";
import { StrokeIcon } from "../../primitives/reel/StrokeIcon";
import { Chevron } from "../../primitives/reel/Chevron";
import { UnderlineTab } from "../UnderlineTab";

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
  indicatorColor?: string;
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
          <Chevron up />
          <Chevron />
        </span>
        <span style={{ color: token.textSecondary, marginLeft: 4 }}>
          <StrokeIcon d="M6 6l12 12M18 6L6 18" size={14} strokeWidth={1.8} />
        </span>
      </div>

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
            {tabs.map((t) => (
              <UnderlineTab
                key={t}
                label={t}
                active={t === activeTab}
                fontSize={13}
                height="100%"
                color={indicatorColor}
                underlineOffset={0}
                underlineInset={6}
                style={{ padding: "0 12px" }}
              />
            ))}
          </div>
        ))}

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
