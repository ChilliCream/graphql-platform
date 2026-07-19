"use client";

/**
 * Checks in the stream language. Query and response beads ride the output
 * line the entire loop — the live composite never blinks. Below the surface,
 * Billing drops a contract chip that conflicts with Catalog's price type: the
 * node flashes red, the chip is deflected aside with the exact diagnostic,
 * the fix drops, the version bumps. The top of the river never noticed.
 */

import { MONO_FONT } from "../palette";
import {
  PulseGlyph,
  easeInOutCubic,
  measure,
  pointAt,
  ramp,
  useVisual,
} from "./anim";
import {
  CANON,
  GlowNode,
  INK_DIM,
  NodeCaption,
  StreamMarker,
  sampleCubic,
  stream,
} from "./stage";

const T = 13000;

const BILLING = { x: 280, y: 58 } as const;
const CATALOG = { x: 640, y: 70 } as const;
const NODE: readonly [number, number] = [450, 260];

const S_BILL = stream(BILLING.x, BILLING.y + 8, NODE);
const S_CAT = stream(CATALOG.x, CATALOG.y + 8, NODE);

const OUT_UP = measure([
  [450, 424],
  [450, 272],
]);
const OUT_DOWN = measure([
  [450, 272],
  [450, 424],
]);

// The rejected chip is deflected off the node, down and to the left.
const REJECT = measure(
  sampleCubic([450, 262], [430, 292], [392, 310], [356, 318]).pts,
);

