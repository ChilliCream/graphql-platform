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
  GREEN,
  MONO_FONT,
  NAVY,
} from "@/src/components/mocha/palette";
import { type Pin, PinRow } from "@/src/components/mocha/PinRow";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 14400;
const REST_T = 10700;
const H = 360;
const MIN_W = 760;

const SURFACE = "#0c1322";
const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const SILK = "rgba(154,172,200,0.75)";

const REQ_DEP = 300;
const REQ_ARR = 1800;
const RES_DEP = 2600;
const RES_ARR = 4000;

const CHIP_X = 8;
const CHIP_W = 76;
const CHIP_Y = 146;
const CHIP_H = 64;
const REQ_Y = 152;
const RES_Y = 190;

interface FlowTime {
  readonly dep: number;
  readonly legA: number;
  readonly rest: number;
  readonly legB: number;
  readonly src: number;
  readonly dst: number;
}

const FLOWS: readonly FlowTime[] = [
  { dep: 2000, legA: 1700, rest: 700, legB: 600, src: 0, dst: 1 },
  { dep: 4600, legA: 2000, rest: 0, legB: 0, src: 1, dst: 2 },
  { dep: 6200, legA: 2200, rest: 800, legB: 600, src: 0, dst: 3 },
  { dep: 8800, legA: 1700, rest: 0, legB: 0, src: 2, dst: 0 },
  { dep: 10300, legA: 1500, rest: 0, legB: 0, src: 1, dst: 3 },
  { dep: 11200, legA: 1400, rest: 600, legB: 700, src: 3, dst: 2 },
];

const ARR = FLOWS.map((f) => f.dep + f.legA + f.rest + f.legB);

interface Box {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface PanelBox extends Box {
  readonly title: string;
}

interface FlowGeo {
  readonly poly: Polyline;
  readonly d: string;
  readonly restU: number;
  readonly end: Pt;
  readonly pill: number;
}

interface Layout {
  readonly gwR: number;
  readonly ox: number;
  readonly panels: readonly PanelBox[];
  readonly req: Polyline;
  readonly res: Polyline;
  readonly flows: readonly FlowGeo[];
  readonly pills: readonly Box[];
  readonly pipe: Box;
  readonly c1: Pt;
  readonly c1b: Pt;
  readonly c3: Pt;
  readonly pins: readonly Pin[];
}

function loopFlash(t: number, at: number, fall: number): number {
  const e = (t - at + T) % T;
  return e < 200 ? e / 200 : Math.max(0, 1 - (e - 200) / fall);
}

function uAtLastSeg(poly: Polyline, px: number): number {
  const end = poly.pts[poly.pts.length - 1];
  return (poly.total - Math.abs(end[0] - px)) / poly.total;
}

function buildLayout(lw: number): Layout {
  const m = 8;
  const gwR = CHIP_X + CHIP_W;
  const run = Math.max(48, Math.min(88, Math.round(lw * 0.07)));
  const ox = gwR + run;

  const ow = Math.max(150, Math.min(210, Math.round(lw * 0.18)));
  const oR = ox + ow;
  const bx = Math.round(lw * 0.58);
  const bw = Math.max(140, Math.min(180, Math.round(lw * 0.16)));
  const bR = bx + bw;
  const qw = Math.max(130, Math.min(170, Math.round(lw * 0.15)));
  const qx = lw - m - qw;
  const sx = Math.round(lw * 0.4);
  const sw = Math.max(150, Math.min(190, Math.round(lw * 0.17)));
  const sR = sx + sw;

  const panels: readonly PanelBox[] = [
    { x: ox, y: 112, w: ow, h: 100, title: "ORDERS" },
    { x: bx, y: 32, w: bw, h: 80, title: "BILLING" },
    { x: sx, y: 258, w: sw, h: 80, title: "SHIPPING" },
    { x: qx, y: 138, w: qw, h: 96, title: "SEARCH" },
  ];

  const bBotX = bx + Math.round(bw * 0.4);
  const sTopX = sx + Math.round(sw * 0.55);
  const oBotX = Math.min(ox + Math.round(ow * 0.45), sx - 46);
  const qTopX = qx + Math.round(qw * 0.4);
  const qBotX = qx + Math.round(qw * 0.5);

  const pills: readonly Box[] = [
    { x: bx - 60, y: 65, w: 44, h: 14 },
    { x: qx - 82, y: 169, w: 44, h: 14 },
    { x: sR + 36, y: 291, w: 44, h: 14 },
  ];

  const mkFlow = (pts: readonly Pt[], pill = -1): FlowGeo => {
    const poly = measure(pts);
    return {
      poly,
      d: laneD(pts),
      restU: pill < 0 ? 0 : uAtLastSeg(poly, pills[pill].x + pills[pill].w / 2),
      end: pts[pts.length - 1],
      pill,
    };
  };

  const flows: readonly FlowGeo[] = [
    mkFlow(
      [
        [oR, 124],
        [oR + 28, 124],
        [oR + 80, 72],
        [bx, 72],
      ],
      0,
    ),
    mkFlow([
      [bBotX, 112],
      [bBotX, 234],
      [sTopX + 24, 234],
      [sTopX, 258],
    ]),
    mkFlow(
      [
        [oR, 176],
        [qx, 176],
      ],
      1,
    ),
    mkFlow([
      [sx, 298],
      [oBotX + 40, 298],
      [oBotX, 258],
      [oBotX, 212],
    ]),
    mkFlow([
      [bR, 88],
      [qTopX - 50, 88],
      [qTopX, 138],
    ]),
    mkFlow(
      [
        [qBotX, 234],
        [qBotX, 274],
        [qBotX - 24, 298],
        [sR, 298],
      ],
      2,
    ),
  ];

  const pins: readonly Pin[] = [
    { x: gwR, y: REQ_Y, side: "right" },
    { x: gwR, y: RES_Y, side: "right" },
    { x: ox, y: REQ_Y, side: "left" },
    { x: ox, y: RES_Y, side: "left" },
    { x: oR, y: 124, side: "right" },
    { x: bx, y: 72, side: "left" },
    { x: bBotX, y: 112, side: "bottom" },
    { x: sTopX, y: 258, side: "top" },
    { x: oR, y: 176, side: "right" },
    { x: qx, y: 176, side: "left" },
    { x: sx, y: 298, side: "left" },
    { x: oBotX, y: 212, side: "bottom" },
    { x: bR, y: 88, side: "right" },
    { x: qTopX, y: 138, side: "top" },
    { x: qBotX, y: 234, side: "bottom" },
    { x: sR, y: 298, side: "right" },
  ];

  return {
    gwR,
    ox,
    panels,
    req: measure([
      [gwR, REQ_Y],
      [ox, REQ_Y],
    ]),
    res: measure([
      [ox, RES_Y],
      [gwR, RES_Y],
    ]),
    flows,
    pills,
    pipe: { x: bBotX - 5, y: 188, w: 10, h: 40 },
    c1: [oR + 56, 176],
    c1b: [oR + 86, 176],
    c3: [bBotX, 234],
    pins,
  };
}

export function WhyMessagingVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
  const [w, setW] = useState(1100);
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

