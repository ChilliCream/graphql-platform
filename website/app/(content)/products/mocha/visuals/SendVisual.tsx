"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  type Polyline,
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
  GREEN,
  MONO_FONT,
  NAVY,
  SLATE,
} from "@/src/components/mocha/palette";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 10000;
const REST_T = 8700;
const H = 176;
const MIN_W = 700;

const INK = "#a1a3af";
const CYAN_SOFT = "#b7e8f7";
const GREEN_SOFT = "#a7f3d0";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.7)";
const GRID_DOT = "rgba(150,166,194,0.10)";

const M = 8;
const CL_X = 8;
const CL_W = 96;
const CL_H = 26;
const LANE_Y = 91;
const PANEL_Y = 36;
const PANEL_H = 110;
const ROW_TOP = 74;
const ROW_H = 34;
const SLOT_W = 70;
const SLOT_H = 14;
const PW1 = 158;
const PW2 = 196;

interface Layout {
  readonly px1: number;
  readonly px2: number;
  readonly rowX1: number;
  readonly rowW1: number;
  readonly rowX2: number;
  readonly rowW2: number;
  readonly slotL: number;
  readonly slotR: number;
  readonly entry: number;
  readonly front: number;
  readonly req: Polyline;
  readonly resp: Polyline;
  readonly cmd: Polyline;
  readonly dlv: Polyline;
  readonly lane1D: string;
  readonly lane2D: string;
  readonly lane3D: string;
}

function buildLayout(lw: number): Layout {
  const chipR = CL_X + CL_W;
  const flexTotal = lw - (chipR + PW1 + PW2 + 2 * M);
  const run1 = Math.max(40, Math.min(110, Math.round(flexTotal * 0.25)));
  const px1 = chipR + run1;
  const x1R = px1 + PW1;
  const px2 = lw - M - PW2;
  const slotL = x1R + (px2 - x1R - SLOT_W) / 2;
  const slotR = slotL + SLOT_W;
  const rowX1 = px1 + 12;
  const rowX2 = px2 + 12;
  const entry = slotL + 7;
  const front = slotR - 9;
  return {
    px1,
    px2,
    rowX1,
    rowW1: PW1 - 24,
    rowX2,
    rowW2: PW2 - 24,
    slotL,
    slotR,
    entry,
    front,
    req: measure([
      [chipR, LANE_Y],
      [rowX1 + 14, LANE_Y],
    ]),
    resp: measure([
      [px1, LANE_Y],
      [chipR - 20, LANE_Y],
    ]),
    cmd: measure([
      [x1R, LANE_Y],
      [entry, LANE_Y],
    ]),
    dlv: measure([
      [front, LANE_Y],
      [rowX2 + 14, LANE_Y],
    ]),
    lane1D: `M${chipR} ${LANE_Y} H${px1}`,
    lane2D: laneD([
      [x1R, LANE_Y],
      [slotL, LANE_Y],
    ]),
    lane3D: laneD([
      [slotR, LANE_Y],
      [px2, LANE_Y],
    ]),
  };
}

