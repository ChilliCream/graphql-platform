"use client";

/**
 * Composition checks, animated as a split-screen truth. Top lane: client
 * traffic flows against the live composite the entire loop, never once
 * interrupted. Bottom lane: pricing publishes a conflicting type, composition
 * fails with the exact diagnostic, the fix is published, the version bumps.
 * The story is what does NOT happen: no error ever reaches the top lane.
 */

import { AMBER, CORAL, GREEN, MONO_FONT, SLATE, TEAL } from "../palette";
import {
  DIM,
  HAIR,
  LANE,
  PANEL_STROKE,
  PulseGlyph,
  SURFACE,
  VisualCard,
  easeInOutCubic,
  measure,
  ramp,
  useVisual,
} from "./anim";

const T = 13000;
const TOP = 56;
const BOT = 230;

const REQ = measure([
  [112, TOP],
  [380, TOP],
]);
const RES = measure([
  [380, TOP],
  [112, TOP],
]);
const PUB = measure([
  [148, BOT],
  [380, BOT],
]);
const SHIP = measure([
  [520, BOT],
  [700, BOT],
]);

export function CheckVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // ── Top: traffic, uninterrupted, the whole loop ────────────────────
    const tc = (t - 200 + T) % 1400;
    if (tc < 500) {
      h.placePulse(
        "req",
        REQ,
        easeInOutCubic(tc / 500),
        Math.min(tc / 120, 1),
        2.2,
      );
    } else {
      h.hidePulse("req");
    }
    if (tc >= 650 && tc < 1150) {
      h.placePulse("res", RES, easeInOutCubic((tc - 650) / 500), 1, 2.2);
    } else {
      h.hidePulse("res");
    }
    h.setRing("ringOK", (tc - 1150) / 350, 3, 8);

    // ── Bottom: the bad publish ────────────────────────────────────────
    const bad = 1200;
    h.setO("pubBad", t >= bad - 400 && t < bad + 600 ? 0.85 : 0);
    if (t >= bad && t < bad + 800) {
      const u = easeInOutCubic(ramp(t, bad, bad + 800));
      h.placePulse("badP", PUB, u, Math.min((t - bad) / 150, 1), 2.4);
    } else {
      h.hidePulse("badP");
    }
    const xOn = t >= 2000 ? 1 - ramp(t, 6100, 6500) : 0;
    h.setO("failX", xOn * 0.95);
    h.setRing("ringFail", (t - 2000) / 550, 4, 14);
    const diag =
      easeInOutCubic(ramp(t, 2100, 2500)) * (1 - ramp(t, 6100, 6500));
    h.setPop("diag", diag * 0.95, diag);
    const unch = t >= 2400 && t < 4400 ? 0.75 : 0;
    h.setO("unchanged", unch * (0.6 + 0.3 * Math.sin(t / 240)));

    // ── The fix ────────────────────────────────────────────────────────
    const fix = 7000;
    h.setO("pubFix", t >= fix - 400 && t < fix + 600 ? 0.85 : 0);
    if (t >= fix && t < fix + 800) {
      const u = easeInOutCubic(ramp(t, fix, fix + 800));
      h.placePulse("fixP", PUB, u, Math.min((t - fix) / 150, 1), 2.4);
    } else {
      h.hidePulse("fixP");
    }
    const tickOn = t >= 7800 ? 1 - ramp(t, 11600, 12000) : 0;
    h.setO("fixTick", tickOn * 0.95);
    h.setRing("ringPass", (t - 7800) / 550, 4, 14);
    if (t >= 8200 && t < 8800) {
      const u = easeInOutCubic(ramp(t, 8200, 8800));
      h.placePulse("ship", SHIP, u, 1, 2.6);
    } else {
      h.hidePulse("ship");
    }
    h.setRing("ringShip", (t - 8800) / 550, 4, 12);
    const v41 = t < 8800 ? 1 : 1 - ramp(t, 8800, 9100);
    const v42 = ramp(t, 8800, 9100);
    h.setO("v41", v41 * 0.9);
    h.setO("v42", v42 * 0.9);

    const cap = t >= 9300 && t < 12200 ? 1 : 0;
    h.setO("cap", cap * (0.5 + 0.3 * Math.sin(t / 300)));
  });

  return (
    <VisualCard rootRef={rootRef}>
      <svg viewBox="0 0 900 360" width="100%" className="block">
        <defs>
          <filter id="ck-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* ── Top lane: the invariant ── */}
        <path
          d={`M112 ${TOP} H380`}
          fill="none"
          stroke={LANE}
          strokeWidth={1.75}
        />
        <rect
          x={8}
          y={TOP - 15}
          width={104}
          height={30}
          rx={8}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <text
          x={60}
          y={TOP + 4}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={11}
          fill={SLATE}
        >
          clients
        </text>
        <circle
          ref={set("ringOK")}
          cx={112}
          cy={TOP}
          r={3}
          fill="none"
          stroke={GREEN}
          strokeWidth={1.5}
          opacity={0}
        />
        <rect
          x={380}
          y={TOP - 19}
          width={150}
          height={38}
          rx={9}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <text
          x={455}
          y={TOP + 4}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.16em"
          fill={SLATE}
        >
          GATEWAY
        </text>
        <text
          x={560}
          y={TOP + 4}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill={DIM}
        >
          serving the live composite · uninterrupted
        </text>

        {/* ── Bottom lane: the change pipeline ── */}
        <path
          d={`M148 ${BOT} H380`}
          fill="none"
          stroke={LANE}
          strokeWidth={1.5}
        />
        <path
          d={`M520 ${BOT} H700`}
          fill="none"
          stroke={LANE}
          strokeWidth={1.5}
        />
        <rect
          x={8}
          y={BOT - 26}
          width={140}
          height={52}
          rx={10}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <circle cx={30} cy={BOT - 6} r={4} fill={AMBER} />
        <text
          x={44}
          y={BOT - 2}
          fontFamily={MONO_FONT}
          fontSize={11}
          fill={SLATE}
        >
          pricing
        </text>
        <text
          ref={set("pubBad")}
          x={30}
          y={BOT + 16}
          fontFamily={MONO_FONT}
          fontSize={8.5}
          fill={CORAL}
          opacity={0}
        >
          publish price: Float!
        </text>
        <text
          ref={set("pubFix")}
          x={30}
          y={BOT + 16}
          fontFamily={MONO_FONT}
          fontSize={8.5}
          fill={GREEN}
          opacity={0}
        >
          publish price: Money!
        </text>

        <rect
          x={380}
          y={BOT - 32}
          width={140}
          height={64}
          rx={12}
          fill={SURFACE}
          stroke={TEAL}
          strokeOpacity={0.45}
        />
        <text
          x={450}
          y={BOT - 8}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.18em"
          fill={TEAL}
        >
          COMPOSE
        </text>
        <text
          ref={set("failX")}
          x={450}
          y={BOT + 16}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={12}
          fill={CORAL}
          opacity={0}
        >
          ✕ failed
        </text>
        <text
          ref={set("fixTick")}
          x={450}
          y={BOT + 16}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={12}
          fill={GREEN}
          opacity={0}
        >
          ✓ passed
        </text>
        <circle
          ref={set("ringFail")}
          cx={450}
          cy={BOT}
          r={4}
          fill="none"
          stroke={CORAL}
          strokeWidth={1.5}
          opacity={0}
        />
        <circle
          ref={set("ringPass")}
          cx={450}
          cy={BOT}
          r={4}
          fill="none"
          stroke={GREEN}
          strokeWidth={1.5}
          opacity={0}
        />

        <rect
          x={700}
          y={BOT - 19}
          width={176}
          height={38}
          rx={9}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <text
          x={716}
          y={BOT + 4}
          fontFamily={MONO_FONT}
          fontSize={10.5}
          fill={SLATE}
        >
          composite
        </text>
        <text
          ref={set("v41")}
          x={860}
          y={BOT + 4}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={10}
          fill={DIM}
          opacity={0.9}
        >
          v41
        </text>
        <text
          ref={set("v42")}
          x={860}
          y={BOT + 4}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={10}
          fill={GREEN}
          opacity={0}
        >
          v42
        </text>
        <text
          ref={set("unchanged")}
          x={788}
          y={BOT + 32}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={8.5}
          fill={DIM}
          opacity={0}
        >
          still v41 · still serving
        </text>
        <circle
          ref={set("ringShip")}
          cx={700}
          cy={BOT}
          r={4}
          fill="none"
          stroke={GREEN}
          strokeWidth={1.5}
          opacity={0}
        />

        {/* The diagnostic: exact, early, and invisible to clients. */}
        <g ref={set("diag")} opacity={0}>
          <rect
            x={330}
            y={276}
            width={440}
            height={58}
            rx={10}
            fill={SURFACE}
            stroke={CORAL}
            strokeOpacity={0.6}
          />
          <text
            x={350}
            y={299}
            fontFamily={MONO_FONT}
            fontSize={11}
            fill="#e8eef8"
          >
            OUTPUT_FIELD_TYPES_NOT_MERGEABLE
          </text>
          <text
            x={350}
            y={320}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={DIM}
          >
            Product.price: Money! (catalog) vs Float! (pricing) · failed in CI
          </text>
        </g>
        <line
          x1={30}
          x2={870}
          y1={140}
          y2={140}
          stroke={HAIR}
          strokeDasharray="3 6"
        />
        <text
          ref={set("cap")}
          x={450}
          y={352}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.14em"
          fill={GREEN}
          opacity={0}
        >
          the fix shipped · clients never noticed
        </text>

        <PulseGlyph
          set={set}
          id="req"
          main={TEAL}
          soft="#c8faf0"
          filter="ck-soft"
        />
        <PulseGlyph
          set={set}
          id="res"
          main={GREEN}
          soft="#a7f3d0"
          filter="ck-soft"
        />
        <PulseGlyph
          set={set}
          id="badP"
          main={CORAL}
          soft="#f6cabe"
          filter="ck-soft"
        />
        <PulseGlyph
          set={set}
          id="fixP"
          main={AMBER}
          soft="#fde9b8"
          filter="ck-soft"
        />
        <PulseGlyph
          set={set}
          id="ship"
          main={TEAL}
          soft="#c8faf0"
          filter="ck-soft"
        />
      </svg>
    </VisualCard>
  );
}
