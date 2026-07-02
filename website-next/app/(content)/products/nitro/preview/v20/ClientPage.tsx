"use client";

import {
  motion,
  type MotionValue,
  useMotionValue,
  useReducedMotion,
  useScroll,
  useTransform,
} from "motion/react";
import { createContext, useContext, useEffect, useRef, useState } from "react";
import type { CSSProperties, ReactNode, RefObject } from "react";

import { ControlPlaneConsole } from "@/src/components/nitro/ControlPlaneConsole";
import { RisingParticles } from "@/src/components/nitro/RisingParticles";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  BarSeries,
  ChartPanel,
  CountUp,
  HBarSeries,
  InsightsTable,
  LineAreaChart,
  NitroCompose,
  NitroDiagnose,
  NitroFusion,
  NitroReel,
  NitroSchema,
  NitroTheme,
  NitroTrace,
  Sparkline,
  token,
  TraceWaterfall,
} from "@/src/nitro";
import type { Client, InsightRow, Trace } from "@/src/nitro/lib/data/types";

// Brand spectrum gradient. Used sparingly as the single "color event" per Linear.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

/* ────────────────────────────────────────────────────────────────────────
   The signal line

   One continuous, unbroken thread of light runs from the top of the page to the
   foot on brand navy. It carries a single GraphQL operation from the hero
   through every station (Observe, Trace, Diagnose, Evolve, Compose). Its colour
   is a restrained slice of the landing-page hero gradient, cool cyan at the top
   easing through periwinkle to a warm coral at the foot, so it reads as one
   elegant hue event rather than a rainbow shift. The line is what lights the
   faint background grid: the grid is bright right beside the thread and fades
   out smoothly with horizontal distance, dark where the light does not reach. A
   scroll-linked pulse marks how far the operation has travelled.
   ──────────────────────────────────────────────────────────────────────── */

// Hero-gradient anchor hues, top -> bottom of the line (cyan -> periwinkle ->
// coral). A restrained subset of the landing hero gradient, no green.
const RAIL_CYAN = "22, 185, 228"; // #16B9E4
const RAIL_PERIWINKLE = "124, 146, 198"; // #7C92C6
const RAIL_CORAL = "240, 120, 106"; // #F0786A

// Sample the cyan -> periwinkle -> coral gradient at a vertical fraction
// (0 = top). Lets a station node glow in the same hue as the line where it sits.
function railHueAt(fraction: number): readonly [number, number, number] {
  const stops: readonly (readonly [number, number, number])[] = [
    [22, 185, 228],
    [124, 146, 198],
    [240, 120, 106],
  ];
  const f = Math.max(0, Math.min(1, fraction)) * (stops.length - 1);
  const i = Math.min(stops.length - 2, Math.floor(f));
  const t = f - i;
  const a = stops[i];
  const b = stops[i + 1];
  return [
    Math.round(a[0] + (b[0] - a[0]) * t),
    Math.round(a[1] + (b[1] - a[1]) * t),
    Math.round(a[2] + (b[2] - a[2]) * t),
  ];
}

// The thread's x, kept identical for the full-page line, the grid, and every
// node. Viewport-left anchored so the single background field and the
// section-level station nodes share one axis. Sits near the copy's left edge
// (~17% of a 1440 viewport) so the line reads as the spine of the narrative.
const RAIL_LINE = "left-[52px] md:left-[176px]";
// The same axis as a raw length, for the full-bleed background field's gradients
// and masks. The field spans the whole viewport, so this is a viewport-x that
// resolves to the content column's left + 176px on desktop widths (matching the
// station nodes), clamped so the line never crosses under the copy on narrower
// screens.
const RAIL_X = "max(224px, 50vw - 464px)";
// Where station labels start, a hair right of the thread so they clear the node.
const RAIL_LABEL = "left-[72px] md:left-[196px]";
// Left indent so section content clears the thread and its nodes with a clear,
// comfortable gap. Wide enough that copy starts well to the right of the line
// and its station labels, so the thread never crowds the text.
const RAIL_INDENT = "pl-[4.5rem] md:pl-[17rem]";

interface RailContextValue {
  readonly scrollYProgress: MotionValue<number>;
  readonly railRef: RefObject<HTMLDivElement | null>;
  readonly reduced: boolean;
}

const RailContext = createContext<RailContextValue | null>(null);

interface SignalLineProps {
  readonly reduced: boolean;
  readonly pulseTop: MotionValue<string>;
  readonly fillScaleY: MotionValue<number>;
  readonly bloomOpacity: MotionValue<number>;
}

// --- Illumination fields ---------------------------------------------------
// Each light source is described ONCE as an intensity field: opaque at its core,
// fading to transparent at its reach. The SAME field masks the grid and shapes
// the glow, so the grid is lit exactly where the light is and dissolves on the
// identical gradient. Where there is no light, there is no grid.

// The vertical thread: a band hugging the line, fading horizontally to nothing
// with a long, gentle tail so there is no visible edge where the grid ends.
const LINE_FIELD = `radial-gradient(260px 150% at ${RAIL_X} 50%, #000 0%, rgba(0,0,0,0.85) 16%, rgba(0,0,0,0.5) 38%, rgba(0,0,0,0.22) 58%, rgba(0,0,0,0.07) 76%, transparent 92%)`;

// The coloured hues, each a single light source. Each is one elongated,
// tilted shaft of light grazing in at an angle: the SAME elongated radial both
// masks the grid and paints the glow, and the whole shaft is rotated by `tilt`,
// so the grid is lit in exactly the diagonal patch the colour glows in and the
// two fade out together. There is no coloured glow without lit grid, and no lit
// grid without a hue. `pos`/`origin` place the shaft's centre inside its own
// rotated frame; `size` is width x length, length >> width so it reads as a
// beam, not a round bloom.
const LIGHTS: readonly {
  readonly frame: string; // fixed-size, absolutely placed box for the shaft
  readonly tilt: number; // degrees, tilts the whole shaft off-vertical
  readonly color: string;
  readonly intensity: number; // peak glow alpha
}[] = [
  // Top hero hues, cyan -> periwinkle -> coral, each an angled shaft grazing in
  // from above the fold. Boxes are sized in px so the beam scale is fixed and
  // does not stretch with the (very tall) full-page field.
  {
    frame: "top-[-300px] left-[-80px] h-[1080px] w-[560px]",
    tilt: -26,
    color: RAIL_CYAN,
    intensity: 0.2,
  },
  {
    frame: "top-[-340px] left-[38%] h-[1080px] w-[520px]",
    tilt: 18,
    color: RAIL_PERIWINKLE,
    intensity: 0.2,
  },
  {
    frame: "top-[-280px] right-[-40px] h-[1060px] w-[540px]",
    tilt: 32,
    color: RAIL_CORAL,
    intensity: 0.16,
  },
  // Down-page hues so the spectrum recurs; each still lights its own grid patch
  // so no coloured glow floats over dark, gridless navy.
  {
    frame: "top-[42%] right-[-2%] h-[1140px] w-[600px]",
    tilt: 26,
    color: RAIL_PERIWINKLE,
    intensity: 0.1,
  },
  {
    frame: "bottom-[-200px] left-[16%] h-[1140px] w-[600px]",
    tilt: -22,
    color: RAIL_CYAN,
    intensity: 0.1,
  },
];

