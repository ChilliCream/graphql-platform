"use client";

import { useLayoutEffect, useMemo, useRef, useState } from "react";

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
} from "@/src/components/mocha/palette";
import { type Pin, PinRow } from "@/src/components/mocha/PinRow";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 10000;
const REST_T = 9080;
const REQ_DEP = 500;
const REQ_PILL = 1600;
const REST = 350;
const REQ_ARR = 2950;
const REP_DEP = 5100;
const REP_PILL = 6000;
const REP_ARR = 7350;

const MIN_W = 460;
const DIAG_H = 150;

const DIM = "#62748e";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.7)";
const GRID_DOT = "rgba(150,166,194,0.10)";
const CORR_LIT = "#c7d2f0";
const REPLY_SOFT = "#c9f7e4";

const REQ_Y = 44;
const REP_Y = 100;
const CY = 72;
const REQR_X = 8;
const REQR_W = 108;
const REQR_TOP = 26;
const REQR_H = 92;
const PANEL_W = 170;
const PANEL_TOP = 14;
const PANEL_H = 118;
const ROW_TOP = 64;
const ROW_H = 30;

interface Box {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface Layout {
  readonly x1: number;
  readonly x2: number;
  readonly panelX: number;
  readonly midX: number;
  readonly rowX: number;
  readonly rowW: number;
  readonly bendInX: number;
  readonly bendOutX: number;
  readonly req: Polyline;
  readonly rep: Polyline;
  readonly reqRestU: number;
  readonly repRestU: number;
  readonly pillReq: Box;
  readonly pillRep: Box;
  readonly pins: readonly Pin[];
}

function loopFlash(t: number, at: number, fall: number): number {
  const e = (t - at + T) % T;
  return e < 200 ? e / 200 : Math.max(0, 1 - (e - 200) / fall);
}

function buildLayout(lw: number): Layout {
  const panelX = lw - 8 - PANEL_W;
  const x1 = REQR_X + REQR_W;
  const x2 = panelX;
  const span = x2 - x1;
  const rowX = panelX + 12;
  const bendInX = rowX + 26;
  const bendOutX = rowX + 8;

  const req = measure([
    [x1, REQ_Y],
    [bendInX - 8, REQ_Y],
    [bendInX, REQ_Y + 8],
    [bendInX, ROW_TOP],
  ]);
  const rep = measure([
    [bendOutX, ROW_TOP + ROW_H],
    [bendOutX - 6, REP_Y],
    [x2, REP_Y],
    [x1, REP_Y],
  ]);

  const pillReq: Box = { x: x1 + span * 0.45 - 22, y: REQ_Y - 7, w: 44, h: 14 };
  const pillRep: Box = { x: x1 + span * 0.58 - 22, y: REP_Y - 7, w: 44, h: 14 };

  return {
    x1,
    x2,
    panelX,
    midX: Math.round((x1 + x2) / 2),
    rowX,
    rowW: PANEL_W - 24,
    bendInX,
    bendOutX,
    req,
    rep,
    reqRestU: (pillReq.x + 22 - x1) / req.total,
    repRestU: (rep.total - (pillRep.x + 22 - x1)) / rep.total,
    pillReq,
    pillRep,
    pins: [
      { x: x1, y: REQ_Y, side: "right" },
      { x: x2, y: REQ_Y, side: "left" },
      { x: x1, y: REP_Y, side: "right" },
      { x: x2, y: REP_Y, side: "left" },
    ],
  };
}

export function RequestReplyVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
  const [w, setW] = useState(520);
  const lw = Math.max(w, MIN_W);
  const layout = useMemo(() => buildLayout(lw), [lw]);
  const layoutRef = useRef(layout);

  useLayoutEffect(() => {
    layoutRef.current = layout;
  }, [layout]);

