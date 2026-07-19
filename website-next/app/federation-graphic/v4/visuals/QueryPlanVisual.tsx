"use client";

/**
 * Query planning, animated. One teal query enters the gateway; the plan
 * splits it into three colored calls. Catalog answers fast, inventory mid,
 * reviews slow — the merge visibly waits for the stragglers before ONE teal
 * response returns to the client. The story is the wait at 2/3: independent
 * services, independent speeds, one round trip for the client.
 */

import { CYAN, GREEN, MONO_FONT, SLATE, TEAL, VIOLET } from "../palette";
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

const T = 11000;
const MID = 150;

const TEAL_SOFT = "#c8faf0";
const CYAN_SOFT = "#b7e8f7";
const GREEN_SOFT = "#a7f3d0";
const VIOLET_SOFT = "#cdd7f2";

const ROWS = [105, 150, 195] as const;
const SERV_Y = [72, 150, 228] as const;

const qIn = measure([
  [112, MID],
  [206, MID],
]);
const qOut = measure([
  [200, MID],
  [92, MID],
]);
const lanes = [
  measure([
    [380, ROWS[0]],
    [470, ROWS[0]],
    [540, SERV_Y[0]],
    [626, SERV_Y[0]],
  ]),
  measure([
    [380, ROWS[1]],
    [626, ROWS[1]],
  ]),
  measure([
    [380, ROWS[2]],
    [470, ROWS[2]],
    [540, SERV_Y[2]],
    [626, SERV_Y[2]],
  ]),
];
const returns = lanes.map((l) => measure([...l.pts].reverse()));

const SERVICES = [
  { name: "catalog", color: CYAN, soft: CYAN_SOFT, lat: "~80ms" },
  { name: "inventory", color: GREEN, soft: GREEN_SOFT, lat: "~300ms" },
  { name: "reviews", color: VIOLET, soft: VIOLET_SOFT, lat: "~1.2s" },
] as const;

// Departures/returns per service: [workEnd, returnEnd].
const WORK = [
  [3200, 4100],
  [3700, 4600],
  [5300, 6200],
] as const;