// The shaft's elongated radial, centred in its own box and reaching just past
// its edges, so the beam fills the box and fades smoothly to nothing. The SAME
// radial masks the grid and paints the glow, at the box's origin `50% 30%` so
// the light reads as arriving from above the box.
const SHAFT_ORIGIN = "50% 30%";
const SHAFT_SIZE = "70% 62%";

// The intensity field of a shaft (used to mask its grid).
function shaftField(): string {
  return `radial-gradient(${SHAFT_SIZE} at ${SHAFT_ORIGIN}, #000 0%, rgba(0,0,0,0.82) 24%, rgba(0,0,0,0.42) 48%, rgba(0,0,0,0.14) 70%, transparent 90%)`;
}

// The coloured glow of a shaft (same elongated radial, so glow and lit grid are
// the identical shape and fade out together).
function shaftGlow(color: string, a: number) {
  return `radial-gradient(${SHAFT_SIZE} at ${SHAFT_ORIGIN}, rgba(${color},${a}) 0%, rgba(${color},${a * 0.32}) 46%, transparent 82%)`;
}

// Two crossed 1px line masks that punch a full-bleed fill into a thin 72px grid.
const GRID_LINE_MASK =
  "linear-gradient(to right, #000 1px, transparent 1px), linear-gradient(to bottom, #000 1px, transparent 1px)";

// Neutral grid ink: the light fields carry the colour, not the grid itself.
// Kept faint so even at a light's core the grid only just reads.
const GRID_INK = "rgba(255, 255, 255, 0.14)";

interface LitGridProps {
  readonly field: string;
}

/**
 * A full-page sheet of thin neutral grid lines revealed ONLY through `field`
 * (a single light source's intensity gradient). The grid therefore fades out on
 * exactly the same gradient as the light lighting it; with no light, no grid.
 */
function LitGrid({ field }: LitGridProps) {
  return (
    <div
      className="absolute inset-0"
      style={{ maskImage: field, WebkitMaskImage: field }}
    >
      <div
        className="absolute inset-0"
        style={{
          backgroundColor: GRID_INK,
          maskImage: GRID_LINE_MASK,
          WebkitMaskImage: GRID_LINE_MASK,
          maskSize: "72px 72px, 72px 72px",
          WebkitMaskSize: "72px 72px, 72px 72px",
          maskComposite: "add",
          WebkitMaskComposite: "source-over",
        }}
      />
    </div>
  );
}

interface AngledLightProps {
  readonly frame: string;
  readonly tilt: number;
  readonly color: string;
  readonly intensity: number;
}

/**
 * One hue rendered as a single angled shaft of light. A rotated box carries the
 * elongated radial as BOTH the grid mask and the coloured glow, so the grid is
 * lit in exactly the diagonal patch the colour glows in and the two dissolve on
 * the identical gradient. The grid ink is counter-rotated inside the shaft so
 * the grid lines stay axis-aligned while the lit region reads as a beam grazing
 * in at an angle. Every hue therefore lights the grid and vice versa.
 */
function AngledLight({ frame, tilt, color, intensity }: AngledLightProps) {
  const field = shaftField();
  return (
    <div
      className={["absolute", frame].join(" ")}
      style={{ transform: `rotate(${tilt}deg)` }}
    >
      {/* Grid lit through the shaft: mask is the elongated radial, grid ink is
          counter-rotated so the grid stays straight inside the diagonal beam. */}
      <div
        className="absolute inset-0"
        style={{ maskImage: field, WebkitMaskImage: field }}
      >
        <div
          className="absolute inset-[-80%]"
          style={{
            transform: `rotate(${-tilt}deg)`,
            backgroundColor: GRID_INK,
            maskImage: GRID_LINE_MASK,
            WebkitMaskImage: GRID_LINE_MASK,
            maskSize: "72px 72px, 72px 72px",
            WebkitMaskSize: "72px 72px, 72px 72px",
            maskComposite: "add",
            WebkitMaskComposite: "source-over",
          }}
        />
      </div>
      {/* The coloured glow of the same shaft. */}
      <div
        className="absolute inset-0 blur-xl"
        style={{ background: shaftGlow(color, intensity) }}
      />
    </div>
  );
}

/**
 * The single, unbroken vertical line and the faint grid it lights up, rendered
 * as one full-page background field so the thread runs from the top of the page
 * to the foot with no break at any section boundary. The line is one continuous
 * thread of light hued cyan -> periwinkle -> coral around a white-hot core. The
 * grid is painted through the same gradient and revealed by two combined light
 * sources: a wide band around the thread and the coloured hues at the top of the
 * page, so a broad band of grid is visible near the line and inside the hero
 * atmosphere, fading smoothly to dark with distance. The traversed length and
 * pulse head ride brighter on top as the page scrolls.
 */