      const park = [0, 0, 0];

      const apply = (t: number) => {
        const L = layoutRef.current;

        park[0] = 0;
        park[1] = 0;
        park[2] = 0;

        if (t >= REQ_DEP && t < REQ_ARR) {
          placePulse(
            "rq",
            L.req,
            easeInOutCubic(ramp(t, REQ_DEP, REQ_ARR)),
            Math.min((t - REQ_DEP) / 150, 1) *
              (1 - ramp(t, REQ_ARR - 120, REQ_ARR)),
          );
        } else {
          placePulse("rq", L.req, 0, 0);
        }
        setRing("rgq", ((t - REQ_ARR + T) % T) / 700, 3, 11);
        if (t >= RES_DEP && t < RES_ARR) {
          placePulse(
            "rs",
            L.res,
            easeInOutCubic(ramp(t, RES_DEP, RES_ARR)),
            Math.min((t - RES_DEP) / 150, 1) *
              (1 - ramp(t, RES_ARR - 120, RES_ARR)),
          );
        } else {
          placePulse("rs", L.res, 0, 0);
        }
        setRing("rgc", ((t - RES_ARR + T) % T) / 700, 3, 11);
        setO("gwEcho", loopFlash(t, RES_ARR, 1600) * 0.8);

        const oc = loopFlash(t, REQ_ARR, 1400);
        setO("ocw", oc * 0.07);
        setO("oce", oc * 0.5);
        setO("plc", oc * 0.9);
        setO("phc", oc * 0.5);

        let litA = 0;
        let litB = 0;
        let litC = 0;
        const act = [0, 0, 0, 0];
        for (let i = 0; i < FLOWS.length; i++) {
          const f = FLOWS[i];
          const g = L.flows[i];
          const tA = f.dep + f.legA;
          let u = 0;
          let op = 0;
          if (f.rest === 0) {
            if (t >= f.dep && t < tA) {
              u = easeInOutCubic(ramp(t, f.dep, tA));
              op = Math.min((t - f.dep) / 150, 1) * (1 - ramp(t, tA - 140, tA));
            }
          } else {
            const tR = tA + f.rest;
            const tB = tR + f.legB;
            if (t >= f.dep && t < tA) {
              u = g.restU * easeInOutCubic(ramp(t, f.dep, tA));
              op = Math.min((t - f.dep) / 150, 1);
            } else if (t >= tA && t < tR) {
              u = g.restU;
              op = 0.95;
            } else if (t >= tR && t < tB) {
              u = g.restU + (1 - g.restU) * easeInOutCubic(ramp(t, tR, tB));
              op = 1 - ramp(t, tR + f.legB * 0.55, tB);
            }
            if (g.pill >= 0) {
              const pv = ramp(t, tA, tA + 120) * (1 - ramp(t, tR, tR + 180));
              if (pv > park[g.pill]) {
                park[g.pill] = pv;
              }
            }
          }
          placePulse(`f${i}`, g.poly, u, op);

          if (op > 0.02) {
            const [px, py] = pointAt(g.poly, u);
            if (i === 2) {
              litA = Math.max(litA, 1 - Math.abs(px - L.c1[0]) / 46);
              litB = Math.max(litB, 1 - Math.abs(px - L.c1b[0]) / 40);
            } else if (i === 1) {
              litC = Math.max(
                litC,
                1 - Math.hypot(px - L.c3[0], py - L.c3[1]) / 46,
              );
            }
          }

          setRing(`ar${i}`, ((t - ARR[i] + T) % T) / 700, 2.5, 10);
          const dv = loopFlash(t, ARR[i], 900);
          if (dv > act[f.dst]) {
            act[f.dst] = dv;
          }
          const sv = 0.35 * loopFlash(t, f.dep, 700);
          if (sv > act[f.src]) {
            act[f.src] = sv;
          }
        }

        setO("pk0", park[0] * 0.95);
        setO("pk1", park[1] * 0.95);
        setO("pk2", park[2] * 0.95);

        for (let p = 0; p < 4; p++) {
          setO(`pw${p}`, act[p] * 0.07);
          setO(`pe${p}`, act[p] * 0.55);
          setO(`pl${p}`, act[p] * 0.9);
          setO(`ph${p}`, act[p] * 0.5);
        }
        setO("c1g", clamp01(litA) * 0.7);
        setO("c1bg", clamp01(litB) * 0.7);
        setO("c3g", clamp01(litC) * 0.7);
      };

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
        filter="url(#whym-soft)"
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
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[440px]"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${H}`}
          width="100%"
          height={(H * w) / lw}
          className="block"
        >
          <defs>
            <filter id="whym-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="whym-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect x={0} y={0} width={lw} height={H} fill="url(#whym-grid)" />

