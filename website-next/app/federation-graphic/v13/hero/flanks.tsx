"use client";

/**
 * Widescreen edge treatment for the v12 hero: the "journey" concept.
 *
 * Three earlier attempts filled the margins with decoration (fans,
 * starfields, particle funnels) and every one was rejected for the
 * same root cause: the margins did not integrate with the scene. The
 * rule that survived: the flanks are PART OF THE STORY, told in the
 * scene's own vocabulary, or they are nothing.
 *
 * So the margins hold exactly what the story implies is there: the
 * quiet transport lane running off both screen edges toward the unseen
 * client. The story's own request envelope arrives along it as the
 * intro fades in, and the sealed response envelope departs along it at
 * the end, accelerating away. Flow chevrons sit IN the lane (stroked
 * with the lane's own distance gradient, so they are texture of the
 * line, not floating marks); they light up briefly as an envelope
 * passes. After each transit the lane keeps a faint color residue:
 * finished things hold, like everywhere else in the scene.
 *
 * Each flank is a slice-cropped SVG whose unit scale matches the main
 * scene, anchored at the seam. The envelope legs share one clock and
 * easing with the main scene's legs (see ENTRY_* in GatewayScene.tsx),
 * so the glyph crosses the seam without a velocity jump, and both
 * transits replay when the timeline is scrubbed.
 */

import type { AnimHelpers, Visual } from "./anim";
import { easeInOutCubic, Envelope, ramp } from "./anim";

export type FlankVariant = "journey" | "off";

const TEAL = "#5eead4";
const GREEN = "#8fd6a0";
const INK = "#f5f0ea";

const W = 1000;
const LANE_Y = 246;

/* The entry journey: flank leg (0..1000) then the main scene's leg to
   the request card, one shared easing over 300..800ms. Must mirror
   ENTRY_TOTAL in GatewayScene.tsx (1000 flank units + 118 units from
   the seam to the card edge). */
const ENTRY_TOTAL = W + 118;

/* The exit journey: the outgoing envelope's center crosses the main
   SVG's clipping edge (x=1400) at t=34727 moving at 0.402 units/ms
   (inverting LANE_OUT's easeInOutCubic over 34200..35100). This leg
   picks it up at the seam with that exact velocity, then accelerates
   away into the distance, reaching the far edge in ~1.25s. */
const OUT_SEAM_T = 34727;
const OUT_V0 = 0.402;
const OUT_ACCEL = 0.00032;

/* Flow chevrons, placed seam-relative so common crop widths always cut
   the lane, never a mark: 1920px reveals one per side, 2560px two. */
const CHEV_L = [850, 670, 460, 220] as const;
const CHEV_R = [150, 330, 540, 780] as const;

function chevronD(x: number): string {
  return `M${x - 7} ${LANE_Y - 8} L${x + 2} ${LANE_Y} L${x - 7} ${LANE_Y + 8}`;
}

interface FlankLayerProps {
  readonly side: "left" | "right";
  readonly variant: FlankVariant;
  readonly set: Visual["set"];
}