  useLayoutEffect(() => {
    const node = wrapRef.current;
    if (!node) {
      return;
    }
    const cw0 = node.getBoundingClientRect().width;
    if (cw0 > 80) {
      setW(Math.round(cw0));
    }
    const ro = new ResizeObserver((entries) => {
      const cw = entries[entries.length - 1]?.contentRect.width;
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
        const d = clamp01(u) * poly.total;
        const [x, y] = pointAt(poly, u);
        setPart(p + "core", x, y);
        setPart(p + "in", x, y);
        setPart(p + "glow", x, y);
        for (let k = 1; k <= 2; k++) {
          const dk = d - 8 * k;
          const el = E.get(p + "t" + k);
          if (el) {
            if (dk <= 0) {
              el.setAttribute("opacity", "0");
            } else {
              const [tx, ty] = pointAt(poly, dk / poly.total);
              el.setAttribute("cx", tx.toFixed(2));
              el.setAttribute("cy", ty.toFixed(2));
              el.setAttribute("opacity", k === 1 ? "0.3" : "0.15");
            }
          }
        }
      };

      const apply = (t: number) => {
        const L = layoutRef.current;

        const emit =
          easeOutCubic(ramp(t, 220, 520)) *
          (1 - easeInOutCubic(ramp(t, 1000, 1800)));
        const back = loopFlash(t, REP_ARR, 1200);
        const act0 = Math.max(emit, back);
        setO("pw0", act0 * 0.07);
        setO("pe0", act0 * 0.5);
        setO("pl0", act0 * 0.9);
        setO("ph0", act0 * 0.5);
        setO("cEcho", loopFlash(t, REP_ARR, 1500) * 0.8);

        let corr = 0;

        let qu = 0;
        let qop = 0;
        if (t >= REQ_DEP && t < REQ_PILL) {
          qu = L.reqRestU * easeInOutCubic(ramp(t, REQ_DEP, REQ_PILL));
          qop = Math.min((t - REQ_DEP) / 150, 1);
        } else if (t >= REQ_PILL && t < REQ_PILL + REST) {
          qu = L.reqRestU;
          qop = 0.95;
        } else if (t >= REQ_PILL + REST && t < REQ_ARR) {
          qu =
            L.reqRestU +
            (1 - L.reqRestU) *
              easeInOutCubic(ramp(t, REQ_PILL + REST, REQ_ARR));
          qop = 1 - ramp(t, REQ_ARR - 160, REQ_ARR);
        }
        placePulse("rq", L.req, qu, qop);
        if (qop > 0.02) {
          corr = Math.max(
            corr,
            clamp01(1 - Math.abs(pointAt(L.req, qu)[0] - L.midX) / 70),
          );
        }
        setO(
          "pkq",
          0.95 *
            ramp(t, REQ_PILL, REQ_PILL + 120) *
            (1 - ramp(t, REQ_PILL + REST, REQ_PILL + REST + 180)),
        );

        setRing("ringH", ((t - REQ_ARR + T) % T) / 700, 3, 11);
        const hw =
          easeOutCubic(ramp(t, REQ_ARR - 40, REQ_ARR + 220)) *
          (1 - easeInOutCubic(ramp(t, REP_DEP + 150, REP_DEP + 800)));
        setO("hFx", hw * 0.9);
        setO("pw1", hw * 0.07);
        setO("pe1", hw * 0.55);
        setO("pl1", hw * 0.9);
        setO("ph1", hw * 0.5);

        let pu = 0;
        let pop = 0;
        if (t >= REP_DEP && t < REP_PILL) {
          pu = L.repRestU * easeInOutCubic(ramp(t, REP_DEP, REP_PILL));
          pop = Math.min((t - REP_DEP) / 150, 1);
        } else if (t >= REP_PILL && t < REP_PILL + REST) {
          pu = L.repRestU;
          pop = 0.95;
        } else if (t >= REP_PILL + REST && t < REP_ARR) {
          pu =
            L.repRestU +
            (1 - L.repRestU) *
              easeInOutCubic(ramp(t, REP_PILL + REST, REP_ARR));
          pop = 1 - ramp(t, REP_ARR - 160, REP_ARR);
        }
        placePulse("rp", L.rep, pu, pop);
        if (pop > 0.02) {
          corr = Math.max(
            corr,
            clamp01(1 - Math.abs(pointAt(L.rep, pu)[0] - L.midX) / 70),
          );
        }
        setO(
          "pkr",
          0.95 *
            ramp(t, REP_PILL, REP_PILL + 120) *
            (1 - ramp(t, REP_PILL + REST, REP_PILL + REST + 180)),
        );

        setO("corrLit", corr * 0.95);

        setRing("ringL", ((t - REP_ARR + T) % T) / 700, 3, 11);
        const resp =
          easeOutCubic(ramp(t, REP_ARR, REP_ARR + 320)) *
          (1 - ramp(t, 9100, 9600));
        setO("resp", resp * 0.9);

        const aw =
          easeOutCubic(ramp(t, 700, 1000)) *
          (1 - ramp(t, REP_ARR - 100, REP_ARR + 160));
        setO("await", aw * 0.55);
      };

      apply(0);

      return { frame: apply, rest: () => apply(REST_T) };
    },
    { period: T },
  );

  const pulseGlyph = (p: string, color: string, inner: string) => (
    <g key={p} ref={set(p)} opacity={0}>
      <circle ref={set(p + "t2")} r={1.6} fill={color} opacity={0} />
      <circle ref={set(p + "t1")} r={2} fill={color} opacity={0} />
      <circle
        ref={set(p + "glow")}
        r={6}
        fill={color}
        opacity={0.2}
        filter="url(#reqrep-soft)"
      />
      <circle ref={set(p + "core")} r={2.5} fill={color} />
      <circle ref={set(p + "in")} r={1.1} fill={inner} />
    </g>
  );

  const L = layout;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[320px]"
    >
      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${DIAG_H}`}
          width="100%"
          height={(DIAG_H * w) / lw}
          className="block"
        >
          <defs>
            <filter
              id="reqrep-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="reqrep-grid"
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
            height={DIAG_H}
            fill="url(#reqrep-grid)"
          />

          <path
            d={laneD(L.req.pts)}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
            strokeLinejoin="round"
          />
          <path
            d={laneD(L.rep.pts)}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
            strokeLinejoin="round"
          />

          {([L.pillReq, L.pillRep] as const).map((p, i) => (
            <rect
              key={`pill${i}`}
              x={p.x}
              y={p.y}
              width={p.w}
              height={p.h}
              rx={p.h / 2}
              fill={NAVY}
              stroke={VIA_STROKE}
              strokeWidth={1}
            />
          ))}

          <circle
            cx={L.bendInX}
            cy={ROW_TOP}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />
          <circle
            cx={L.bendOutX}
            cy={ROW_TOP + ROW_H}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />

          {L.pins.map((pin, i) => (
            <PinRow key={`pin${i}`} pin={pin} />
          ))}

          <text
            x={L.midX}
            y={CY + 2.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={SILK_SOFT}
            opacity={0.55}
          >
            corr · 7f3a
          </text>
          <text
            ref={set("corrLit")}
            x={L.midX}
            y={CY + 2.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={CORR_LIT}
            opacity={0}
          >
            corr · 7f3a
          </text>

          <text
            ref={set("await")}
            x={REQR_X + REQR_W / 2}
            y={REQR_TOP + REQR_H + 14}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.08em"
            fill={SILK_SOFT}
            opacity={0}
          >
            awaiting…
          </text>

          <text
            ref={set("resp")}
            x={L.x1 + 10}
            y={CY + 16}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.02em"
            fill={GREEN}
            opacity={0}
          >
            ProductResponse
          </text>

          <g>
            <rect
              x={REQR_X}
              y={REQR_TOP}
              width={REQR_W}
              height={REQR_H}
              rx={3}
              fill="rgba(139,160,188,0.03)"
              stroke={PANEL_STROKE}
              strokeWidth={1}
            />
            <circle cx={REQR_X + 6} cy={REQR_TOP + 6} r={1.2} fill={SILK} />
            <rect
              x={REQR_X + REQR_W - 30}
              y={REQR_TOP + REQR_H - 17}
              width={8}
              height={3}
              fill="rgba(154,172,200,0.12)"
            />
            <rect
              x={REQR_X + REQR_W - 30}
              y={REQR_TOP + REQR_H - 11}
              width={8}
              height={3}
              fill="rgba(154,172,200,0.12)"
            />
            <rect
              ref={set("pw0")}
              x={REQR_X}
              y={REQR_TOP}
              width={REQR_W}
              height={REQR_H}
              rx={3}
              fill={CORAL}
              opacity={0}
            />
            <rect
              ref={set("pe0")}
              x={REQR_X}
              y={REQR_TOP}
              width={REQR_W}
              height={REQR_H}
              rx={3}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.2}
              opacity={0}
            />
            <rect
              ref={set("cEcho")}
              x={REQR_X}
              y={REQR_TOP}
              width={REQR_W}
              height={REQR_H}
              rx={3}
              fill="none"
              stroke={GREEN}
              strokeWidth={1.2}
              opacity={0}
            />
            <text
              x={REQR_X + 12}
              y={REQR_TOP + 18}
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.16em"
              fill={SILK}
            >
              ORDERS SVC
            </text>
            <circle
              cx={REQR_X + REQR_W - 10}
              cy={REQR_TOP + 10}
              r={2}
              fill={SILK}
              opacity={0.25}
            />
            <circle
              ref={set("ph0")}
              cx={REQR_X + REQR_W - 10}
              cy={REQR_TOP + 10}
              r={6}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              filter="url(#reqrep-soft)"
              opacity={0}
            />
            <circle
              ref={set("pl0")}
              cx={REQR_X + REQR_W - 10}
              cy={REQR_TOP + 10}
              r={2}
              fill={CORAL}
              opacity={0}
            />
          </g>

          <g>
            <rect
              x={L.panelX}
              y={PANEL_TOP}
              width={PANEL_W}
              height={PANEL_H}
              rx={3}
              fill="rgba(139,160,188,0.03)"
              stroke={PANEL_STROKE}
              strokeWidth={1}
            />
            <circle cx={L.panelX + 6} cy={PANEL_TOP + 6} r={1.2} fill={SILK} />
            <path
              d={`M${L.panelX + PANEL_W - 30} ${PANEL_TOP + PANEL_H - 8} l7 -7 M${L.panelX + PANEL_W - 24} ${PANEL_TOP + PANEL_H - 8} l7 -7 M${L.panelX + PANEL_W - 18} ${PANEL_TOP + PANEL_H - 8} l7 -7`}
              stroke="rgba(154,172,200,0.11)"
              strokeWidth={1}
              fill="none"
            />
            <rect
              ref={set("pw1")}
              x={L.panelX}
              y={PANEL_TOP}
              width={PANEL_W}
              height={PANEL_H}
              rx={3}
              fill={CORAL}
              opacity={0}
            />
            <rect
              ref={set("pe1")}
              x={L.panelX}
              y={PANEL_TOP}
              width={PANEL_W}
              height={PANEL_H}
              rx={3}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.2}
              opacity={0}
            />
            <text
              x={L.panelX + 12}
              y={PANEL_TOP + 18}
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.16em"
              fill={SILK}
            >
              CATALOG SERVICE
            </text>
            <circle
              cx={L.panelX + PANEL_W - 10}
              cy={PANEL_TOP + 10}
              r={2}
              fill={SILK}
              opacity={0.25}
            />
            <circle
              ref={set("ph1")}
              cx={L.panelX + PANEL_W - 10}
              cy={PANEL_TOP + 10}
              r={6}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              filter="url(#reqrep-soft)"
              opacity={0}
            />
            <circle
              ref={set("pl1")}
              cx={L.panelX + PANEL_W - 10}
              cy={PANEL_TOP + 10}
              r={2}
              fill={CORAL}
              opacity={0}
            />
          </g>

          <rect
            x={L.rowX}
            y={ROW_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX}
            y={ROW_TOP + 4}
            width={3}
            height={ROW_H - 8}
            rx={1.5}
            fill={CYAN}
          />
          <text
            x={L.rowX + 13}
            y={ROW_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.04em"
            fill={DIM}
          >
            GetProductHandler
          </text>
          <g ref={set("hFx")} opacity={0.35}>
            <rect
              x={L.rowX}
              y={ROW_TOP}
              width={L.rowW}
              height={ROW_H}
              rx={5}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.2}
            />
            <text
              x={L.rowX + 13}
              y={ROW_TOP + 19}
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.04em"
              fill={CORAL_SOFT}
            >
              GetProductHandler
            </text>
          </g>

          <circle
            ref={set("pkq")}
            cx={L.pillReq.x + L.pillReq.w / 2}
            cy={L.pillReq.y + L.pillReq.h / 2}
            r={2.5}
            fill={CORAL}
            opacity={0.95}
          />
          <circle
            ref={set("pkr")}
            cx={L.pillRep.x + L.pillRep.w / 2}
            cy={L.pillRep.y + L.pillRep.h / 2}
            r={2.5}
            fill={GREEN}
            opacity={0.95}
          />

          <circle
            ref={set("ringH")}
            cx={L.bendInX}
            cy={ROW_TOP}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringL")}
            cx={L.x1}
            cy={REP_Y}
            r={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.5}
            opacity={0}
          />

          {pulseGlyph("rq", CORAL, CORAL_SOFT)}
          {pulseGlyph("rp", GREEN, REPLY_SOFT)}
        </svg>
      </div>
    </div>
  );
}