function SignalLine({
  reduced,
  pulseTop,
  fillScaleY,
  bloomOpacity,
}: SignalLineProps) {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-y-0 left-1/2 w-screen -translate-x-1/2 overflow-hidden"
    >
      {/* The line lights its own copy of the grid; then each coloured hue is a
          single angled shaft that lights grid and glows in the very same
          diagonal patch. No coloured glow floats over gridless navy, and no lit
          grid patch is missing its hue. */}
      <LitGrid field={LINE_FIELD} />
      {LIGHTS.map((l, i) => (
        <AngledLight key={i} {...l} />
      ))}

      {/* Wide, very soft white wash so the line reads as a clean column of
          white light bleeding gently onto the illuminated grid. Spans the full
          page height so the glow is one continuous column. */}
      {reduced ? (
        <div
          className="absolute inset-y-0 w-24 -translate-x-1/2 opacity-40 blur-[40px]"
          style={{ left: RAIL_X, background: "rgba(255,255,255,0.28)" }}
        />
      ) : (
        <motion.div
          className="absolute inset-y-0 w-24 -translate-x-1/2 blur-[40px]"
          style={{
            left: RAIL_X,
            opacity: bloomOpacity,
            background: "rgba(255,255,255,0.28)",
          }}
        />
      )}

      {/* Tight soft white glow hugging the line. Full height. */}
      <div
        className="absolute inset-y-0 w-2 -translate-x-1/2 blur-[4px]"
        style={{ left: RAIL_X, background: "rgba(255,255,255,0.45)" }}
      />
      {/* Constant white column: one continuous thread, always lit end to end. */}
      <div
        className="absolute inset-y-0 w-[2px] -translate-x-1/2"
        style={{ left: RAIL_X, background: "rgba(255,255,255,0.6)" }}
      />
      {/* White-hot core: a hairline thread running the full height unbroken, so
          the gradient reads as light rather than paint. */}
      <div
        className="absolute inset-y-0 w-px -translate-x-1/2"
        style={{
          left: RAIL_X,
          background:
            "linear-gradient(180deg, rgba(255,255,255,0.85), rgba(255,255,255,0.85))",
        }}
      />
      {/* Traversed length: a soft white brightening that grows from the top as
          you scroll, so the length already travelled reads hotter without
          changing the line's local hue (no colour jump). */}
      {reduced ? (
        <div
          className="absolute inset-y-0 w-[2px] -translate-x-1/2"
          style={{
            left: RAIL_X,
            background: "rgba(255,255,255,0.45)",
            boxShadow: "0 0 10px rgba(255,255,255,0.4)",
          }}
        />
      ) : (
        <motion.div
          className="absolute inset-y-0 w-[2px] origin-top -translate-x-1/2"
          style={{
            left: RAIL_X,
            scaleY: fillScaleY,
            background:
              "linear-gradient(180deg, rgba(255,255,255,0.55), rgba(255,255,255,0.4))",
            boxShadow: "0 0 12px rgba(255,255,255,0.45)",
          }}
        />
      )}
      {/* The travelling pulse head: a clean white-hot core in a white halo. */}
      {!reduced && (
        <motion.div
          className="absolute -translate-x-1/2"
          style={{ left: RAIL_X, top: pulseTop }}
        >
          <div
            className="h-2.5 w-2.5 -translate-y-1/2 rounded-full"
            style={{
              background: "#fff",
              boxShadow: "0 0 18px 4px rgba(255,255,255,0.75)",
            }}
          />
        </motion.div>
      )}
    </div>
  );
}

interface RailRootProps {
  readonly children: ReactNode;
}

/** Wraps the page, hosts the scroll-linked signal line behind the content. */
function RailRoot({ children }: RailRootProps) {
  const ref = useRef<HTMLDivElement>(null);
  const reduced = useReducedMotion() ?? false;
  const { scrollYProgress } = useScroll({
    target: ref,
    offset: ["start start", "end end"],
  });
  const pulseTop = useTransform(scrollYProgress, [0, 1], ["0%", "100%"]);
  // Gentle scroll-linked breathing of the line's wide bloom.
  const bloomOpacity = useTransform(
    scrollYProgress,
    [0, 0.5, 1],
    [0.4, 0.6, 0.4],
  );

  return (
    <RailContext.Provider value={{ scrollYProgress, railRef: ref, reduced }}>
      <div ref={ref} className="relative isolate">
        <SignalLine
          reduced={reduced}
          pulseTop={pulseTop}
          fillScaleY={scrollYProgress}
          bloomOpacity={bloomOpacity}
        />
        <div className="relative z-10">{children}</div>
      </div>
    </RailContext.Provider>
  );
}

interface StationProps {
  readonly index: string;
}

/**
 * A labelled stop on the line, pinned where the section's top border crosses
 * it. The node lights and its index brightens as the scroll pulse passes, so
 * each stop reads as a stage the operation reaches on its journey down the page.
 * The full section name lives once in the section eyebrow, not here.
 */
function Station({ index }: StationProps) {
  const ctx = useContext(RailContext);
  const ref = useRef<HTMLDivElement>(null);
  const reduced = ctx?.reduced ?? false;
  const [fraction, setFraction] = useState<number | null>(null);

  useEffect(() => {
    const rail = ctx?.railRef.current;
    if (!rail) {
      return;
    }
    const measure = () => {
      const el = ref.current;
      if (!el) {
        return;
      }
      const railTop = rail.getBoundingClientRect().top + window.scrollY;
      const elTop = el.getBoundingClientRect().top + window.scrollY;
      const height = rail.scrollHeight || 1;
      setFraction((elTop - railTop) / height);
    };
    measure();
    window.addEventListener("resize", measure);
    return () => window.removeEventListener("resize", measure);
  }, [ctx]);

  // The node glows in the local hue of the spectrum line where it sits.
  const [hr, hg, hb] = railHueAt(fraction ?? 0.5);
  const hue = `${hr}, ${hg}, ${hb}`;

  const fallback = useMotionValue(0);
  const progress = ctx?.scrollYProgress ?? fallback;
  // 1 when the pulse is on this station, easing to 0 within a small window.
  const active = useTransform(progress, (p) => {
    if (fraction == null) {
      return 0;
    }
    const distance = Math.abs(p - fraction);
    const window_ = 0.06;
    return distance < window_ ? 1 - distance / window_ : 0;
  });
  const labelColor = useTransform(
    active,
    [0, 1],
    [`rgba(${hue},0.85)`, `rgba(${hue},1)`],
  );

  return (
    <div
      ref={ref}
      aria-hidden="true"
      className="pointer-events-none absolute inset-x-0 top-0 z-20"
    >
      {/* Node centred on the line: crisp core dot, thin ring, tight halo. */}
      <span
        className={[
          "absolute flex h-3 w-3 -translate-x-1/2 -translate-y-1/2 items-center justify-center",
          RAIL_LINE,
        ].join(" ")}
      >
        {reduced ? (
          <span
            className="absolute inset-[-2px] rounded-full opacity-55 blur-[2px]"
            style={{
              background: `radial-gradient(circle, rgba(${hue},0.9), transparent 66%)`,
            }}
          />
        ) : (
          <motion.span
            className="absolute inset-[-2px] rounded-full blur-[2px]"
            style={{
              opacity: active,
              background: `radial-gradient(circle, rgba(${hue},1), transparent 66%)`,
            }}
          />
        )}
        <span
          className="absolute inset-0 rounded-full border"
          style={{ borderColor: `rgba(${hue},0.6)` }}
        />
        <span
          className="h-[5px] w-[5px] rounded-full"
          style={{
            background: `rgb(${hue})`,
            boxShadow: `0 0 6px rgba(${hue},0.7)`,
          }}
        />
      </span>

      {/* Compact index label pinned to the node; the name is carried by the
          section eyebrow so each label appears exactly once. */}
      <div
        className={["absolute top-0", RAIL_LABEL].join(" ")}
        style={{ transform: "translateY(calc(-50% - 11px))" }}
      >
        {reduced ? (
          <span
            className="font-mono text-[11px] tracking-[0.14em] tabular-nums"
            style={{ color: `rgba(${hue},0.9)` }}
          >
            {index}
          </span>
        ) : (
          <motion.span
            className="font-mono text-[11px] tracking-[0.14em] tabular-nums"
            style={{ color: labelColor }}
          >
            {index}
          </motion.span>
        )}
      </div>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Full-page atmosphere: two low-count spectrum particle fields + soft
   multi-colour glows that scroll with the document, so the brand spectrum
   recurs from hero to footer rather than one flat teal.
   ──────────────────────────────────────────────────────────────────────── */

function PageAtmosphere() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 left-1/2 -z-10 w-screen -translate-x-1/2 overflow-hidden"
    >
      {/* Two thin particle layers, warm coral and cool cyan, additively blended
          so the drifting field reads as spectrum motes, not monochrome teal.
          The coloured glows themselves live in the signal-line light system so
          every glow lights the grid; the atmosphere carries only these motes. */}
      <RisingParticles
        color="242,150,120"
        count={14}
        className="absolute inset-0"
      />
      <RisingParticles
        color="22,185,228"
        count={14}
        className="absolute inset-0"
      />
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Shared shells
   ──────────────────────────────────────────────────────────────────────── */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span
      className="text-caption font-medium tracking-[0.22em] uppercase"
      style={{ color: "#f27765" }}
    >
      {children}
    </span>
  );
}

