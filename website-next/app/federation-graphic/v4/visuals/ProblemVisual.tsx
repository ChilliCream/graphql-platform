"use client";

/**
 * The organizational problem, animated in two acts with one cast. Act one:
 * three teams' schema changes converge on THE API TEAM, park in its queue,
 * and drain one at a time. Act two (crossfade): the same teams ship straight
 * to their own services on their own cadence — reviews goes on a spree,
 * inventory ships once, nobody waits. The loop alternates between the acts.
 */

import { CYAN, GREEN, MONO_FONT, SLATE, VIOLET } from "../palette";
import {
  DIM,
  HAIR,
  LANE,
  PANEL_STROKE,
  PulseGlyph,
  SURFACE,
  VisualCard,
  clamp01,
  easeInOutCubic,
  easeOutCubic,
  measure,
  ramp,
  useVisual,
} from "./anim";

const T = 15400;

const TEAMS = [
  { name: "catalog", color: CYAN, soft: "#b7e8f7", y: 80 },
  { name: "inventory", color: GREEN, soft: "#a7f3d0", y: 170 },
  { name: "reviews", color: VIOLET, soft: "#cdd7f2", y: 260 },
] as const;

// Act one geometry: chips converge on the API-team panel and its queue tray.
const A_LANES = [
  measure([
    [118, 80],
    [220, 80],
    [300, 150],
    [336, 150],
  ]),
  measure([
    [118, 170],
    [336, 170],
  ]),
  measure([
    [118, 260],
    [220, 260],
    [300, 190],
    [336, 190],
  ]),
];
const TRAY = { x: 350, y: 162, w: 116, h: 16 } as const;
const FRONT = 452;
const SLOTS = [FRONT, FRONT - 16, FRONT - 32] as const;
const DRAIN = measure([
  [FRONT, 170],
  [604, 170],
]);

// Act one schedule: fire, arrive (fire+900), drain start, deploy ring.
const A_FIRE = [400, 1500, 2600] as const;
const A_DRAIN = [3300, 5000, 6700] as const;

// Act two geometry: straight lanes to each team's own service.
const B_LANES = TEAMS.map((tm) =>
  measure([
    [118, tm.y],
    [640, tm.y],
  ]),
);
// Each team's deploys in act two: reviews on a spree, inventory once.
const B_FIRE: readonly (readonly number[])[] = [
  [8800, 11600],
  [9400],
  [10000, 10700, 11400],
];