export function SendVisual() {
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

      const setO = (k: string, v: number) => {
        const el = E.get(k);
        if (el) {
          el.setAttribute("opacity", v.toFixed(3));
        }
      };

      const setPop = (k: string, o: number, rise: number) => {
        const el = E.get(k);
        if (el) {
          el.setAttribute("opacity", o.toFixed(3));
          el.setAttribute(
            "transform",
            `translate(0 ${((1 - rise) * 5).toFixed(2)})`,
          );
        }
      };

      const setDot = (k: string, x: number, y: number, r?: number) => {
        const el = E.get(k);
        if (el) {
          el.setAttribute("cx", x.toFixed(2));
          el.setAttribute("cy", y.toFixed(2));
          if (r !== undefined) {
            el.setAttribute("r", Math.max(0, r).toFixed(2));
          }
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

      const placePulse = (
        p: string,
        poly: Polyline,
        u: number,
        op: number,
        coreR: number,
      ) => {
        const g = E.get(p);
        if (!g) {
          return;
        }
        if (op <= 0.01 || coreR <= 0.05) {
          g.setAttribute("opacity", "0");
          return;
        }
        g.setAttribute("opacity", op.toFixed(3));
        const d = clamp01(u) * poly.total;
        const [x, y] = pointAt(poly, u);
        setDot(p + "core", x, y, coreR);
        setDot(p + "in", x, y, coreR * 0.45);
        setDot(p + "glow", x, y, Math.max(0.6, coreR * 2.4));
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

      const hidePulse = (p: string) => setO(p, 0);

      const apply = (t: number) => {
        const L = layoutRef.current;
        const master = 1 - ramp(t, 9450, 9750);

        if (t >= 300 && t < 1800) {
          const u = easeInOutCubic(ramp(t, 300, 1800));
          const r = 2.5 * (1 - ramp(t, 1680, 1800));
          placePulse("req", L.req, u, Math.min((t - 300) / 150, 1), r);
        } else {
          hidePulse("req");
        }
        setRing("ring1", (t - 1800) / 700, 3, 12);
        const w1 = t < 1950 ? ramp(t, 1800, 1950) : 1 - ramp(t, 2700, 3450);
        const a1 = Math.max(0, w1);
        setO("h1echo", a1 * 0.7);
        setO("h1lit", a1 * 0.9);
        setO("pw1", a1 * 0.07);
        setO("pe1", a1 * 0.55);
        setO("pl1", a1 * 0.9);
        setO("ph1", a1 * 0.5);

        if (t >= 2700 && t < 4100) {
          const u = easeInOutCubic(ramp(t, 2700, 4100));
          placePulse("resp", L.resp, u, Math.min((t - 2700) / 150, 1), 2.5);
        } else {
          hidePulse("resp");
        }
        setRing("ringC", (t - 4100) / 650, 3, 10);
        const cl = t < 4250 ? ramp(t, 4100, 4250) : 1 - ramp(t, 4250, 5150);
        setO("clLit", Math.max(0, cl) * 0.9);
        setO("clGlow", Math.max(0, cl) * 0.25);
        const tp = easeOutCubic(ramp(t, 4100, 4600));
        setPop("tag200", tp * 0.92 * master, tp);

        if (t >= 2700 && t < 4400) {
          const u = easeInOutCubic(ramp(t, 2700, 4400));
          placePulse("cmd", L.cmd, u, Math.min((t - 2700) / 150, 1), 2.5);
        } else {
          hidePulse("cmd");
        }
        setRing("ringQ", (t - 4400) / 650, 2, 8);

        const dot = E.get("qdot");
        if (dot) {
          if (t >= 4400 && t < 6900) {
            const u = easeOutCubic(ramp(t, 4400, 4900));
            setDot("qdot", L.entry + (L.front - L.entry) * u, LANE_Y);
            dot.setAttribute("opacity", "0.95");
          } else {
            dot.setAttribute("opacity", "0");
          }
        }

        if (t >= 6900 && t < 8400) {
          const u = easeInOutCubic(ramp(t, 6900, 8400));
          const r = 2.5 * (1 - ramp(t, 8280, 8400));
          placePulse("dlv", L.dlv, u, Math.min((t - 6900) / 150, 1), r);
        } else {
          hidePulse("dlv");
        }
        setRing("ring2", (t - 8400) / 700, 3, 12);
        const w2 = t < 8550 ? ramp(t, 8400, 8550) : 1 - ramp(t, 8850, 9600);
        const a2 = Math.max(0, w2);
        setO("h2echo", a2 * 0.7);
        setO("h2lit", a2 * 0.9);
        setO("pw2", a2 * 0.07);
        setO("pe2", a2 * 0.55);
        setO("pl2", a2 * 0.9);
        setO("ph2", a2 * 0.5);

        const de = t - 8550;
        let dd = 0;
        if (de >= 0) {
          dd = de < 1050 ? (Math.floor(de / 350) % 2 === 0 ? 0.9 : 0.3) : 0.6;
        }
        setO("later", dd * master);
      };

      return { frame: apply, rest: () => apply(REST_T) };
    },
    { period: T },
  );

  const pulseGlyph = (p: string, main: string, soft: string) => (
    <g key={p} ref={set(p)} opacity={0}>
      <circle ref={set(p + "t3")} r={1.4} fill={main} opacity={0} />
      <circle ref={set(p + "t2")} r={1.7} fill={main} opacity={0} />
      <circle ref={set(p + "t1")} r={2} fill={main} opacity={0} />
      <circle
        ref={set(p + "glow")}
        r={6}
        fill={main}
        opacity={0.22}
        filter="url(#send-soft)"
      />
      <circle ref={set(p + "core")} r={2.5} fill={main} />
      <circle ref={set(p + "in")} r={1.1} fill={soft} />
    </g>
  );

  const L = layout;
  const chipR = CL_X + CL_W;
  const chipTop = LANE_Y - CL_H / 2;

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
            <filter id="send-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="send-pcb-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect width={lw} height={H} fill="url(#send-pcb-grid)" />

          <path
            d={L.lane1D}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={L.lane2D}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
            strokeLinejoin="round"
          />
          <path
            d={L.lane3D}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
            strokeLinejoin="round"
          />

          <circle
            cx={L.slotR}
            cy={LANE_Y}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />

          <rect
            x={L.slotL}
            y={LANE_Y - SLOT_H / 2}
            width={SLOT_W}
            height={SLOT_H}
            rx={SLOT_H / 2}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />
          <text
            x={(L.slotL + L.slotR) / 2}
            y={LANE_Y + SLOT_H / 2 + 13}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={SILK_SOFT}
          >
            reserve-inventory
          </text>
          <circle
            ref={set("qdot")}
            cx={L.front}
            cy={LANE_Y}
            r={2.5}
            fill={CORAL}
            opacity={0.95}
          />

          <rect
            x={L.px1}
            y={PANEL_Y}
            width={PW1}
            height={PANEL_H}
            rx={3}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <circle cx={L.px1 + 5.5} cy={PANEL_Y + 5.5} r={1.2} fill={SILK} />
          {[-5, 0, 5].map((dy) => (
            <g key={dy}>
              <rect
                x={L.px1 - 1}
                y={LANE_Y + dy - 1.75}
                width={2}
                height={3.5}
                fill={PAD_FILL}
              />
              <rect
                x={L.px1 + PW1 - 1}
                y={LANE_Y + dy - 1.75}
                width={2}
                height={3.5}
                fill={PAD_FILL}
              />
            </g>
          ))}
          <rect
            ref={set("pw1")}
            x={L.px1}
            y={PANEL_Y}
            width={PW1}
            height={PANEL_H}
            rx={3}
            fill={CYAN}
            opacity={0}
          />
          <rect
            ref={set("pe1")}
            x={L.px1}
            y={PANEL_Y}
            width={PW1}
            height={PANEL_H}
            rx={3}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />
          <text
            x={L.px1 + 12}
            y={PANEL_Y + 18}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={SILK}
          >
            ORDERS SERVICE
          </text>
          <circle
            cx={L.px1 + PW1 - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={SILK}
            opacity={0.25}
          />
          <circle
            ref={set("ph1")}
            cx={L.px1 + PW1 - 10}
            cy={PANEL_Y + 10}
            r={6}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            filter="url(#send-soft)"
            opacity={0}
          />
          <circle
            ref={set("pl1")}
            cx={L.px1 + PW1 - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={CYAN}
            opacity={0}
          />
          <rect
            x={L.rowX1}
            y={ROW_TOP}
            width={L.rowW1}
            height={ROW_H}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX1}
            y={ROW_TOP}
            width={L.rowW1}
            height={ROW_H}
            rx={5}
            fill={CYAN}
            opacity={0.05}
          />
          <rect
            x={L.rowX1}
            y={ROW_TOP + 5}
            width={3}
            height={ROW_H - 10}
            rx={1.5}
            fill={CYAN}
          />
          <text
            x={L.rowX1 + 13}
            y={ROW_TOP + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={INK}
          >
            PlaceOrderHandler
          </text>
          <text
            ref={set("h1lit")}
            x={L.rowX1 + 13}
            y={ROW_TOP + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={CYAN_SOFT}
            opacity={0}
          >
            PlaceOrderHandler
          </text>
          <rect
            ref={set("h1echo")}
            x={L.rowX1}
            y={ROW_TOP}
            width={L.rowW1}
            height={ROW_H}
            rx={5}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />

          <rect
            x={L.px2}
            y={PANEL_Y}
            width={PW2}
            height={PANEL_H}
            rx={3}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <circle cx={L.px2 + 5.5} cy={PANEL_Y + 5.5} r={1.2} fill={SILK} />
          {[-5, 0, 5].map((dy) => (
            <rect
              key={dy}
              x={L.px2 - 1}
              y={LANE_Y + dy - 1.75}
              width={2}
              height={3.5}
              fill={PAD_FILL}
            />
          ))}
          <rect
            ref={set("pw2")}
            x={L.px2}
            y={PANEL_Y}
            width={PW2}
            height={PANEL_H}
            rx={3}
            fill={CORAL}
            opacity={0}
          />
          <rect
            ref={set("pe2")}
            x={L.px2}
            y={PANEL_Y}
            width={PW2}
            height={PANEL_H}
            rx={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />
          <text
            x={L.px2 + 12}
            y={PANEL_Y + 18}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={SILK}
          >
            INVENTORY SERVICE
          </text>
          <circle
            cx={L.px2 + PW2 - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={SILK}
            opacity={0.25}
          />
          <circle
            ref={set("ph2")}
            cx={L.px2 + PW2 - 10}
            cy={PANEL_Y + 10}
            r={6}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            filter="url(#send-soft)"
            opacity={0}
          />
          <circle
            ref={set("pl2")}
            cx={L.px2 + PW2 - 10}
            cy={PANEL_Y + 10}
            r={2}
            fill={CORAL}
            opacity={0}
          />
          <rect
            x={L.rowX2}
            y={ROW_TOP}
            width={L.rowW2}
            height={ROW_H}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX2}
            y={ROW_TOP}
            width={L.rowW2}
            height={ROW_H}
            rx={5}
            fill={CYAN}
            opacity={0.05}
          />
          <rect
            x={L.rowX2}
            y={ROW_TOP + 5}
            width={3}
            height={ROW_H - 10}
            rx={1.5}
            fill={CYAN}
          />
          <text
            x={L.rowX2 + 13}
            y={ROW_TOP + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={INK}
          >
            ReserveInventoryHandler
          </text>
          <text
            ref={set("h2lit")}
            x={L.rowX2 + 13}
            y={ROW_TOP + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={CORAL_SOFT}
            opacity={0}
          >
            ReserveInventoryHandler
          </text>
          <rect
            ref={set("h2echo")}
            x={L.rowX2}
            y={ROW_TOP}
            width={L.rowW2}
            height={ROW_H}
            rx={5}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />
          <text
            ref={set("later")}
            x={L.px2 + PW2 / 2}
            y={PANEL_Y + PANEL_H + 16}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.14em"
            fill={SILK_SOFT}
            opacity={0.5}
          >
            handled later
          </text>

          {pulseGlyph("req", CYAN, CYAN_SOFT)}
          {pulseGlyph("resp", GREEN, GREEN_SOFT)}
          {pulseGlyph("cmd", CORAL, CORAL_SOFT)}
          {pulseGlyph("dlv", CORAL, CORAL_SOFT)}

          <circle
            ref={set("ring1")}
            cx={L.px1}
            cy={LANE_Y}
            r={3}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringC")}
            cx={chipR}
            cy={LANE_Y}
            r={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringQ")}
            cx={L.slotL}
            cy={LANE_Y}
            r={2}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ring2")}
            cx={L.px2}
            cy={LANE_Y}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          <text
            x={CL_X}
            y={chipTop - 10}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.22em"
            fill={SILK}
          >
            CLIENT
          </text>
          <rect
            ref={set("clGlow")}
            x={CL_X}
            y={chipTop}
            width={CL_W}
            height={CL_H}
            rx={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={5}
            filter="url(#send-soft)"
            opacity={0}
          />
          <rect
            x={CL_X}
            y={chipTop}
            width={CL_W}
            height={CL_H}
            rx={3}
            fill={SURFACE}
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <circle cx={CL_X + 4.5} cy={chipTop + 4.5} r={1.2} fill={SILK} />
          {[-5, 0, 5].map((dy) => (
            <rect
              key={dy}
              x={chipR - 1}
              y={LANE_Y + dy - 1.75}
              width={2}
              height={3.5}
              fill={PAD_FILL}
            />
          ))}
          <rect
            ref={set("clLit")}
            x={CL_X}
            y={chipTop}
            width={CL_W}
            height={CL_H}
            rx={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1}
            opacity={0}
          />
          <text
            x={CL_X + CL_W / 2}
            y={LANE_Y + 3}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.04em"
            fill={SLATE}
            fillOpacity={0.9}
          >
            POST /orders
          </text>
          <g ref={set("tag200")} opacity={0.92}>
            <text
              x={CL_X}
              y={chipTop + CL_H + 18}
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.04em"
              fill={GREEN}
            >
              200 · already returned
            </text>
          </g>
        </svg>
      </div>
    </div>
  );
}