interface CardProps {
  readonly className?: string;
  readonly children: ReactNode;
  readonly glow?: boolean;
}

/** Glassy, hairline-bordered surface: transparent enough for the page
    atmosphere and signal line to glow through, still legible for text. */
function Card({ className, children, glow = false }: CardProps) {
  return (
    <div
      className={[
        "relative overflow-hidden rounded-2xl border border-white/10 bg-white/[0.03] backdrop-blur-sm",
        className ?? "",
      ].join(" ")}
    >
      {glow && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-24 right-0 -z-0 h-56 w-56 opacity-40 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 60% 40%, rgba(242,119,101,0.18), transparent 70%)",
          }}
        />
      )}
      <div className="relative z-10 flex h-full flex-col">{children}</div>
    </div>
  );
}

interface CardHeaderProps {
  readonly index: string;
  readonly title: string;
  readonly hint?: string;
}

function CardHeader({ index, title, hint }: CardHeaderProps) {
  return (
    <div className="flex items-baseline justify-between gap-3 px-5 pt-5">
      <div className="flex items-center gap-2.5">
        <span className="text-cc-ink-dim text-caption font-mono tabular-nums">
          {index}
        </span>
        <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
      </div>
      {hint && (
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
          {hint}
        </span>
      )}
    </div>
  );
}

interface NitroCanvasProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
}

/** Wraps chart primitives so their `--t-*` token vars resolve; stays transparent. */
function NitroCanvas({ children, className, style }: NitroCanvasProps) {
  return (
    <NitroTheme
      theme="dark"
      reducedMotion="never"
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}

/** Frames a chrome-less Nitro product screen like an embedded screenshot. */
interface FramedVisualProps {
  readonly children: ReactNode;
}

function FramedVisual({ children }: FramedVisualProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-4 -z-10 rounded-[2rem] opacity-40 blur-3xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 40%, rgba(124,146,198,0.18), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        {children}
      </div>
    </div>
  );
}

interface SplitSectionProps {
  readonly id: string;
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly content: ReactNode;
  readonly aside?: ReactNode;
  readonly framed?: boolean;
  readonly wide?: boolean;
}

/**
 * The single grammar for every feature section: text anchored to the line on
 * the left, product window on the right, and a station node on the line.
 * Nothing is centred, so the narrative line is never orphaned. `wide` widens
 * the product column for dense dashboards; `framed` wraps a chrome-less screen.
 */
