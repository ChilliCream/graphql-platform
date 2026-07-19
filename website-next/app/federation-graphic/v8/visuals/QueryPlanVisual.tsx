"use client";

/**
 * The query plan made visible. One request enters the gateway on the left
 * spine; the gateway fans three canon streams up to Catalog, Billing and
 * Shipping, and a station card hangs on each stream carrying the exact
 * sub-operation the gateway sends that service, with its latency. The three
 * calls leave together; each answer returns at its own honest speed, so the
 * plan sits at 2/3 waiting only for the slow Billing call; then one merged
 * response lands in the JSON card on the right. The sub-op cards and their
 * latencies are always drawn: they are the plan, so any frozen frame tells the
 * whole story and beads only add the motion. On phones the same plan renders
 * as a short stacked column.
 */

import { MONO_FONT } from "../palette";
import type { ReactNode } from "react";
import { PulseGlyph, easeInOutCubic, measure, ramp, useVisual } from "./anim";
import { CANON, GatewayChip, INK_DIM, stream } from "./stage";

const T = 9000;

const NODE: readonly [number, number] = [540, 384];

const MARK_Y = 44;
const CARD_Y = 92;
const CARD_W = 232;
const SUB_ROWS = 5;
const CARD_H = 40 + SUB_ROWS * 18 + 12;

/** The three services the plan calls, each with its stream, card and pace. */
const STATIONS = [
  { s: 0, mx: 160, cx: 44, lat: "~80ms", field: "name", back: [2500, 3100] },
  { s: 1, mx: 540, cx: 424, lat: "~900ms", field: "price", back: [4300, 5100] },
  {
    s: 3,
    mx: 920,
    cx: 804,
    lat: "~300ms",
    field: "delivery",
    back: [2700, 3400],
  },
] as const;

const STREAMS = STATIONS.map((st) =>
  stream(st.mx, MARK_Y + 10, [NODE[0], NODE[1] - 13], 0.5),
);

/** The real sub-operation the gateway sends one service. */
function subOp(field: string): readonly string[] {
  return ["{", '  productById(id: "P-401") {', `    ${field}`, "  }", "}"];
}

const Q = { x: 36, y: 295, w: 260 } as const;
const QUERY_LINES = [
  { code: "{", bar: undefined },
  { code: '  product(id: "P-401") {', bar: "#ffffff" },
  { code: "    name", bar: CANON[0].color },
  { code: "    price", bar: CANON[1].color },
  { code: "    delivery", bar: CANON[3].color },
  { code: "  }", bar: undefined },
  { code: "}", bar: undefined },
] as const;

const R = { x: 784, y: 295, w: 260 } as const;
const R_LINES = [
  "{",
  '  "product": {',
  '    "name": "Aero Mug",',
  '    "price": "24.90 EUR",',
  '    "delivery": "2d"',
  "  }",
  "}",
] as const;

const Q_IN = measure([
  [Q.x + Q.w, NODE[1]],
  [NODE[0] - 46, NODE[1]],
]);
const TO_CARD = measure([
  [NODE[0] + 46, NODE[1]],
  [R.x, NODE[1]],
]);