export function CheckVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // ── Traffic never stops: up beads and down beads all loop long ──────
    const tc = (t - 100 + T) % 1300;
    if (tc < 500) {
      h.placePulse(
        "req",
        OUT_UP,
        easeInOutCubic(tc / 500),
        Math.min(tc / 120, 1),
        2.2,
      );
    } else {
      h.hidePulse("req");
    }
    if (tc >= 620 && tc < 1120) {
      h.placePulse("res", OUT_DOWN, easeInOutCubic((tc - 620) / 500), 1, 2.2);
    } else {
      h.hidePulse("res");
    }

    // ── The bad publish ─────────────────────────────────────────────────
    const bad = 1400;
    h.setO("pubBad", t >= bad - 300 && t < bad + 700 ? 0.8 : 0);
    if (t >= bad && t < bad + 800) {
      const u = easeInOutCubic(ramp(t, bad, bad + 800));
      const [x, y] = pointAt(S_BILL.poly, u);
      h.setX("chipBad", x, y);
      h.setO("chipBad", 1);
    } else if (t >= bad + 800 && t < bad + 1500) {
      // Deflected: the chip never makes it into the composite.
      const u = easeInOutCubic(ramp(t, bad + 800, bad + 1500));
      const [x, y] = pointAt(REJECT, u);
      h.setX("chipBad", x, y);
      h.setO("chipBad", 1 - 0.4 * u);
    } else {
      h.setO("chipBad", 0);
    }
    h.setRing("ringFail", (t - (bad + 800)) / 550, 11, 22);
    const diag =
      easeInOutCubic(ramp(t, 2500, 2900)) * (1 - ramp(t, 6400, 6800));
    h.setPop("diag", diag * 0.95, diag);
    const unch = t >= 2600 && t < 4800 ? 0.7 : 0;
    h.setO("still", unch * (0.6 + 0.3 * Math.sin(t / 240)));

    // ── The fix ─────────────────────────────────────────────────────────
    const fix = 7200;
    h.setO("pubFix", t >= fix - 300 && t < fix + 700 ? 0.8 : 0);
    if (t >= fix && t < fix + 800) {
      const u = easeInOutCubic(ramp(t, fix, fix + 800));
      const [x, y] = pointAt(S_BILL.poly, u);
      h.setX("chipFix", x, y);
      h.setO("chipFix", 1);
    } else {
      h.setO("chipFix", 0);
    }
    h.setRing("ringPass", (t - (fix + 800)) / 550, 11, 22);
    h.setO("passTick", t >= fix + 900 ? (1 - ramp(t, 11600, 12000)) * 0.95 : 0);
    const v41 = t < 8600 ? 1 : 1 - ramp(t, 8600, 8900);
    h.setO("v41", v41 * 0.85);
    h.setO("v42", ramp(t, 8600, 8900) * 0.9);

    const cap = t >= 9300 && t < 12200 ? 1 : 0;
    h.setO("cap", cap * (0.5 + 0.3 * Math.sin(t / 300)));
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 440" width="100%" className="block">
        <defs>
          <filter id="kv-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Two owners of Product, one of them about to be wrong. */}
        <path
          d={S_BILL.d}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={2}
          strokeOpacity={0.85}
          strokeLinecap="round"
        />
        <path
          d={S_CAT.d}
          fill="none"
          stroke={CANON[0].color}
          strokeWidth={2}
          strokeOpacity={0.85}
          strokeLinecap="round"
        />
        <StreamMarker
          x={BILLING.x}
          y={BILLING.y}
          color={CANON[1].color}
          label={CANON[1].name}
        />
        <StreamMarker
          x={CATALOG.x}
          y={CATALOG.y}
          color={CANON[0].color}
          label={CANON[0].name}
        />
        <text
          ref={set("pubBad")}
          x={BILLING.x + 16}
          y={BILLING.y + 22}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill="#f27765"
          opacity={0}
        >
          publish price: Float!
        </text>
        <text
          ref={set("pubFix")}
          x={BILLING.x + 16}
          y={BILLING.y + 22}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill="#66be77"
          opacity={0}
        >
          publish price: Money!
        </text>
        <text
          x={CATALOG.x + 16}
          y={CATALOG.y + 22}
          fontFamily={MONO_FONT}
          fontSize={9}
          fill={INK_DIM}
          opacity={0.7}
        >
          price: Money!
        </text>

        {/* The output line: the part clients live on. */}
        <rect
          x={449.25}
          y={272}
          width={1.5}
          height={156}
          fill="#f5f0ea"
          opacity={0.4}
        />
        <text
          x={466}
          y={420}
          fontFamily={MONO_FONT}
          fontSize={9}
          letterSpacing="0.16em"
          fill={INK_DIM}
          opacity={0.6}
        >
          LIVE TRAFFIC · UNINTERRUPTED
        </text>

        <GlowNode x={NODE[0]} y={NODE[1]} id="kv-node" r={8} />
        <NodeCaption x={330} y={NODE[1]} label="compose check" toX={424} />
        <circle
          ref={set("ringFail")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke="#f27765"
          strokeWidth={1.5}
          opacity={0}
        />
        <circle
          ref={set("ringPass")}
          cx={NODE[0]}
          cy={NODE[1]}
          r={11}
          fill="none"
          stroke="#66be77"
          strokeWidth={1.5}
          opacity={0}
        />
        <text
          ref={set("passTick")}
          x={520}
          y={NODE[1] + 4}
          fontFamily={MONO_FONT}
          fontSize={10.5}
          fill="#66be77"
          opacity={0}
        >
          ✓ composed
        </text>

        {/* Version tags. */}
        <text
          ref={set("v41")}
          x={520}
          y={NODE[1] + 26}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.16em"
          fill={INK_DIM}
          opacity={0.85}
        >
          COMPOSITE · V41
        </text>
        <text
          ref={set("v42")}
          x={520}
          y={NODE[1] + 26}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.16em"
          fill="#66be77"
          opacity={0}
        >
          COMPOSITE · V42
        </text>
        <text
          ref={set("still")}
          x={520}
          y={NODE[1] + 44}
          fontFamily={MONO_FONT}
          fontSize={8.5}
          fill={INK_DIM}
          opacity={0}
        >
          still v41 · still serving
        </text>

        {/* The deflected chip and its diagnostic. */}
        <g ref={set("chipBad")} opacity={0}>
          <rect
            x={-5}
            y={-5}
            width={10}
            height={10}
            rx={2.5}
            fill={CANON[1].color}
            stroke="#f27765"
            strokeWidth={1.5}
          />
        </g>
        <g ref={set("chipFix")} opacity={0}>
          <rect
            x={-5}
            y={-5}
            width={10}
            height={10}
            rx={2.5}
            fill={CANON[1].color}
          />
        </g>
        <g ref={set("diag")} opacity={0}>
          <text
            x={70}
            y={332}
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#f27765"
          >
            ✕ OUTPUT_FIELD_TYPES_NOT_MERGEABLE
          </text>
          <text
            x={70}
            y={352}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
          >
            Product.price — Money! (catalog) vs Float! (billing)
          </text>
          <text
            x={70}
            y={372}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
            opacity={0.7}
          >
            failed in CI · nothing deployed
          </text>
        </g>

        <text
          ref={set("cap")}
          x={450}
          y={436}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.2em"
          fill="#66be77"
          opacity={0}
        >
          THE FIX SHIPPED · CLIENTS NEVER NOTICED
        </text>

        <PulseGlyph
          set={set}
          id="req"
          main="#f5f0ea"
          soft="#ffffff"
          filter="kv-soft"
        />
        <PulseGlyph
          set={set}
          id="res"
          main="#66be77"
          soft="#bce5c4"
          filter="kv-soft"
        />
      </svg>
    </div>
  );
}