function SplitSection({
  id,
  index,
  eyebrow,
  title,
  body,
  content,
  aside,
  framed = true,
  wide = false,
}: SplitSectionProps) {
  const productRef = useRef<HTMLDivElement>(null);

  return (
    <section
      id={id}
      className="relative scroll-mt-24 border-t border-white/10 py-16 sm:py-24"
    >
      <Station index={index} />
      <div
        className={[
          "grid items-center gap-10 lg:grid-cols-12 lg:gap-14",
          RAIL_INDENT,
        ].join(" ")}
      >
        <RevealOnScroll className={wide ? "lg:col-span-4" : "lg:col-span-5"}>
          <div className="flex flex-col gap-5">
            <Eyebrow>{eyebrow}</Eyebrow>
            <h2 className="text-cc-heading font-heading text-h3 text-balance">
              {title}
            </h2>
            <p className="text-cc-ink max-w-md text-base leading-relaxed text-pretty sm:text-lg">
              {body}
            </p>
            {aside}
          </div>
        </RevealOnScroll>

        <RevealOnScroll
          className={wide ? "lg:col-span-8" : "lg:col-span-7"}
          hiddenClassName="translate-y-8 opacity-0"
        >
          <div ref={productRef}>
            {framed ? <FramedVisual>{content}</FramedVisual> : content}
          </div>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Fixtures for the bento chart primitives
   ──────────────────────────────────────────────────────────────────────── */

const P95_SERIES = [
  38, 41, 39, 44, 42, 47, 45, 43, 40, 42, 46, 49, 44, 41, 43, 42, 45, 48, 44,
  40, 39, 42, 44, 41,
];
const P99_SERIES = [
  92, 96, 89, 104, 98, 118, 132, 121, 108, 99, 112, 141, 168, 128, 110, 104,
  118, 152, 137, 112, 101, 108, 116, 106,
];
const THROUGHPUT_BARS = [
  32, 41, 38, 47, 52, 58, 61, 66, 72, 78, 74, 81, 88, 92, 86, 94,
];
const ERROR_SERIES = [
  0.2, 0.3, 0.2, 0.4, 0.3, 0.6, 1.1, 0.9, 0.5, 0.3, 0.4, 0.8, 1.6, 0.7, 0.4,
  0.3, 0.5, 0.9, 0.6, 0.3,
];

const CLIENTS: readonly Client[] = [
  { name: "web-storefront", total: 184000, impact: 94 },
  { name: "mobile-ios", total: 121000, impact: 71 },
  { name: "partner-api", total: 68000, impact: 58 },
  { name: "admin-console", total: 24000, impact: 33 },
  { name: "analytics-etl", total: 12000, impact: 18 },
];

const INSIGHTS: readonly InsightRow[] = [
  {
    id: "op-checkout",
    spanKind: "server",
    name: "mutation checkout",
    averageLatency: 168,
    opm: 1240,
    errorRate: 0.031,
    impact: 98,
    latencySeries: [42, 48, 61, 88, 132, 168, 141],
    throughputSeries: [820, 910, 1040, 1180, 1210, 1240, 1190],
  },
  {
    id: "op-cart",
    spanKind: "server",
    name: "query cart",
    averageLatency: 44,
    opm: 4820,
    errorRate: 0.004,
    impact: 61,
    latencySeries: [38, 41, 44, 42, 46, 44, 41],
    throughputSeries: [4200, 4500, 4700, 4820, 4780, 4820, 4900],
  },
  {
    id: "op-search",
    spanKind: "internal",
    name: "query search",
    averageLatency: 72,
    opm: 2140,
    errorRate: 0.012,
    impact: 47,
    latencySeries: [58, 64, 71, 69, 74, 72, 70],
    throughputSeries: [1900, 2010, 2100, 2140, 2080, 2140, 2200],
  },
];

const TRACE: Trace = {
  totalMs: 168,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 168,
      depth: 0,
    },
    {
      id: "s2",
      name: "mutation checkout",
      kind: "graphql",
      startMs: 4,
      durationMs: 158,
      depth: 1,
    },
    {
      id: "s3",
      name: "PricingService.quote",
      kind: "http",
      startMs: 12,
      durationMs: 34,
      depth: 2,
    },
    {
      id: "s4",
      name: "InventoryDb.reserve",
      kind: "internal",
      startMs: 48,
      durationMs: 96,
      depth: 2,
    },
    {
      id: "s5",
      name: "PaymentGateway.charge",
      kind: "http",
      startMs: 146,
      durationMs: 18,
      depth: 2,
    },
  ],
};

/* ────────────────────────────────────────────────────────────────────────
   Bento of telemetry signals (Observe, built from chart primitives)
   ──────────────────────────────────────────────────────────────────────── */

function SignalsBento() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
      {/* Latency p95/p99 */}
      <Card className="sm:col-span-4" glow>
        <CardHeader index="a" title="Latency" hint="p95 / p99 · ms" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <ChartPanel
              title="Response time"
              subtitle="last 60 minutes"
              height={168}
              yDomain={[0, 180]}
              yTicks={[0, 60, 120, 180]}
              yFormat={(n) => `${n}`}
              legend={[
                { label: "p95", color: token.cP95 },
                { label: "p99", color: token.cP99 },
              ]}
            >
              <LineAreaChart
                series={[
                  {
                    values: P95_SERIES,
                    stroke: token.cP95,
                    fill: true,
                    fillOpacity: 0.12,
                  },
                  {
                    values: P99_SERIES,
                    stroke: token.cP99,
                    fill: true,
                    fillOpacity: 0.1,
                  },
                ]}
                domain={[0, 180]}
                grid
                showHead
              />
            </ChartPanel>
          </NitroCanvas>
        </div>
      </Card>

      {/* Throughput big stat */}
      <Card className="sm:col-span-2">
        <CardHeader index="b" title="Throughput" hint="ops / min" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <NitroCanvas className="h-11">
            <CountUp
              value={94200}
              format={(n) => Math.round(n).toLocaleString("en-US")}
              style={{ justifyContent: "flex-start", fontSize: 34 }}
            />
          </NitroCanvas>
          <NitroCanvas className="mt-3 h-16">
            <BarSeries values={THROUGHPUT_BARS} color={token.cThroughput} />
          </NitroCanvas>
        </div>
      </Card>

      {/* Per-client usage & impact */}
      <Card className="sm:col-span-3">
        <CardHeader index="c" title="Top clients" hint="by impact" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <HBarSeries clients={CLIENTS as Client[]} maxBars={5} />
          </NitroCanvas>
        </div>
      </Card>

      {/* Error rate */}
      <Card className="sm:col-span-3">
        <CardHeader index="d" title="Error rate" hint="% of requests" />
        <div className="flex flex-1 flex-col justify-between px-5 pt-4 pb-5">
          <div className="flex items-baseline gap-2">
            <span
              className="font-heading text-h4 tabular-nums"
              style={{ color: token.cError }}
            >
              0.31%
            </span>
            <span className="text-cc-ink-dim text-caption">
              within budget · 1.6% peak
            </span>
          </div>
          <NitroCanvas className="mt-3 h-16">
            <Sparkline values={ERROR_SERIES} stroke={token.cError} fill />
          </NitroCanvas>
        </div>
      </Card>

      {/* Impact-ranked operations */}
      <Card className="sm:col-span-4">
        <CardHeader index="e" title="Impact score" hint="what hurts most" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <InsightsTable
              rows={INSIGHTS as InsightRow[]}
              errorThreshold={0.02}
            />
          </NitroCanvas>
        </div>
      </Card>

      {/* Trace preview */}
      <Card className="sm:col-span-2">
        <CardHeader index="f" title="Slow span" hint="checkout" />
        <div className="px-5 pt-3 pb-5">
          <NitroCanvas>
            <TraceWaterfall trace={TRACE} rowHeight={22} />
          </NitroCanvas>
        </div>
      </Card>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Schema change classification (Evolve aside graphic)
   ──────────────────────────────────────────────────────────────────────── */

type ChangeKind = "safe" | "dangerous" | "breaking";

const KIND_STYLE: Record<
  ChangeKind,
  { readonly label: string; readonly className: string }
> = {
  safe: {
    label: "SAFE",
    className: "text-cc-success border-cc-success/40 bg-cc-success/[0.08]",
  },
  dangerous: {
    label: "DANGEROUS",
    className: "text-cc-warning border-cc-warning/40 bg-cc-warning/[0.08]",
  },
  breaking: {
    label: "BREAKING",
    className: "text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08]",
  },
};

interface KindPillProps {
  readonly kind: ChangeKind;
}