export function FlankLayer({ side, variant, set }: FlankLayerProps) {
  if (variant === "off") {
    return null;
  }
  const left = side === "left";
  const s = left ? "L" : "R";
  const color = left ? TEAL : GREEN;
  const chevrons = left ? CHEV_L : CHEV_R;
  return (
    <div
      className={
        "pointer-events-none absolute inset-y-0 hidden w-[max(0px,calc((100%-1480px)/2))] sm:block " +
        (left ? "left-0" : "right-0")
      }
    >
      <svg
        viewBox={`0 76 ${W} 624`}
        preserveAspectRatio={left ? "xMaxYMid slice" : "xMinYMid slice"}
        className="h-full w-full"
      >
        <defs>
          {/* Brightest at the seam, fading toward the screen edge; the
              seam stop matches the main scene's lane stubs (0.09) so
              the join is invisible, and the lane runs off the screen
              edge toward the unseen client, as the story says. */}
          <linearGradient
            id={`fjlane-${s}`}
            gradientUnits="userSpaceOnUse"
            x1={left ? 0 : W}
            y1={0}
            x2={left ? W : 0}
            y2={0}
          >
            <stop offset="0" stopColor={INK} stopOpacity="0.02" />
            <stop offset="0.55" stopColor={INK} stopOpacity="0.05" />
            <stop offset="1" stopColor={INK} stopOpacity="0.09" />
          </linearGradient>
          <linearGradient
            id={`fjres-${s}`}
            gradientUnits="userSpaceOnUse"
            x1={left ? 0 : W}
            y1={0}
            x2={left ? W : 0}
            y2={0}
          >
            <stop offset="0" stopColor={color} stopOpacity="0.25" />
            <stop offset="1" stopColor={color} stopOpacity="1" />
          </linearGradient>
        </defs>
        <line
          x1={0}
          x2={W}
          y1={LANE_Y}
          y2={LANE_Y}
          stroke={`url(#fjlane-${s})`}
          strokeWidth={1.5}
        />
        {/* The transit residue: finished things hold. The initial
            opacity is the reduced-motion poster (story completed). */}
        <line
          ref={set(`fj${s}res`)}
          x1={0}
          x2={W}
          y1={LANE_Y}
          y2={LANE_Y}
          stroke={`url(#fjres-${s})`}
          strokeWidth={1.5}
          opacity={0.03}
        />
        {chevrons.map((x, i) => (
          <g key={x}>
            <path
              d={chevronD(x)}
              fill="none"
              stroke={`url(#fjlane-${s})`}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <path
              ref={set(`fj${s}w${i}`)}
              d={chevronD(x)}
              fill="none"
              stroke={color}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
              opacity={0}
            />
          </g>
        ))}
        {left ? (
          <Envelope set={set} id="fjLenv" stroke={TEAL} />
        ) : (
          <Envelope set={set} id="fjRenv" stroke={GREEN} />
        )}
      </svg>
    </div>
  );
}

/* ── The driver ──────────────────────────────────────────────────────── */

/** A chevron lights as the envelope reaches it and cools in its wake. */
function wake(d: number, cx: number): number {
  return 0.35 * ramp(d, cx - 30, cx) * (1 - ramp(d, cx + 40, cx + 340));
}

export function driveFlanks(
  variant: FlankVariant,
  t: number,
  _wall: number,
  h: AnimHelpers,
) {
  if (variant !== "journey") {
    return;
  }
  // The query arrives from the unseen client; the main scene's entry
  // leg takes over past the seam (same clock and easing, so the glyph
  // crosses the seam without a velocity jump).
  const inDist = easeInOutCubic(ramp(t, 300, 800)) * ENTRY_TOTAL;
  if (inDist < W) {
    h.setX("fjLenv", inDist, LANE_Y);
    h.setO("fjLenv", ramp(t, 300, 400));
  } else {
    h.setO("fjLenv", 0);
  }
  CHEV_L.forEach((cx, i) => {
    h.setO(`fjLw${i}`, wake(inDist, cx));
  });
  h.setO("fjLres", 0.03 * ramp(t, 650, 1400));

  // The response departs to the unseen client, accelerating away. The
  // right lane brightens slightly when the respond beat announces it,
  // and holds a faint residue afterwards: the response left through
  // this margin.
  const tau = t - OUT_SEAM_T;
  const outDist = OUT_V0 * tau + OUT_ACCEL * tau * tau;
  if (tau > 0 && outDist < W + 50) {
    h.setX("fjRenv", outDist, LANE_Y);
    h.setO("fjRenv", ramp(tau, 0, 90));
  } else {
    h.setO("fjRenv", 0);
  }
  CHEV_R.forEach((cx, i) => {
    h.setO(`fjRw${i}`, tau > 0 ? wake(outDist, cx) : 0);
  });
  h.setO("fjRres", 0.05 * ramp(t, 33400, 33900) - 0.02 * ramp(t, 36000, 36400));
}

/* ── The compare switcher ────────────────────────────────────────────── */

const VARIANTS: readonly FlankVariant[] = ["journey", "off"];

interface FlankSwitcherProps {
  readonly value: FlankVariant;
  readonly onChange: (v: FlankVariant) => void;
}

/** Floating playground control for comparing the edge treatments. */
export function FlankSwitcher({ value, onChange }: FlankSwitcherProps) {
  return (
    <div className="fixed right-5 bottom-5 z-50 flex gap-1 rounded-full border border-white/10 bg-[#0c1322]/90 p-1 font-mono text-[11px] tracking-wide backdrop-blur">
      {VARIANTS.map((o) => (
        <button
          key={o}
          type="button"
          onClick={() => onChange(o)}
          className={
            o === value
              ? "rounded-full bg-[#5eead4]/15 px-3 py-1 text-[#5eead4]"
              : "rounded-full px-3 py-1 text-slate-400 hover:text-slate-200"
          }
        >
          {o}
        </button>
      ))}
    </div>
  );
}