export function QueryPlanVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // The plan reads the query: gutter bars glow in owner order.
    QUERY_LINES.forEach((l, i) => {
      if (!l.bar) {
        return;
      }
      const glow =
        t >= 700 + i * 150 && t < 1800 ? 0.65 + 0.35 * Math.sin(t / 110) : 1;
      h.setO(`bar${i}`, ramp(t, 300 + i * 100, 450 + i * 100) * 0.95 * glow);
    });

    // One request into the gateway.
    if (t >= 400 && t < 1100) {
      h.placePulse(
        "q",
        Q_IN,
        easeInOutCubic(ramp(t, 400, 1100)),
        Math.min((t - 400) / 130, 1),
        2.6,
      );
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringQ", (t - 1100) / 500, 16, 30);

    // Three calls leave together; each service answers at its own pace.
    STATIONS.forEach((st, k) => {
      if (t >= 1500 && t < 2200) {
        const u = easeInOutCubic(ramp(t, 1500, 2200)) * 0.96;
        h.placePulse(`up${k}`, STREAMS[k].up, u, 0.85, 1.9);
      } else {
        h.hidePulse(`up${k}`);
      }
      h.setRing(`ringS${k}`, (t - 2200) / 450, 4, 9);
      h.setO(
        `glow${k}`,
        (ramp(t, 1950, 2250) - ramp(t, st.back[0], st.back[1])) * 0.09,
      );
      if (t >= st.back[0] && t < st.back[1]) {
        const u = 0.06 + easeInOutCubic(ramp(t, st.back[0], st.back[1])) * 0.94;
        h.placePulse(`dn${k}`, STREAMS[k].poly, u, 1, 2.2);
      } else {
        h.hidePulse(`dn${k}`);
      }
      const on =
        ramp(t, st.back[1], st.back[1] + 150) * (1 - ramp(t, 8300, 8600));
      h.setO(`got${k}`, on * 0.95);
    });

    // Two are in; the plan waits only for the slowest.
    const wait = t >= 3500 && t < 4300 ? 1 : 0;
    h.setO("waiting", wait * (0.5 + 0.35 * Math.sin(t / 250)));

    // Merge; one response into the JSON card. The rows only dim while the
    // graph is traveling, and never below the house floor.
    h.setRing("ringM", (t - 5200) / 500, 16, 30);
    if (t >= 5400 && t < 6100) {
      h.placePulse(
        "resp",
        TO_CARD,
        easeInOutCubic(ramp(t, 5400, 6100)),
        1,
        2.7,
      );
    } else {
      h.hidePulse("resp");
    }
    R_LINES.forEach((_, j) => {
      const dim = ramp(t, 500, 800);
      const pop = easeInOutCubic(ramp(t, 6100 + j * 140, 6240 + j * 140));
      const v = 1 - 0.55 * dim * (1 - pop);
      h.setPop(`r${j}`, v, v);
    });
    const tag = easeInOutCubic(ramp(t, 6900, 7300)) * (1 - ramp(t, 8400, 8700));
    h.setPop("tag", tag * 0.92, tag);
  });

  return (
    <div ref={rootRef} aria-hidden="true">
      {/* Phones: the same plan as a short stacked column. */}
      <div className="space-y-4 sm:hidden">
        <MobileCard
          label="One request"
          lines={QUERY_LINES.map((l) => ({
            text: l.code,
            dot:
              l.bar === CANON[0].color ||
              l.bar === CANON[1].color ||
              l.bar === CANON[3].color
                ? l.bar
                : undefined,
          }))}
        />
        <p className="text-cc-nav-label text-center font-mono text-[10px] tracking-[0.2em] uppercase">
          The plan · 3 calls · in parallel
        </p>
        {STATIONS.map((st) => (
          <MobileCard
            key={st.s}
            label={CANON[st.s].name}
            color={CANON[st.s].color}
            lat={st.lat}
            lines={subOp(st.field).map((text, i) => ({
              text,
              bright: i === 2,
            }))}
          />
        ))}
        <MobileCard
          label="One response"
          lines={R_LINES.map((text, i) => ({
            text,
            dot:
              i >= 2 && i <= 4
                ? [CANON[0].color, CANON[1].color, CANON[3].color][i - 2]
                : undefined,
          }))}
        />
      </div>

      {/* Larger screens: the animated plan canvas. */}
      <div className="hidden overflow-x-auto sm:block">
        <svg
          viewBox="0 0 1080 504"
          width="100%"
          className="block min-w-[900px]"
        >
          <defs>
            <filter id="q6-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* The three service streams, fanning up out of the gateway. */}
          {STREAMS.map((s, k) => (
            <path
              key={k}
              d={s.d}
              fill="none"
              stroke={CANON[STATIONS[k].s].color}
              strokeWidth={2.5}
              strokeOpacity={0.85}
              strokeLinecap="round"
            />
          ))}

          {/* The spine lanes into and out of the gateway. */}
          <path
            d={`M${Q.x + Q.w} ${NODE[1]} H${NODE[0] - 46}`}
            fill="none"
            stroke="rgba(139,160,188,0.4)"
            strokeWidth={1.5}
          />
          <path
            d={`M${NODE[0] + 46} ${NODE[1]} H${R.x}`}
            fill="none"
            stroke="rgba(139,160,188,0.4)"
            strokeWidth={1.5}
          />

          {/* Beads ride the wires, passing behind the cards. */}
          <PulseGlyph
            set={set}
            id="q"
            main="#ffffff"
            soft="#ffffff"
            filter="q6-soft"
          />
          <PulseGlyph
            set={set}
            id="resp"
            main="#ffffff"
            soft="#ffffff"
            filter="q6-soft"
          />
          {STATIONS.map((st, k) => (
            <g key={k}>
              <PulseGlyph
                set={set}
                id={`up${k}`}
                main="#ffffff"
                soft="#ffffff"
                filter="q6-soft"
              />
              <PulseGlyph
                set={set}
                id={`dn${k}`}
                main={CANON[st.s].color}
                soft={CANON[st.s].soft}
                filter="q6-soft"
              />
            </g>
          ))}

          {/* Each stream carries its service's sub-operation as a station. */}
          {STATIONS.map((st, k) => {
            const color = CANON[st.s].color;
            return (
              <g key={k}>
                <rect
                  x={st.cx}
                  y={CARD_Y}
                  width={CARD_W}
                  height={CARD_H}
                  rx={12}
                  fill="#0d1424"
                  stroke="rgba(245,241,234,0.13)"
                />
                <rect
                  ref={set(`glow${k}`)}
                  x={st.cx}
                  y={CARD_Y}
                  width={CARD_W}
                  height={CARD_H}
                  rx={12}
                  fill={color}
                  opacity={0}
                />
                <rect
                  x={st.cx + 14}
                  y={CARD_Y + 11}
                  width={10}
                  height={10}
                  rx={3}
                  fill={color}
                />
                <text
                  x={st.cx + 32}
                  y={CARD_Y + 20}
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  letterSpacing="0.16em"
                  fill={INK_DIM}
                >
                  {CANON[st.s].name.toUpperCase()}
                </text>
                <rect
                  x={st.cx + CARD_W - 54}
                  y={CARD_Y + 9}
                  width={42}
                  height={16}
                  rx={8}
                  fill={color}
                  opacity={0.16}
                />
                <text
                  x={st.cx + CARD_W - 33}
                  y={CARD_Y + 20}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={9}
                  fill={color}
                >
                  {st.lat}
                </text>
                <line
                  x1={st.cx}
                  x2={st.cx + CARD_W}
                  y1={CARD_Y + 32}
                  y2={CARD_Y + 32}
                  stroke="rgba(245,241,234,0.1)"
                />
                {subOp(st.field).map((code, i) => (
                  <text
                    key={i}
                    x={st.cx + 16}
                    y={CARD_Y + 50 + i * 18}
                    xmlSpace="preserve"
                    fontFamily={MONO_FONT}
                    fontSize={11.5}
                    fill={i === 2 ? "#e8eef8" : "#c9d4e8"}
                  >
                    {code}
                  </text>
                ))}
                {/* The stream's source marker and its arrival ring. */}
                <rect
                  x={st.mx - 6}
                  y={MARK_Y - 6}
                  width={12}
                  height={12}
                  rx={3}
                  fill={color}
                />
                <circle
                  ref={set(`ringS${k}`)}
                  cx={st.mx}
                  cy={MARK_Y + 10}
                  r={4}
                  fill="none"
                  stroke={color}
                  strokeWidth={1.5}
                  opacity={0}
                />
              </g>
            );
          })}

          {/* The runtime gateway and its arrival rings. */}
          <GatewayChip x={NODE[0]} y={NODE[1]} />
          <circle
            ref={set("ringQ")}
            cx={NODE[0]}
            cy={NODE[1]}
            r={16}
            fill="none"
            stroke="#fff"
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringM")}
            cx={NODE[0]}
            cy={NODE[1]}
            r={16}
            fill="none"
            stroke="#fff"
            strokeWidth={1.5}
            opacity={0}
          />

          {/* The plan's caption and its 2/3 progress beat. */}
          <text
            x={NODE[0]}
            y={414}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.2em"
            fill="#c9d4e8"
          >
            THE PLAN · 3 CALLS · IN PARALLEL
          </text>
          <text
            x={NODE[0]}
            y={431}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.18em"
            fill={INK_DIM}
            opacity={0.75}
          >
            QUERY PLAN · CACHED
          </text>
          {STATIONS.map((st, k) => (
            <g key={k}>
              <circle
                cx={NODE[0] - 20 + k * 20}
                cy={456}
                r={3.5}
                fill="none"
                stroke={CANON[st.s].color}
                strokeWidth={1}
                opacity={0.3}
              />
              <circle
                ref={set(`got${k}`)}
                cx={NODE[0] - 20 + k * 20}
                cy={456}
                r={3.5}
                fill={CANON[st.s].color}
                opacity={0.95}
              />
            </g>
          ))}
          <text
            ref={set("waiting")}
            x={NODE[0]}
            y={476}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={CANON[1].color}
            opacity={0}
          >
            2/3 · waiting on billing
          </text>

          {/* One request, with owners in the gutter. */}
          <rect
            x={Q.x}
            y={Q.y}
            width={Q.w}
            height={40 + QUERY_LINES.length * 18 + 12}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <text
            x={Q.x + 14}
            y={Q.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            ONE REQUEST
          </text>
          <line
            x1={Q.x}
            x2={Q.x + Q.w}
            y1={Q.y + 32}
            y2={Q.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {QUERY_LINES.map((l, i) => (
            <g key={i}>
              {l.bar && (
                <rect
                  ref={set(`bar${i}`)}
                  x={Q.x + 10}
                  y={Q.y + 40 + i * 18}
                  width={3}
                  height={12}
                  rx={1.5}
                  fill={l.bar}
                  opacity={0.95}
                />
              )}
              <text
                x={Q.x + 22}
                y={Q.y + 50 + i * 18}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={11.5}
                fill="#c9d4e8"
              >
                {l.code}
              </text>
            </g>
          ))}

          {/* One response: JSON, owners still visible. */}
          <rect
            x={R.x}
            y={R.y}
            width={R.w}
            height={40 + R_LINES.length * 18 + 12}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(94,234,212,0.35)"
          />
          <text
            x={R.x + 14}
            y={R.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill="#5eead4"
          >
            ONE RESPONSE
          </text>
          <line
            x1={R.x}
            x2={R.x + R.w}
            y1={R.y + 32}
            y2={R.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {R_LINES.map((code, j) => (
            <g key={j} ref={set(`r${j}`)} opacity={1}>
              <text
                x={R.x + 16}
                y={R.y + 50 + j * 18}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={11.5}
                fill={j <= 1 || j >= 5 ? "#c9d4e8" : "#e8eef8"}
              >
                {code}
              </text>
              {j >= 2 && j <= 4 && (
                <circle
                  cx={R.x + R.w - 22}
                  cy={R.y + 46 + j * 18}
                  r={3}
                  fill={[CANON[0].color, CANON[1].color, CANON[3].color][j - 2]}
                />
              )}
            </g>
          ))}
          <g ref={set("tag")} opacity={0.92}>
            <text
              x={R.x + 14}
              y={R.y + 40 + R_LINES.length * 18 + 36}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.18em"
              fill={INK_DIM}
            >
              200 · ONE ROUND TRIP
            </text>
          </g>
        </svg>
      </div>
    </div>
  );
}

interface MobileLine {
  readonly text: string;
  readonly dot?: string;
  readonly bright?: boolean;
}

interface MobileCardProps {
  readonly label: string;
  readonly color?: string;
  readonly lat?: string;
  readonly lines: readonly MobileLine[];
}

function MobileCard({ label, color, lat, lines }: MobileCardProps): ReactNode {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <div className="flex items-center gap-2">
        {color && (
          <span
            className="inline-block h-2.5 w-2.5 rounded-[3px]"
            style={{ background: color }}
          />
        )}
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          {label}
        </span>
        {lat && color && (
          <span
            className="ml-auto rounded-full px-2 py-0.5 font-mono text-[10px]"
            style={{ color, background: `${color}22` }}
          >
            {lat}
          </span>
        )}
      </div>
      <div className="border-cc-card-border mt-2 overflow-x-auto border-t pt-2 font-mono text-[12px] leading-6">
        {lines.map((l, i) => (
          <div key={i} className="flex items-center gap-2">
            <span
              className={
                "whitespace-pre " +
                (l.bright ? "text-[#e8eef8]" : "text-[#c9d4e8]")
              }
            >
              {l.text}
            </span>
            {l.dot && (
              <span
                className="ml-auto inline-block h-2 w-2 rounded-full"
                style={{ background: l.dot }}
              />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