function KindPill({ kind }: KindPillProps) {
  const s = KIND_STYLE[kind];
  return (
    <span
      className={[
        "rounded border px-1.5 py-0.5 font-mono text-[10px] tracking-[0.12em]",
        s.className,
      ].join(" ")}
    >
      {s.label}
    </span>
  );
}

const SCHEMA_CHANGES: readonly {
  readonly field: string;
  readonly kind: ChangeKind;
}[] = [
  { field: "+ Order.deliveryEstimate: DateTime", kind: "safe" },
  { field: "~ Product.price: Float → Money", kind: "dangerous" },
  { field: "- Order.total: Float", kind: "breaking" },
];

function ClassificationCard() {
  return (
    <Card className="mt-1">
      <div className="flex items-center justify-between px-4 py-3">
        <span className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
          orders-api · v14
        </span>
        <span className="text-cc-danger text-caption font-mono">
          publish blocked
        </span>
      </div>
      <div className="divide-y divide-white/10 border-t border-white/10">
        {SCHEMA_CHANGES.map((c) => (
          <div
            key={c.field}
            className="flex items-center justify-between gap-3 px-4 py-2.5"
          >
            <code className="text-cc-ink truncate font-mono text-xs">
              {c.field}
            </code>
            <KindPill kind={c.kind} />
          </div>
        ))}
      </div>
      <div className="text-cc-ink-dim border-t border-white/10 px-4 py-2.5 font-mono text-[11px]">
        1 safe · 1 dangerous · 1 breaking
      </div>
    </Card>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Delivery / safety content (persisted ops · CI checks · safe rollout)
   ──────────────────────────────────────────────────────────────────────── */

interface CheckRow {
  readonly label: string;
  readonly detail: string;
  readonly state: "pass" | "fail";
}

const CI_CHECKS: readonly CheckRow[] = [
  { label: "schema validate", detail: "127 fields", state: "pass" },
  { label: "client checks", detail: "3 published clients", state: "pass" },
  { label: "breaking change", detail: "Order.total removed", state: "fail" },
  { label: "trusted operations", detail: "482 hashes signed", state: "pass" },
];

function CheckIconMark({ state }: { readonly state: "pass" | "fail" }) {
  if (state === "pass") {
    return (
      <svg
        viewBox="0 0 16 16"
        aria-hidden="true"
        className="text-cc-success h-4 w-4 fill-current"
      >
        <path d="M6.5 11.2 3.3 8l1.1-1.1 2.1 2.1 5-5L12.6 5z" />
      </svg>
    );
  }
  return (
    <svg
      viewBox="0 0 16 16"
      aria-hidden="true"
      className="text-cc-danger h-4 w-4 fill-current"
    >
      <path d="M11.5 5.6 9.1 8l2.4 2.4-1.1 1.1L8 9.1l-2.4 2.4-1.1-1.1L6.9 8 4.5 5.6l1.1-1.1L8 6.9l2.4-2.4z" />
    </svg>
  );
}

function DeliveryContent() {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <Card className="md:col-span-2">
        <div className="flex items-center justify-between px-5 py-3.5">
          <span className="text-cc-heading font-heading text-h6">
            CI schema check
          </span>
          <span className="text-cc-danger border-cc-danger/40 bg-cc-danger/[0.08] rounded border px-2 py-0.5 font-mono text-[10px] tracking-[0.12em]">
            FAILED
          </span>
        </div>
        <div className="divide-y divide-white/10 border-t border-white/10">
          {CI_CHECKS.map((c) => (
            <div key={c.label} className="flex items-center gap-3 px-5 py-3">
              <CheckIconMark state={c.state} />
              <span className="text-cc-ink font-mono text-sm">{c.label}</span>
              <span className="text-cc-ink-dim ml-auto font-mono text-xs">
                {c.detail}
              </span>
            </div>
          ))}
        </div>
        <div className="text-cc-ink-dim border-t border-white/10 px-5 py-3 font-mono text-[11px]">
          merging is blocked until every check passes
        </div>
      </Card>

      <Card className="h-full">
        <div className="flex flex-col gap-5 p-6">
          <div>
            <div className="text-cc-nav-label text-caption font-mono tracking-[0.16em] uppercase">
              Persisted operations
            </div>
            <p className="text-cc-ink mt-2 text-sm leading-relaxed">
              Only registered query hashes execute. Ad-hoc queries and injection
              never reach a resolver.
            </p>
          </div>
          <div className="rounded-lg border border-white/10 bg-black/20 p-3 font-mono text-xs">
            <div className="text-cc-ink-dim">POST /graphql</div>
            <div className="mt-1 truncate" style={{ color: "#f27765" }}>
              documentId: sha256:7f3a9b2e…
            </div>
            <div className="text-cc-success mt-1">200 · trusted · 12 ms</div>
          </div>
          <div className="mt-auto flex items-center gap-2">
            <span className="text-cc-success text-caption font-mono">
              ● safe rollout
            </span>
            <span className="text-cc-ink-dim text-caption">
              stage → canary → prod
            </span>
          </div>
        </div>
      </Card>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Ecosystem / platform strip
   ──────────────────────────────────────────────────────────────────────── */

interface PlatformIconProps {
  readonly className?: string;
}

function ServerGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <rect x="3" y="4" width="18" height="6" rx="1.5" />
      <rect x="3" y="14" width="18" height="6" rx="1.5" />
      <circle cx="7" cy="7" r="1" className="fill-cc-bg" />
      <circle cx="7" cy="17" r="1" className="fill-cc-bg" />
    </svg>
  );
}

function ClientGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <rect x="3" y="4" width="18" height="13" rx="1.5" />
      <rect x="9" y="19" width="6" height="1.6" rx="0.8" />
      <rect
        x="6"
        y="7"
        width="8"
        height="1.4"
        rx="0.7"
        className="fill-cc-bg"
      />
    </svg>
  );
}

function GatewayGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <circle cx="12" cy="12" r="3" />
      <circle cx="4" cy="5" r="2" />
      <circle cx="4" cy="19" r="2" />
      <circle cx="20" cy="12" r="2" />
      <path
        d="M6 6l4 5M6 18l4-5M15 12h3"
        stroke="currentColor"
        strokeWidth="1.4"
        fill="none"
      />
    </svg>
  );
}

function ControlGlyph({ className }: PlatformIconProps) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className}>
      <circle
        cx="12"
        cy="12"
        r="9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
      />
      <circle cx="12" cy="12" r="2.5" />
      <path
        d="M12 3v3M12 18v3M3 12h3M18 12h3"
        stroke="currentColor"
        strokeWidth="1.6"
      />
    </svg>
  );
}