export function ProblemVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // Act crossfade: 1 → 0.12 at 7600, back at 14700.
    const down = ramp(t, 7600, 8300);
    const up = ramp(t, 14700, 15300);
    const gA = 1 - 0.88 * clamp01(down - up);
    const gB = 0.12 + 0.88 * clamp01(down - up);
    h.setO("gA", gA);
    h.setO("gB", gB);

    // ── Act one: converge, park, drain ────────────────────────────────
    for (let i = 0; i < 3; i++) {
      const fire = A_FIRE[i];
      if (t >= fire && t < fire + 900) {
        const u = easeInOutCubic(ramp(t, fire, fire + 900));
        h.placePulse(
          `a${i}`,
          A_LANES[i],
          u,
          Math.min((t - fire) / 150, 1),
          2.4,
        );
      } else {
        h.hidePulse(`a${i}`);
      }
    }

    // Queue dots: appear on arrival, promote toward the front as the tray
    // drains, vanish when their own drain run starts.
    const promo = (dotIndex: number): number => {
      // How many earlier dots have already left the tray?
      let ahead = 0;
      for (let j = 0; j < dotIndex; j++) {
        if (t >= A_DRAIN[j] + 100) {
          ahead += 1;
        }
      }
      const target = SLOTS[dotIndex - ahead];
      return target;
    };
    for (let i = 0; i < 3; i++) {
      const arrive = A_FIRE[i] + 900;
      const gone = t >= A_DRAIN[i];
      const dot = `qd${i}`;
      if (t < arrive || gone) {
        h.setO(dot, 0);
      } else {
        const slideIn = easeOutCubic(ramp(t, arrive, arrive + 350));
        const x = TRAY.x + 6 + (promo(i) - TRAY.x - 6) * slideIn;
        h.setDot(dot, x, 170);
        h.setO(dot, 0.95 * gA);
      }
      // Drain run to the deploy node.
      if (t >= A_DRAIN[i] && t < A_DRAIN[i] + 800) {
        const u = easeInOutCubic(ramp(t, A_DRAIN[i], A_DRAIN[i] + 800));
        h.placePulse(`d${i}`, DRAIN, u, 1, 2.4);
      } else {
        h.hidePulse(`d${i}`);
      }
      h.setRing(`ringD${i}`, (t - (A_DRAIN[i] + 800)) / 600, 4, 12);
    }
    const capA = t >= 900 && t < 7400 ? 0.7 : 0;
    h.setO("capA", capA * gA);

    // ── Act two: everyone ships on their own cadence ──────────────────
    for (let i = 0; i < 3; i++) {
      B_FIRE[i].forEach((fire, k) => {
        const p = `b${i}_${k}`;
        if (t >= fire && t < fire + 800) {
          const u = easeInOutCubic(ramp(t, fire, fire + 800));
          h.placePulse(p, B_LANES[i], u, Math.min((t - fire) / 150, 1), 2.4);
        } else {
          h.hidePulse(p);
        }
      });
      // Live tick lights on each arrival, then cools.
      let lit = 0;
      B_FIRE[i].forEach((fire) => {
        const arrive = fire + 800;
        const heat = t >= arrive ? 1 - ramp(t, arrive + 900, arrive + 1500) : 0;
        lit = Math.max(lit, heat);
      });
      h.setO(`live${i}`, lit * 0.95 * gB);
      h.setRing(
        `ringB${i}`,
        (t - (B_FIRE[i][B_FIRE[i].length - 1] + 800)) / 600,
        3,
        10,
      );
    }
    const capB = t >= 9000 && t < 14400 ? 0.7 : 0;
    h.setO("capB", capB * gB);
  });

  return (
    <VisualCard rootRef={rootRef}>
      <svg viewBox="0 0 900 340" width="100%" className="block">
        <defs>
          <filter id="pb-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="2.4" />
          </filter>
        </defs>

        {/* Team chips: the constant cast. */}
        {TEAMS.map((tm) => (
          <g key={tm.name}>
            <rect
              x={8}
              y={tm.y - 15}
              width={110}
              height={30}
              rx={8}
              fill={SURFACE}
              stroke={PANEL_STROKE}
            />
            <circle cx={26} cy={tm.y} r={3.5} fill={tm.color} />
            <text
              x={38}
              y={tm.y + 4}
              fontFamily={MONO_FONT}
              fontSize={10.5}
              fill={SLATE}
            >
              {tm.name}
            </text>
          </g>
        ))}

        {/* ── Act one: the API team and its queue ── */}
        <g ref={set("gA")}>
          {A_LANES.map((l, i) => (
            <path
              key={i}
              d={l.pts
                .map(([x, y], k) => `${k === 0 ? "M" : "L"}${x} ${y}`)
                .join(" ")}
              fill="none"
              stroke={LANE}
              strokeWidth={1.5}
            />
          ))}
          <rect
            x={336}
            y={110}
            width={144}
            height={120}
            rx={12}
            fill={SURFACE}
            stroke={PANEL_STROKE}
          />
          <text
            x={352}
            y={134}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.16em"
            fill={SLATE}
          >
            THE API TEAM
          </text>
          <line x1={336} x2={480} y1={144} y2={144} stroke={HAIR} />
          <rect
            x={TRAY.x}
            y={TRAY.y}
            width={TRAY.w}
            height={TRAY.h}
            rx={4}
            fill="rgba(139,160,188,0.07)"
            stroke={PANEL_STROKE}
          />
          <text
            x={TRAY.x + TRAY.w / 2}
            y={TRAY.y + 30}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={DIM}
          >
            schema changes
          </text>
          <path
            d={`M480 170 H592`}
            fill="none"
            stroke={LANE}
            strokeWidth={1.5}
          />
          <circle
            cx={604}
            cy={170}
            r={12}
            fill="none"
            stroke={SLATE}
            strokeWidth={1.5}
            opacity={0.7}
          />
          <text
            x={604}
            y={196}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            fill={DIM}
          >
            api/v1
          </text>
          {[0, 1, 2].map((i) => (
            <g key={i}>
              <circle
                ref={set(`qd${i}`)}
                cx={SLOTS[i]}
                cy={170}
                r={2.6}
                fill={TEAMS[i].color}
                opacity={i === 0 ? 0.95 : 0.85}
              />
              <circle
                ref={set(`ringD${i}`)}
                cx={604}
                cy={170}
                r={4}
                fill="none"
                stroke={TEAMS[i].color}
                strokeWidth={1.5}
                opacity={0}
              />
              <PulseGlyph
                set={set}
                id={`a${i}`}
                main={TEAMS[i].color}
                soft={TEAMS[i].soft}
                filter="pb-soft"
              />
              <PulseGlyph
                set={set}
                id={`d${i}`}
                main={TEAMS[i].color}
                soft={TEAMS[i].soft}
                filter="pb-soft"
              />
            </g>
          ))}
          <text
            ref={set("capA")}
            x={450}
            y={324}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.14em"
            fill={DIM}
            opacity={0.7}
          >
            one schema · one queue · every team waits
          </text>
        </g>

        {/* ── Act two: own service, own cadence ── */}
        <g ref={set("gB")} opacity={0.12}>
          {TEAMS.map((tm, i) => (
            <g key={tm.name}>
              <path
                d={`M118 ${tm.y} H640`}
                fill="none"
                stroke={LANE}
                strokeWidth={1.5}
              />
              <rect
                x={640}
                y={tm.y - 26}
                width={236}
                height={52}
                rx={10}
                fill={SURFACE}
                stroke={PANEL_STROKE}
              />
              <circle cx={662} cy={tm.y} r={4} fill={tm.color} />
              <text
                x={676}
                y={tm.y + 4}
                fontFamily={MONO_FONT}
                fontSize={11}
                fill={SLATE}
              >
                {tm.name} service
              </text>
              <g ref={set(`live${i}`)} opacity={0}>
                <rect
                  x={800}
                  y={tm.y - 11}
                  width={62}
                  height={22}
                  rx={6}
                  fill="rgba(52,211,153,0.12)"
                  stroke={GREEN}
                  strokeOpacity={0.6}
                />
                <text
                  x={831}
                  y={tm.y + 4}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={9.5}
                  fill={GREEN}
                >
                  live ✓
                </text>
              </g>
              <circle
                ref={set(`ringB${i}`)}
                cx={640}
                cy={tm.y}
                r={3}
                fill="none"
                stroke={tm.color}
                strokeWidth={1.5}
                opacity={0}
              />
              {B_FIRE[i].map((_, k) => (
                <PulseGlyph
                  key={k}
                  set={set}
                  id={`b${i}_${k}`}
                  main={tm.color}
                  soft={tm.soft}
                  filter="pb-soft"
                />
              ))}
            </g>
          ))}
          <text
            ref={set("capB")}
            x={450}
            y={324}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.14em"
            fill={DIM}
            opacity={0}
          >
            no queue · every team ships on its own cadence
          </text>
        </g>
      </svg>
    </VisualCard>
  );
}
