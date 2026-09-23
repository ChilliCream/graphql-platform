"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  type Polyline,
  type Pt,
  clamp01,
  easeInOutCubic,
  easeOutCubic,
  laneD,
  measure,
  pointAt,
  ramp,
} from "@/src/components/mocha/geometry";
import {
  CORAL,
  CORAL_SOFT,
  CYAN,
  MONO_FONT,
  VIOLET,
} from "@/src/components/mocha/palette";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 2800;
const REST_T = 2400;
const H = 210;
const MIN_W = 540;

const PULSE_MS = 1100;
const DOT_GAP = 10;
const V_STREAM = 0.1;
const MAX_DOTS = 28;
const COUNT_BASE = 2481302;
const TICK_MS = 115;

const INK = "#a1a3af";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_DIM = "rgba(154,172,200,0.7)";
const GRID_DOT = "rgba(150,166,194,0.10)";

const CHIP_X = 8;
const CHIP_W = 96;
const CHIP_H = 26;
const CHIP_R = CHIP_X + CHIP_W;
const C1_Y = 51;
const C2_Y = 158;
const PANEL_Y = 10;
const PANEL_H = 190;
const ROW_H = 30;
const R1_TOP = 44;
const R2_TOP = 123;
const R1_Y = R1_TOP + ROW_H / 2;
const R2_Y = R2_TOP + ROW_H / 2;

const MAX_CHARS = 33;

interface Layout {
  readonly px: number;
  readonly pw: number;
  readonly rowX: number;
  readonly rowW: number;
  readonly rowFont: number;
  readonly p1: Polyline;
  readonly p2: Polyline;
  readonly d1: string;
  readonly d2: string;
}

function loopFlash(t: number, at: number, fall: number): number {
  const e = (t - at + T) % T;
  return e < 200 ? e / 200 : Math.max(0, 1 - (e - 200) / fall);
}

function fmtCount(n: number): string {
  const s = String(n);
  let out = "";
  for (let i = 0; i < s.length; i++) {
    if (i > 0 && (s.length - i) % 3 === 0) {
      out += " ";
    }
    out += s[i];
  }
  return out;
}

function buildLayout(lw: number): Layout {
  const run = Math.max(130, Math.min(240, Math.round(lw * 0.3)));
  const px = CHIP_R + run;
  const pw = lw - px - 8;
  const rowX = px + 12;
  const rowW = pw - 24;
  const bx1 = CHIP_R + Math.round(run * 0.6);
  const bx2 = CHIP_R + Math.round(run * 0.4);
  const pts1: readonly Pt[] = [
    [CHIP_R, C1_Y],
    [bx1, C1_Y],
    [bx1 + 8, R1_Y],
    [rowX, R1_Y],
  ];
  const pts2: readonly Pt[] = [
    [CHIP_R, C2_Y],
    [bx2, C2_Y],
    [bx2 + 20, R2_Y],
    [rowX, R2_Y],
  ];
  return {
    px,
    pw,
    rowX,
    rowW,
    rowFont: Math.min(10.5, (rowW - 26) / (MAX_CHARS * 0.635)),
    p1: measure(pts1),
    p2: measure(pts2),
    d1: laneD(pts1),
    d2: laneD(pts2),
  };
}