function laneD(l: {
  readonly pts: readonly (readonly [number, number])[];
}): string {
  return l.pts.map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`).join(" ");
}

export function QueryPlanVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    const master = 1 - ramp(t, 10500, 10850);

    // 1 · the query arrives at the gateway.
    if (t >= 300 && t < 1200) {
      const u = easeInOutCubic(ramp(t, 300, 1200));
      h.placePulse("q", qIn, u, Math.min((t - 300) / 150, 1), 2.6);
    } else {
      h.hidePulse("q");
    }
    h.setRing("ringG", (t - 1200) / 600, 4, 14);

    // 2 · the plan: three rows light in sequence — the split made visible.
    for (let i = 0; i < 3; i++) {
      const on = ramp(t, 1250 + i * 200, 1400 + i * 200);
      const off = 1 - ramp(t, 9200, 9800);
      h.setO(`plan${i}`, Math.min(on, off) * 0.85);
    }

    // 3 · three calls depart together at 2000; arrive 2900.
    for (let i = 0; i < 3; i++) {
      if (t >= 2000 && t < 2900) {
        const u = easeInOutCubic(ramp(t, 2000, 2900));
        h.placePulse(`c${i}`, lanes[i], u, Math.min((t - 2000) / 150, 1), 2.4);
      } else {
        h.hidePulse(`c${i}`);
      }
      h.setRing(`ringS${i}`, (t - 2900) / 550, 3, 10);
      // Service works until its own WORK end; the row stays warm while busy.
      const busy = t >= 2900 && t < WORK[i][0] ? 0.9 : 0;
      h.setO(`busy${i}`, busy * (0.55 + 0.35 * Math.sin(t / 120)));
      // The response travels home.
      if (t >= WORK[i][0] && t < WORK[i][1]) {
        const u = easeInOutCubic(ramp(t, WORK[i][0], WORK[i][1]));
        h.placePulse(`r${i}`, returns[i], u, 1, 2.4);
      } else {
        h.hidePulse(`r${i}`);
      }
      // Merge slot fills when the response lands.
      const land = ramp(t, WORK[i][1], WORK[i][1] + 150);
      const clear = 1 - ramp(t, 9200, 9800);
      h.setO(`slot${i}`, Math.min(land, clear) * 0.95);
    }

    // 4 · the wait: two of three in, reviews still working.
    const waitOn = t >= 4750 && t < 6200 ? 1 : 0;
    h.setO("waiting", waitOn * (0.55 + 0.35 * Math.sin(t / 260)));

    // 5 · merge + one response home; the tag holds.
    const mergeFlash = t >= 6200 && t < 6650 ? 1 - ramp(t, 6200, 6650) : 0;
    h.setO("merge", mergeFlash * 0.9);
    if (t >= 6500 && t < 7400) {
      const u = easeInOutCubic(ramp(t, 6500, 7400));
      h.placePulse("resp", qOut, u, 1, 2.8);
    } else {
      h.hidePulse("resp");
    }
    h.setRing("ringC", (t - 7400) / 600, 4, 12);
    const tag = easeInOutCubic(ramp(t, 7400, 7900));
    h.setPop("tag", tag * 0.92 * master, tag);
  });

  return (
    <VisualCard rootRef={rootRef}>
      <svg viewBox="0 0 900 300" width="100%" className="block">
        <defs>
          <filter id="qp-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Lanes. */}
        <path
          d={`M112 ${MID} H200`}
          fill="none"
          stroke={LANE}
          strokeWidth={1.75}
        />
        {lanes.map((l, i) => (
          <path
            key={i}
            d={laneD(l)}
            fill="none"
            stroke={LANE}
            strokeWidth={1.5}
          />
        ))}

        {/* Client chip. */}
        <rect
          x={8}
          y={MID - 15}
          width={104}
          height={30}
          rx={8}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <text
          x={60}
          y={MID + 4}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={11}
          fill={SLATE}
        >
          client
        </text>
        <circle
          ref={set("ringC")}
          cx={60}
          cy={MID - 15}
          r={4}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />
        {/* The held response tag: the meaningful reduced-motion frame. */}
        <g ref={set("tag")} opacity={0.92}>
          <rect
            x={8}
            y={MID + 24}
            width={150}
            height={22}
            rx={6}
            fill={SURFACE}
            stroke={TEAL}
            strokeOpacity={0.5}
          />
          <text
            x={83}
            y={MID + 39}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={TEAL}
          >
            200 · one round trip
          </text>
        </g>

        {/* Gateway panel with the plan rows. */}
        <rect
          x={200}
          y={52}
          width={180}
          height={196}
          rx={12}
          fill={SURFACE}
          stroke={PANEL_STROKE}
        />
        <text
          x={216}
          y={76}
          fontFamily={MONO_FONT}
          fontSize={10}
          letterSpacing="0.18em"
          fill={SLATE}
        >
          GATEWAY
        </text>
        <line x1={200} x2={380} y1={86} y2={86} stroke={HAIR} />
        <circle
          ref={set("ringG")}
          cx={206}
          cy={MID}
          r={4}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />
        <rect
          ref={set("merge")}
          x={200}
          y={52}
          width={180}
          height={196}
          rx={12}
          fill="none"
          stroke={TEAL}
          strokeWidth={1.5}
          opacity={0}
        />
        {ROWS.map((y, i) => (
          <g key={i}>
            <rect
              ref={set(`plan${i}`)}
              x={214}
              y={y - 12}
              width={152}
              height={24}
              rx={6}
              fill="rgba(139,160,188,0.08)"
              stroke={SERVICES[i].color}
              strokeOpacity={0.55}
              opacity={0.85}
            />
            <text
              x={226}
              y={y + 4}
              fontFamily={MONO_FONT}
              fontSize={10}
              fill={DIM}
            >
              {"→ "}
              {SERVICES[i].name}
            </text>
            {/* Merge slot: fills when this service's answer is home. */}
            <circle
              ref={set(`slot${i}`)}
              cx={354}
              cy={y}
              r={3.5}
              fill={SERVICES[i].color}
              opacity={0.95}
            />
          </g>
        ))}
        <text
          ref={set("waiting")}
          x={290}
          y={238}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9}
          fill={VIOLET}
          opacity={0}
        >
          2/3 · waiting on reviews
        </text>

        {/* Services with honest latencies. */}
        {SERVICES.map((s, i) => (
          <g key={s.name}>
            <rect
              x={626}
              y={SERV_Y[i] - 26}
              width={250}
              height={52}
              rx={10}
              fill={SURFACE}
              stroke={PANEL_STROKE}
            />
            <circle cx={648} cy={SERV_Y[i]} r={4} fill={s.color} />
            <text
              x={662}
              y={SERV_Y[i] + 4}
              fontFamily={MONO_FONT}
              fontSize={11}
              fill={SLATE}
            >
              {s.name}
            </text>
            <text
              x={860}
              y={SERV_Y[i] + 4}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={DIM}
            >
              {s.lat}
            </text>
            <rect
              ref={set(`busy${i}`)}
              x={626}
              y={SERV_Y[i] - 26}
              width={250}
              height={52}
              rx={10}
              fill="none"
              stroke={s.color}
              strokeOpacity={0.7}
              strokeWidth={1.25}
              opacity={0}
            />
            <circle
              ref={set(`ringS${i}`)}
              cx={626}
              cy={SERV_Y[i]}
              r={3}
              fill="none"
              stroke={s.color}
              strokeWidth={1.5}
              opacity={0}
            />
          </g>
        ))}

        {/* Pulses. */}
        <PulseGlyph
          set={set}
          id="q"
          main={TEAL}
          soft={TEAL_SOFT}
          filter="qp-soft"
        />
        <PulseGlyph
          set={set}
          id="resp"
          main={TEAL}
          soft={TEAL_SOFT}
          filter="qp-soft"
        />
        {SERVICES.map((s, i) => (
          <g key={s.name}>
            <PulseGlyph
              set={set}
              id={`c${i}`}
              main={s.color}
              soft={s.soft}
              filter="qp-soft"
            />
            <PulseGlyph
              set={set}
              id={`r${i}`}
              main={s.color}
              soft={s.soft}
              filter="qp-soft"
            />
          </g>
        ))}

        <text
          x={450}
          y={290}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.14em"
          fill={DIM}
        >
          independent services · independent speeds · one round trip for the
          client
        </text>
      </svg>
    </VisualCard>
  );
}