const PLATFORM: readonly {
  readonly name: string;
  readonly role: string;
  readonly Icon: (p: PlatformIconProps) => ReactNode;
}[] = [
  { name: "Hot Chocolate", role: "GraphQL server", Icon: ServerGlyph },
  { name: "Strawberry Shake", role: "GraphQL client", Icon: ClientGlyph },
  { name: "Fusion", role: "Federation gateway", Icon: GatewayGlyph },
  { name: "Nitro", role: "Control plane", Icon: ControlGlyph },
];

// Per-card hairline hues so the strip reads across the spectrum, not one teal.
const PLATFORM_HUES: readonly string[] = [
  "242, 119, 101", // coral
  "102, 190, 119", // green
  "22, 185, 228", // cyan
  "124, 146, 198", // violet (last card also carries the full SPECTRUM bar)
];

function EcosystemContent() {
  return (
    <div className="flex flex-col gap-5">
      <div className="grid gap-3 sm:grid-cols-2">
        {PLATFORM.map(({ name, role, Icon }, i) => (
          <Card key={name} className="h-full">
            <div className="flex flex-col gap-4 p-5">
              <div
                className="flex h-10 w-10 items-center justify-center rounded-lg border border-white/10 bg-black/20"
                style={{ color: "#f27765" }}
              >
                <Icon className="h-5 w-5 fill-current" />
              </div>
              <div>
                <div className="text-cc-heading font-heading text-h6">
                  {name}
                </div>
                <div className="text-cc-ink-dim text-caption font-mono tracking-[0.14em] uppercase">
                  {role}
                </div>
              </div>
              <span
                aria-hidden="true"
                className="h-px w-full rounded-full opacity-60"
                style={{
                  background:
                    i === PLATFORM.length - 1
                      ? SPECTRUM
                      : `linear-gradient(90deg, rgba(${PLATFORM_HUES[i]},0.55), transparent)`,
                }}
              />
            </div>
          </Card>
        ))}
      </div>
      <div className="text-cc-ink-dim text-caption font-mono tracking-[0.14em] uppercase">
        MIT licensed · built in the open · one graph for .NET
      </div>
    </div>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Hero (full-bleed line + grid: left-aligned copy, brand-teal light on navy)
   ──────────────────────────────────────────────────────────────────────── */

const TRUSTED_ON: readonly string[] = [
  "Hot Chocolate",
  "Strawberry Shake",
  "Fusion",
  "OpenTelemetry",
  "ASP.NET Core",
];

function ArrowGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      aria-hidden="true"
      className="ml-2 h-3.5 w-3.5 fill-current"
    >
      <path d="M6.4 3.3 5.3 4.4 8.9 8l-3.6 3.6 1.1 1.1L11.1 8z" />
    </svg>
  );
}

function Hero() {
  const reelRef = useRef<HTMLDivElement>(null);

  return (
    <section className="relative left-1/2 isolate -mt-6 w-screen -translate-x-1/2 overflow-hidden pt-24 pb-14 sm:-mt-12 sm:pt-28">
      {/* No separate hero colour crown: the top hues are the page-root signal
          field's angled light shafts, so every coloured glow up here has lit
          grid inside it (colour and grid arrive together) and nothing floats
          over gridless navy. */}

      {/* Origin of the line: where one operation enters the control plane.
          Pinned inside the same max-w-7xl column as the page-root signal field,
          so the node sits exactly on the line rather than the full-bleed edge. */}
      <div className="pointer-events-none absolute inset-x-0 top-[64px] z-20">
        <div aria-hidden="true" className="relative mx-auto max-w-7xl">
          <span
            className={[
              "absolute flex h-4 w-4 -translate-x-1/2 -translate-y-1/2 items-center justify-center",
              RAIL_LINE,
            ].join(" ")}
          >
            <span
              className="absolute inset-[-3px] rounded-full opacity-80 blur-[3px]"
              style={{
                background: `radial-gradient(circle, rgba(${RAIL_CYAN},0.95), transparent 66%)`,
              }}
            />
            <span
              className="absolute inset-0 rounded-full border"
              style={{ borderColor: `rgba(${RAIL_CYAN},0.75)` }}
            />
            <span
              className="h-[6px] w-[6px] rounded-full"
              style={{
                background: "#fff",
                boxShadow: `0 0 10px 1px rgba(${RAIL_CYAN},0.9)`,
              }}
            />
          </span>
          <div
            className={[
              "absolute top-0 flex items-center gap-2",
              RAIL_LABEL,
            ].join(" ")}
            style={{ transform: "translateY(calc(-50% - 13px))" }}
          >
            <span
              className="font-mono text-[11px] tabular-nums"
              style={{ color: `rgb(${RAIL_CYAN})` }}
            >
              00
            </span>
            <span
              className="font-mono text-[10px] tracking-[0.24em] whitespace-nowrap uppercase"
              style={{ color: `rgb(${RAIL_CYAN})` }}
            >
              one operation
            </span>
          </div>
        </div>
      </div>

      {/* Inner content column on top of the full-bleed light. */}
      <div className="relative z-10 mx-auto max-w-7xl px-5 sm:px-12">
        <div
          className={[
            "grid items-center gap-x-10 gap-y-14 lg:grid-cols-12",
            RAIL_INDENT,
          ].join(" ")}
        >
          <RevealOnScroll className="flex flex-col items-start gap-6 lg:col-span-5">
            <div className="inline-flex items-center gap-2.5 rounded-full border border-white/10 bg-white/5 px-3 py-1 backdrop-blur">
              <span
                aria-hidden="true"
                className="h-1.5 w-1.5 rounded-full"
                style={{ background: "#f27765", boxShadow: "0 0 8px #f27765" }}
              />
              <Eyebrow>The Control Plane for GraphQL</Eyebrow>
            </div>
            <h1 className="text-cc-heading font-heading text-h1 text-left text-balance">
              The control plane for GraphQL on .NET.
            </h1>
            <p className="text-cc-ink max-w-lg text-lg leading-relaxed text-pretty">
              Author operations, watch them run, trace every request, and evolve
              your schema without breaking the clients you ship to.
            </p>
            <div className="mt-2 flex flex-wrap items-center gap-5">
              <SolidButton
                href="/get-started"
                className="!bg-[#f27765] !text-[#0b0f1a] shadow-lg shadow-[#f27765]/25 hover:!bg-[#f5924e]"
              >
                Start for Free
                <ArrowGlyph />
              </SolidButton>
              <OutlineButton
                href="https://nitro.chillicream.com"
                className="!border-transparent !px-2 hover:!text-[#f27765]"
              >
                Request a demo
              </OutlineButton>
            </div>
          </RevealOnScroll>

          {/* First product window enters the story on the right of the hero.
              Crisp; only a soft spectrum glow sits behind it, never on it. */}
          <RevealOnScroll
            className="relative lg:col-span-7"
            hiddenClassName="translate-y-10 opacity-0"
            shownClassName="translate-y-0 opacity-100"
          >
            <div ref={reelRef} className="relative lg:-mr-6 xl:-mr-12">
              <div
                aria-hidden="true"
                className="absolute -inset-x-8 -inset-y-6 -z-10 rounded-[2.5rem] opacity-60 blur-3xl"
                style={{
                  background:
                    "radial-gradient(58% 55% at 46% 32%, rgba(242,119,101,0.18), transparent 62%), radial-gradient(60% 55% at 66% 62%, rgba(22,185,228,0.14), transparent 68%)",
                }}
              />
              <NitroReel tabsOverlay />
            </div>
          </RevealOnScroll>
        </div>

        {/* Trusted-by word band. */}
        <RevealOnScroll
          className={[
            "relative mt-16 flex flex-col items-start gap-4",
            RAIL_INDENT,
          ].join(" ")}
        >
          <span className="text-cc-ink-dim text-caption font-mono tracking-[0.18em] uppercase">
            Trusted by teams building on .NET
          </span>
          <div className="flex flex-wrap items-center gap-x-8 gap-y-3">
            {TRUSTED_ON.map((name) => (
              <span
                key={name}
                className="font-heading text-sm tracking-wide text-white/65"
              >
                {name}
              </span>
            ))}
          </div>
        </RevealOnScroll>
      </div>
    </section>
  );
}