export function TransportsVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
  const [w, setW] = useState(620);
  const lw = Math.max(w, MIN_W);
  const layout = useMemo(() => buildLayout(lw), [lw]);
  const layoutRef = useRef(layout);

  useEffect(() => {
    layoutRef.current = layout;
  }, [layout]);

  useEffect(() => {
    const node = wrapRef.current;
    if (!node) {
      return;
    }
    const ro = new ResizeObserver((entries) => {
      const cw = entries[0]?.contentRect.width;
      if (cw && cw > 80) {
        setW(Math.round(cw));
      }
    });
    ro.observe(node);
    return () => ro.disconnect();
  }, []);

  useRafLoop(
    rootRef,
    () => {
      const E = els;
      let countCache = -1;

      const setO = (k: string, v: number) => {
        const el = E.get(k);
        if (el) {
          el.setAttribute("opacity", v.toFixed(3));
        }
      };

      const setPart = (k: string, x: number, y: number) => {
        const el = E.get(k);
        if (el) {
          el.setAttribute("cx", x.toFixed(2));
          el.setAttribute("cy", y.toFixed(2));
        }
      };

      const setRing = (k: string, s: number, r0: number, dr: number) => {
        const el = E.get(k);
        if (!el) {
          return;
        }
        if (s < 0 || s >= 1) {
          el.setAttribute("opacity", "0");
          return;
        }
        el.setAttribute("r", (r0 + dr * easeOutCubic(s)).toFixed(2));
        el.setAttribute("opacity", (0.5 * (1 - s)).toFixed(3));
      };

      const placePulse = (p: string, poly: Polyline, u: number, op: number) => {
        const g = E.get(p);
        if (!g) {
          return;
        }
        if (op <= 0.01) {
          g.setAttribute("opacity", "0");
          return;
        }
        g.setAttribute("opacity", op.toFixed(3));
        const d = u * poly.total;
        const [x, y] = pointAt(poly, u);
        setPart(p + "core", x, y);
        setPart(p + "in", x, y);
        setPart(p + "glow", x, y);
        for (let k = 1; k <= 3; k++) {
          const dk = d - 7 * k;
          const el = E.get(p + "t" + k);
          if (el) {
            if (dk <= 0) {
              el.setAttribute("opacity", "0");
            } else {
              const [tx, ty] = pointAt(poly, dk / poly.total);
              el.setAttribute("cx", tx.toFixed(2));
              el.setAttribute("cy", ty.toFixed(2));
              el.setAttribute("opacity", (0.45 - 0.12 * k).toFixed(2));
            }
          }
        }
      };

      const apply = (t: number, life: number) => {
        const L = layoutRef.current;

        const off = (t * V_STREAM) % DOT_GAP;
        for (let i = 0; i < MAX_DOTS; i++) {
          const el = E.get("s" + i);
          if (!el) {
            continue;
          }
          const d = off + i * DOT_GAP;
          if (d >= L.p2.total) {
            el.setAttribute("opacity", "0");
            continue;
          }
          const op = 0.9 * clamp01(Math.min(d / 8, (L.p2.total - d) / 8, 1));
          const [x, y] = pointAt(L.p2, d / L.p2.total);
          el.setAttribute("cx", x.toFixed(2));
          el.setAttribute("cy", y.toFixed(2));
          el.setAttribute("opacity", op.toFixed(3));
        }

        if (t < PULSE_MS) {
          const u = easeInOutCubic(t / PULSE_MS);
          const op =
            Math.min(t / 150, 1) * (1 - ramp(t, PULSE_MS - 160, PULSE_MS));
          placePulse("p1", L.p1, u, op);
        } else {
          placePulse("p1", L.p1, 1, 0);
        }

        setRing("ring", ((t - PULSE_MS + T) % T) / 700, 3, 11);
        const e = loopFlash(t, PULSE_MS, 1000);
        setO("r1echo", e * 0.7);
        setO("r1lit", e * 0.9);
        setO("plC", e * 0.9);
        setO("phC", e * 0.5);

        const cl = Math.max(1 - ramp(t, 0, 390), ramp(t, T - 210, T));
        setO("c1lit", cl * 0.9);
        setO("c1glow", cl * 0.28);

        const sh = 0.5 + 0.5 * Math.sin(((t / T) * 2 - 0.5) * Math.PI * 3);
        setO("shim", 0.05 + 0.05 * sh);
        setO("c2glow", 0.12 + 0.08 * sh);

        const a2 = 0.6 + 0.3 * sh;
        setO("plS", a2 * 0.9);
        setO("phS", a2 * 0.5);

        const n = COUNT_BASE + Math.floor(life / TICK_MS);
        if (n !== countCache) {
          countCache = n;
          const el = E.get("count");
          if (el) {
            el.textContent = fmtCount(n);
          }
        }
      };

      return {
        frame: (t, dt, life) => apply(t, life),
        rest: () => apply(REST_T, 0),
      };
    },
    { period: T },
  );

  const L = layout;
  const midU = 0.55;
  const midD = midU * L.p1.total;
  const [mx, my] = pointAt(L.p1, midU);
  const trail = [1, 2, 3].map((k) =>
    pointAt(L.p1, Math.max(0, midD - 7 * k) / L.p1.total),
  );

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[360px]"
    >
      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${H}`}
          width="100%"
          height={(H * w) / lw}
          className="block"
        >
          <defs>
            <filter
              id="transports-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="transports-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect
            x={0}
            y={0}
            width={lw}
            height={H}
            fill="url(#transports-grid)"
          />

          <rect
            x={L.px}
            y={PANEL_Y}
            width={L.pw}
            height={PANEL_H}
            rx={3}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <circle cx={L.px + 7} cy={PANEL_Y + 7} r={1.2} fill={SILK} />
          <text
            x={L.px + 12}
            y={PANEL_Y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill={SILK}
          >
            ORDERS SERVICE
          </text>

          <circle
            cx={L.px + L.pw - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={SILK}
            opacity={0.25}
          />
          <circle
            ref={set("phC")}
            cx={L.px + L.pw - 10}
            cy={PANEL_Y + 10}
            r={6}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            filter="url(#transports-soft)"
            opacity={0}
          />
          <circle
            ref={set("phS")}
            cx={L.px + L.pw - 10}
            cy={PANEL_Y + 10}
            r={6}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            filter="url(#transports-soft)"
            opacity={0}
          />
          <circle
            ref={set("plC")}
            cx={L.px + L.pw - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={CORAL}
            opacity={0}
          />
          <circle
            ref={set("plS")}
            cx={L.px + L.pw - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={CYAN}
            opacity={0}
          />

          <rect
            x={L.rowX}
            y={R1_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={6}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX}
            y={R1_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={6}
            fill={VIOLET}
            opacity={0.06}
          />
          <rect
            x={L.rowX}
            y={R1_TOP + 5}
            width={3}
            height={ROW_H - 10}
            rx={1.5}
            fill={VIOLET}
          />
          <text
            x={L.rowX + 13}
            y={R1_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={L.rowFont}
            fill={INK}
          >
            OrderPlacedHandler
          </text>
          <text
            ref={set("r1lit")}
            x={L.rowX + 13}
            y={R1_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={L.rowFont}
            fill={CORAL_SOFT}
            opacity={0}
          >
            OrderPlacedHandler
          </text>
          <rect
            ref={set("r1echo")}
            x={L.rowX}
            y={R1_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={6}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />

          <rect
            x={L.rowX}
            y={R2_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={6}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            ref={set("shim")}
            x={L.rowX}
            y={R2_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={6}
            fill={CYAN}
            opacity={0.07}
          />
          <rect
            x={L.rowX}
            y={R2_TOP + 5}
            width={3}
            height={ROW_H - 10}
            rx={1.5}
            fill={CYAN}
          />
          <text
            x={L.rowX + 13}
            y={R2_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={L.rowFont}
            fill={INK}
          >
            DeviceTelemetryHandler
          </text>
          <text
            ref={set("count")}
            x={L.rowX + L.rowW - 9}
            y={R2_TOP + 19}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={L.rowFont}
            fill={CYAN}
            style={{ fontVariantNumeric: "tabular-nums" }}
          >
            {fmtCount(COUNT_BASE)}
          </text>
          <text
            x={L.rowX + L.rowW - 9}
            y={R2_TOP + ROW_H + 14}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.12em"
            fill={SILK_DIM}
          >
            events today
          </text>

          <path
            d={L.d1}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
            strokeLinejoin="round"
          />
          <path
            d={L.d2}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
            strokeLinejoin="round"
          />

          {[-5, 0, 5].map((dy) => (
            <g key={dy}>
              <rect
                x={CHIP_R}
                y={C1_Y + dy - 1}
                width={3.5}
                height={2}
                fill={PAD_FILL}
              />
              <rect
                x={CHIP_R}
                y={C2_Y + dy - 1}
                width={3.5}
                height={2}
                fill={PAD_FILL}
              />
              <rect
                x={L.px - 3.5}
                y={R1_Y + dy - 1}
                width={3.5}
                height={2}
                fill={PAD_FILL}
              />
              <rect
                x={L.px - 3.5}
                y={R2_Y + dy - 1}
                width={3.5}
                height={2}
                fill={PAD_FILL}
              />
            </g>
          ))}

          <circle
            cx={L.rowX}
            cy={R1_Y}
            r={2.5}
            fill={SURFACE}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />
          <circle
            cx={L.rowX}
            cy={R2_Y}
            r={2.5}
            fill={SURFACE}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />

          {Array.from({ length: MAX_DOTS }, (_, i) => {
            const d = i * DOT_GAP;
            const on = d < L.p2.total;
            const [x, y] = on ? pointAt(L.p2, d / L.p2.total) : [0, 0];
            const op = on
              ? 0.9 * clamp01(Math.min(d / 8, (L.p2.total - d) / 8, 1))
              : 0;
            return (
              <circle
                key={i}
                ref={set(`s${i}`)}
                cx={x}
                cy={y}
                r={1.4}
                fill={CYAN}
                opacity={op}
              />
            );
          })}

          <g ref={set("p1")} opacity={1}>
            <circle
              ref={set("p1t3")}
              cx={trail[2][0]}
              cy={trail[2][1]}
              r={1.4}
              fill={CORAL}
              opacity={0.09}
            />
            <circle
              ref={set("p1t2")}
              cx={trail[1][0]}
              cy={trail[1][1]}
              r={1.7}
              fill={CORAL}
              opacity={0.21}
            />
            <circle
              ref={set("p1t1")}
              cx={trail[0][0]}
              cy={trail[0][1]}
              r={2}
              fill={CORAL}
              opacity={0.33}
            />
            <circle
              ref={set("p1glow")}
              cx={mx}
              cy={my}
              r={6}
              fill={CORAL}
              opacity={0.22}
              filter="url(#transports-soft)"
            />
            <circle ref={set("p1core")} cx={mx} cy={my} r={2.5} fill={CORAL} />
            <circle
              ref={set("p1in")}
              cx={mx}
              cy={my}
              r={1.1}
              fill={CORAL_SOFT}
            />
          </g>

          <circle
            ref={set("ring")}
            cx={L.rowX}
            cy={R1_Y}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          <rect
            ref={set("c1glow")}
            x={CHIP_X}
            y={C1_Y - CHIP_H / 2}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill="none"
            stroke={VIOLET}
            strokeWidth={5}
            filter="url(#transports-soft)"
            opacity={0}
          />
          <rect
            x={CHIP_X}
            y={C1_Y - CHIP_H / 2}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill={SURFACE}
            stroke={VIOLET + "59"}
            strokeWidth={1}
          />
          <rect
            ref={set("c1lit")}
            x={CHIP_X}
            y={C1_Y - CHIP_H / 2}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill="none"
            stroke={VIOLET}
            strokeWidth={1}
            opacity={0}
          />
          <text
            x={CHIP_X + CHIP_W / 2}
            y={C1_Y + 3.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.12em"
            fill={VIOLET}
            opacity={0.85}
          >
            RABBITMQ
          </text>
          <text
            x={CHIP_X + CHIP_W / 2}
            y={C1_Y + CHIP_H / 2 + 13}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.05em"
            fill={SILK_DIM}
          >
            orders · commands
          </text>

          <rect
            ref={set("c2glow")}
            x={CHIP_X}
            y={C2_Y - CHIP_H / 2}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill="none"
            stroke={CYAN}
            strokeWidth={5}
            filter="url(#transports-soft)"
            opacity={0.15}
          />
          <rect
            x={CHIP_X}
            y={C2_Y - CHIP_H / 2}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill={SURFACE}
            stroke={CYAN + "59"}
            strokeWidth={1}
          />
          <text
            x={CHIP_X + CHIP_W / 2}
            y={C2_Y + 3.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.12em"
            fill={CYAN}
            opacity={0.85}
          >
            EVENT HUB
          </text>
          <text
            x={CHIP_X + CHIP_W / 2}
            y={C2_Y + CHIP_H / 2 + 13}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.05em"
            fill={SILK_DIM}
          >
            device telemetry
          </text>
        </svg>
      </div>
    </div>
  );
}