          <path
            d={`M${L.gwR} ${REQ_Y} H${L.ox}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={`M${L.gwR} ${RES_Y} H${L.ox}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />

          {L.flows.map((f, i) => (
            <path
              key={`lane${i}`}
              d={f.d}
              fill="none"
              stroke={LANE_STROKE}
              strokeWidth={1.5}
              strokeLinejoin="round"
            />
          ))}

          <path
            d={`M${L.pipe.x + L.pipe.w / 2} ${L.pipe.y} V${L.pipe.y + L.pipe.h}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={5}
          />
          <circle
            cx={L.pipe.x + L.pipe.w / 2}
            cy={L.pipe.y}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1.2}
          />
          <circle
            cx={L.pipe.x + L.pipe.w / 2}
            cy={L.pipe.y + L.pipe.h}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1.2}
          />

          {L.pills.map((p, i) => (
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
            cx={L.c1[0]}
            cy={L.c1[1]}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1.2}
          />
          <circle
            cx={L.c1b[0]}
            cy={L.c1b[1]}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1.2}
          />
          <circle
            ref={set("c1g")}
            cx={L.c1[0]}
            cy={L.c1[1]}
            r={8}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("c1bg")}
            cx={L.c1b[0]}
            cy={L.c1b[1]}
            r={5}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          <circle
            cx={L.c3[0]}
            cy={L.c3[1]}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1.2}
          />
          <circle
            ref={set("c3g")}
            cx={L.c3[0]}
            cy={L.c3[1]}
            r={7}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          {L.pins.map((pin, i) => (
            <PinRow key={`pin${i}`} pin={pin} />
          ))}

          {L.panels.map((p, i) => (
            <g key={p.title}>
              <rect
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={3}
                fill="rgba(139,160,188,0.03)"
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
              <circle cx={p.x + 6} cy={p.y + 6} r={1.2} fill={SILK} />
              {i % 2 === 0 ? (
                <g>
                  <rect
                    x={p.x + p.w - 30}
                    y={p.y + p.h - 17}
                    width={8}
                    height={3}
                    fill="rgba(154,172,200,0.12)"
                  />
                  <rect
                    x={p.x + p.w - 30}
                    y={p.y + p.h - 11}
                    width={8}
                    height={3}
                    fill="rgba(154,172,200,0.12)"
                  />
                </g>
              ) : (
                <path
                  d={`M${p.x + p.w - 30} ${p.y + p.h - 8} l7 -7 M${p.x + p.w - 24} ${p.y + p.h - 8} l7 -7 M${p.x + p.w - 18} ${p.y + p.h - 8} l7 -7`}
                  stroke="rgba(154,172,200,0.11)"
                  strokeWidth={1}
                  fill="none"
                />
              )}
              <rect
                ref={set(`pw${i}`)}
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={3}
                fill={CORAL}
                opacity={0}
              />
              <rect
                ref={set(`pe${i}`)}
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={3}
                fill="none"
                stroke={CORAL}
                strokeWidth={1.2}
                opacity={0}
              />
              <text
                x={p.x + 12}
                y={p.y + 18}
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.2em"
                fill={SILK}
              >
                {p.title}
              </text>
              <circle
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={2}
                fill={SILK}
                opacity={0.25}
              />
              <circle
                ref={set(`ph${i}`)}
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={6}
                fill="none"
                stroke={CORAL}
                strokeWidth={1.5}
                filter="url(#whym-soft)"
                opacity={0}
              />
              <circle
                ref={set(`pl${i}`)}
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={2}
                fill={CORAL}
                opacity={0}
              />
            </g>
          ))}

          <rect
            ref={set("ocw")}
            x={L.panels[0].x}
            y={L.panels[0].y}
            width={L.panels[0].w}
            height={L.panels[0].h}
            rx={3}
            fill={CYAN}
            opacity={0}
          />
          <rect
            ref={set("oce")}
            x={L.panels[0].x}
            y={L.panels[0].y}
            width={L.panels[0].w}
            height={L.panels[0].h}
            rx={3}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />
          <circle
            ref={set("phc")}
            cx={L.panels[0].x + L.panels[0].w - 10}
            cy={L.panels[0].y + 10}
            r={6}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            filter="url(#whym-soft)"
            opacity={0}
          />
          <circle
            ref={set("plc")}
            cx={L.panels[0].x + L.panels[0].w - 10}
            cy={L.panels[0].y + 10}
            r={2}
            fill={CYAN}
            opacity={0}
          />

          {L.pills.map((p, i) => (
            <circle
              key={`pk${i}`}
              ref={set(`pk${i}`)}
              cx={p.x + p.w / 2}
              cy={p.y + p.h / 2}
              r={2.5}
              fill={CORAL}
              opacity={0.95}
            />
          ))}

          {pulseGlyph("rq", CYAN, "#c9eef9")}
          {pulseGlyph("rs", GREEN, "#a7f3d0")}
          {FLOWS.map((_, i) => pulseGlyph(`f${i}`, CORAL, CORAL_SOFT))}

          <circle
            ref={set("rgq")}
            cx={L.ox}
            cy={REQ_Y}
            r={3}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("rgc")}
            cx={L.gwR}
            cy={RES_Y}
            r={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.5}
            opacity={0}
          />
          {L.flows.map((f, i) => (
            <circle
              key={`ar${i}`}
              ref={set(`ar${i}`)}
              cx={f.end[0]}
              cy={f.end[1]}
              r={2.5}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              opacity={0}
            />
          ))}

          <rect
            x={CHIP_X}
            y={CHIP_Y}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill={SURFACE}
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <circle cx={CHIP_X + 6} cy={CHIP_Y + 6} r={1.2} fill={SILK} />
          <rect
            ref={set("gwEcho")}
            x={CHIP_X}
            y={CHIP_Y}
            width={CHIP_W}
            height={CHIP_H}
            rx={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.2}
            opacity={0}
          />
          <text
            x={CHIP_X + CHIP_W / 2}
            y={CHIP_Y + CHIP_H / 2 + 3.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.14em"
            fill={SILK}
          >
            GATEWAY
          </text>
        </svg>
      </div>
    </div>
  );
}
