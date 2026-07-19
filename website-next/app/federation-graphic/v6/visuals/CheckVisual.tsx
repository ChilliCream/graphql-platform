"use client";

/**
 * Checks: the conflict is in the SDL, so the SDL is on screen. Catalog and
 * Billing both declare price — with different types. Billing's chip drops
 * into the node and is deflected with the exact diagnostic while query beads
 * ride the live output line without a single gap. Billing's card then fixes
 * its line, the chip drops again, and the version bumps. Build-time failure,
 * runtime calm.
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
  SchemaCard,
  sampleCubic,
  stream,
} from "./stage";

const T = 13000;

const CAT = { x: 90, y: 30, w: 230 } as const;
const BILL = { x: 580, y: 30, w: 230 } as const;
const NODE: readonly [number, number] = [450, 280];

const S_CAT = stream(CAT.x + CAT.w / 2, 30 + 40 + 4 * 18 + 12, NODE, 0.3);
const S_BILL = stream(BILL.x + BILL.w / 2, 30 + 40 + 4 * 18 + 12, NODE, 0.3);

const OUT_UP = measure([
  [450, 452],
  [450, 292],
]);
const OUT_DOWN = measure([
  [450, 292],
  [450, 452],
]);
const REJECT = measure(
  sampleCubic([450, 282], [420, 316], [370, 336], [318, 348]).pts,
);

export function CheckVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // Traffic never stops.
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

    // Billing's line: Float! until the fix, Money! after.
    const fixed = ramp(t, 6900, 7100);
    h.setO("lineBad", 1 - fixed);
    h.setO("lineGood", fixed);

    // The bad chip drops and is deflected.
    const bad = 1300;
    if (t >= bad && t < bad + 800) {
      const u = easeInOutCubic(ramp(t, bad, bad + 800));
      const [x, y] = pointAt(S_BILL.poly, u);
      h.setX("chipBad", x, y);
      h.setO("chipBad", 1);
    } else if (t >= bad + 800 && t < bad + 1500) {
      const u = easeInOutCubic(ramp(t, bad + 800, bad + 1500));
      const [x, y] = pointAt(REJECT, u);
      h.setX("chipBad", x, y);
      h.setO("chipBad", 1 - 0.35 * u);
    } else {
      h.setO("chipBad", 0);
    }
    h.setRing("ringFail", (t - (bad + 800)) / 550, 11, 22);
    const diag =
      easeInOutCubic(ramp(t, 2400, 2800)) * (1 - ramp(t, 6300, 6700));
    h.setPop("diag", diag * 0.95, diag);
    const still = t >= 2600 && t < 4800 ? 0.7 : 0;
    h.setO("still", still * (0.6 + 0.3 * Math.sin(t / 240)));

    // The fix drops; the version bumps.
    const fix = 7400;
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
    h.setO("v41", (t < 8700 ? 1 : 1 - ramp(t, 8700, 9000)) * 0.85);
    h.setO("v42", ramp(t, 8700, 9000) * 0.9);
    const cap = t >= 9400 && t < 12200 ? 1 : 0;
    h.setO("cap", cap * (0.5 + 0.3 * Math.sin(t / 300)));
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      <svg viewBox="0 0 900 500" width="100%" className="block">
        <defs>
          <filter id="k6-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Streams. */}
        <path
          d={S_CAT.d}
          fill="none"
          stroke={CANON[0].color}
          strokeWidth={2}
          strokeOpacity={0.8}
          strokeLinecap="round"
        />
        <path
          d={S_BILL.d}
          fill="none"
          stroke={CANON[1].color}
          strokeWidth={2}
          strokeOpacity={0.8}
          strokeLinecap="round"
        />

        {/* Two owners, one field, two types. */}
        <SchemaCard
          x={CAT.x}
          y={CAT.y}
          w={CAT.w}
          label={CANON[0].name}
          color={CANON[0].color}
          lines={[
            { code: "type Product {" },
            { code: "  id: ID!", dim: true },
            { code: "  price: Money!" },
            { code: "}" },
          ]}
        />
        {/* Billing's card: the price line is dynamic. */}
        <SchemaCard
          x={BILL.x}
          y={BILL.y}
          w={BILL.w}
          label={CANON[1].name}
          color={CANON[1].color}
          lines={[
            { code: "type Product {" },
            { code: "  id: ID!", dim: true },
            { code: "" },
            { code: "}" },
          ]}
        />
        <text
          ref={set("lineBad")}
          x={BILL.x + 16}
          y={BILL.y + 48 + 2 * 18}
          fontFamily={MONO_FONT}
          fontSize={12}
          fill="#f27765"
          opacity={1}
        >
          {"  price: Float!"}
        </text>
        <text
          ref={set("lineGood")}
          x={BILL.x + 16}
          y={BILL.y + 48 + 2 * 18}
          fontFamily={MONO_FONT}
          fontSize={12}
          fill="#66be77"
          opacity={0}
        >
          {"  price: Money!"}
        </text>

        {/* Node, verdicts, versions. */}
        <GlowNode x={NODE[0]} y={NODE[1]} id="k6-node" r={8} />
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

        {/* The live line. */}
        <rect
          x={449.25}
          y={292}
          width={1.5}
          height={164}
          fill="#f5f0ea"
          opacity={0.4}
        />
        <text
          x={466}
          y={448}
          fontFamily={MONO_FONT}
          fontSize={9}
          letterSpacing="0.16em"
          fill={INK_DIM}
          opacity={0.6}
        >
          LIVE TRAFFIC · UNINTERRUPTED
        </text>

        {/* Chips and the diagnostic. */}
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
          <rect
            x={40}
            y={366}
            width={340}
            height={86}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(242,119,101,0.5)"
          />
          <text
            x={58}
            y={392}
            fontFamily={MONO_FONT}
            fontSize={10.5}
            fill="#f27765"
          >
            ✕ OUTPUT_FIELD_TYPES_NOT_MERGEABLE
          </text>
          <text
            x={58}
            y={412}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            fill={INK_DIM}
          >
            Product.price: Money! (catalog) vs Float! (billing)
          </text>
          <text
            x={58}
            y={432}
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
          y={492}
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
          filter="k6-soft"
        />
        <PulseGlyph
          set={set}
          id="res"
          main="#66be77"
          soft="#bce5c4"
          filter="k6-soft"
        />
      </svg>
    </div>
  );
}