/* ────────────────────────────────────────────────────────────────────────
   Page
   ──────────────────────────────────────────────────────────────────────── */

export function ClientPage() {
  return (
    <div className="relative isolate">
      {/* FULL-PAGE ATMOSPHERE ─────────────────────────────────── */}
      <PageAtmosphere />

      <RailRoot>
        {/* HERO ─────────────────────────────────────────────────── */}
        <Hero />

        {/* AUTHOR ───────────────────────────────────────────────── */}
        <SplitSection
          id="author"
          index="01"
          eyebrow="Author"
          title="Compose GraphQL against real, federated data."
          body="A schema-aware editor with live validation and one-click operations, running against your composed graph, not a mock."
          content={<NitroCompose className="w-full" />}
        />

        {/* OBSERVE ──────────────────────────────────────────────── */}
        <SplitSection
          id="observe"
          index="02"
          eyebrow="Observe"
          title="See exactly how your API behaves in production."
          body="OpenTelemetry-native monitoring: latency, throughput, and error rate per operation and per client, ranked by the impact score that tells you what to fix first."
          content={<ControlPlaneConsole className="w-full" />}
          framed={false}
          wide
        />

        {/* SIGNALS BENTO ────────────────────────────────────────── */}
        <SplitSection
          id="signals"
          index="03"
          eyebrow="Every signal"
          title="One console, every signal in view."
          body="p95 and p99 latency, throughput, error budget, per-client usage, impact ranking, and the slow span, all reading from the same telemetry."
          content={<SignalsBento />}
          framed={false}
          wide
        />

        {/* TRACE ────────────────────────────────────────────────── */}
        <SplitSection
          id="trace"
          index="04"
          eyebrow="Trace"
          title="Follow one request across your whole backend."
          body="Distributed tracing stitches a single operation across GraphQL, REST, gRPC, and background jobs. Walk the span waterfall down to the resolver that ran slow."
          content={<NitroTrace className="w-full" />}
        />

        {/* DIAGNOSE ─────────────────────────────────────────────── */}
        <SplitSection
          id="diagnose"
          index="05"
          eyebrow="Diagnose"
          title="From an error spike to the line that threw it."
          body="When errors climb, Nitro takes you from the spike to the exact failing operation and the server-side stack trace behind it. No log spelunking."
          content={<NitroDiagnose className="w-full" />}
        />

        {/* EVOLVE / SCHEMA ──────────────────────────────────────── */}
        <SplitSection
          id="schema"
          index="06"
          eyebrow="Evolve"
          title="Change your schema without breaking clients."
          body="The registry classifies every change as safe, dangerous, or breaking and checks it against published clients before it ships."
          content={<NitroSchema className="w-full" />}
          aside={<ClassificationCard />}
        />

        {/* DELIVERY / SAFETY ────────────────────────────────────── */}
        <SplitSection
          id="delivery"
          index="07"
          eyebrow="Delivery"
          title="Ship on green, roll out with a safety net."
          body="Persisted, trusted operations lock the queries clients can send. Schema and client checks run in CI, so a breaking change fails the build instead of the customer."
          content={<DeliveryContent />}
          framed={false}
          wide
        />

        {/* FUSION ───────────────────────────────────────────────── */}
        <SplitSection
          id="fusion"
          index="08"
          eyebrow="Compose"
          title="One graph, executed across every subgraph."
          body="Fusion shows the distributed query plan: how a single operation fans out into parallel, batched fetches across your subgraphs and folds back into one response."
          content={<NitroFusion className="w-full" />}
        />

        {/* ECOSYSTEM ────────────────────────────────────────────── */}
        <SplitSection
          id="ecosystem"
          index="09"
          eyebrow="Platform"
          title="One open-source stack, end to end."
          body="Nitro sits on top of the same GraphQL platform you already build with, from the server that answers to the gateway that composes."
          content={<EcosystemContent />}
          framed={false}
          wide
        />

        {/* CTA ──────────────────────────────────────────────────── */}
        <section className="relative border-t border-white/10 py-20 sm:py-28">
          <Station index="10" />
          <RevealOnScroll
            className={[
              "flex max-w-2xl flex-col items-start gap-6",
              RAIL_INDENT,
            ].join(" ")}
          >
            <Eyebrow>Ready when you are</Eyebrow>
            <h2 className="text-cc-heading font-heading text-h2 text-left text-balance">
              Put your API on the control plane.
            </h2>
            <p className="text-cc-ink max-w-xl text-lg leading-relaxed">
              Start in the GraphQL IDE in seconds, then grow into observability,
              tracing, and a registry that keeps your schema and clients in
              sync.
            </p>
            <div className="mt-2 flex flex-wrap items-center gap-4">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="https://nitro.chillicream.com">
                Launch Nitro
              </OutlineButton>
            </div>
          </RevealOnScroll>
        </section>
      </RailRoot>
    </div>
  );
}
