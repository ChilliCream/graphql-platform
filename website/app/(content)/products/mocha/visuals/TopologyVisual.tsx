"use client";

import { useLayoutEffect, useMemo, useRef, useState } from "react";

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
import { CORAL, CORAL_SOFT, GREEN, MONO_FONT, NAVY, SLATE } from "@/src/components/mocha/palette";
import { type Pin, PinRow } from "@/src/components/mocha/PinRow";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 12000;
const REST_T = 10300;
const H = 300;
const MIN_W = 560;

const INK = "#a1a3af";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.7)";

const OX = 8;
const OW = 118;
const OY = 116;
const OH = 76;
const O_RIGHT = OX + OW;
const CW = 170;
const B_Y = 46;
const B_H = 78;
const I_Y = 180;
const I_H = 78;
const Y_A = 88;
const Y_B = 218;
const SRC_A = 138;
const SRC_B = 172;
const PILL_W = 128;
const PILL_H = 16;

const CONSUMERS = [
  {
    title: "BILLING",
    handler: "OrderPlacedHandler",
    queue: "billing.order-placed",
    exchange: "order-placed",
    laneY: Y_A,
  },
  {
    title: "INVENTORY",
    handler: "ReserveInventoryHandler",
    queue: "inventory.reserve",
    exchange: "reserve-inventory",
    laneY: Y_B,
  },
] as const;

const DECL = [
  {
    rowIn: 300,
    rowHold: 1500,
    rowOut: 2300,
    laneA: 700,
    laneB: 1700,
    pill: 1050,
    via: 1250,
  },
  {
    rowIn: 2100,
    rowHold: 3300,
    rowOut: 4100,
    laneA: 2500,
    laneB: 3500,
    pill: 2850,
    via: 3050,
  },
] as const;

const FLOWS = [
  { dep: 5000, legA: 1100, rest: 600, legB: 700, dst: 1 },
  { dep: 7700, legA: 1100, rest: 600, legB: 700, dst: 2 },
] as const;

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

interface RouteGeo {
  readonly poly: Polyline;
  readonly drawD: string;
  readonly restU: number;
  readonly end: Pt;
}

interface Layout {
  readonly bx: number;
  readonly viaX: number;
  readonly panels: readonly PanelBox[];
  readonly routes: readonly RouteGeo[];
  readonly pills: readonly Box[];
  readonly pins: readonly Pin[];
  readonly tagX: number;
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
  const bx = lw - 8 - CW;
  const pillX = bx - 28 - PILL_W;
  const bendA = O_RIGHT + 72;
  const bendB = O_RIGHT + 68;
  const viaX = Math.round((bendA + pillX) / 2);

  const mkRoute = (pts: readonly Pt[]): RouteGeo => {
    const poly = measure(pts);
    return {
      poly,
      drawD: laneD([...pts].reverse()),
      restU: uAtLastSeg(poly, pillX + PILL_W / 2),
      end: pts[pts.length - 1],
    };
  };

  return {
    bx,
    viaX,
    panels: [
      { x: OX, y: OY, w: OW, h: OH, title: "ORDERS" },
      { x: bx, y: B_Y, w: CW, h: B_H, title: "BILLING" },
      { x: bx, y: I_Y, w: CW, h: I_H, title: "INVENTORY" },
    ],
    routes: [
      mkRoute([
        [O_RIGHT, SRC_A],
        [O_RIGHT + 22, SRC_A],
        [bendA, Y_A],
        [bx, Y_A],
      ]),
      mkRoute([
        [O_RIGHT, SRC_B],
        [O_RIGHT + 22, SRC_B],
        [bendB, Y_B],
        [bx, Y_B],
      ]),
    ],
    pills: [
      { x: pillX, y: Y_A - PILL_H / 2, w: PILL_W, h: PILL_H },
      { x: pillX, y: Y_B - PILL_H / 2, w: PILL_W, h: PILL_H },
    ],
    pins: [
      { x: O_RIGHT, y: SRC_A, side: "right" },
      { x: O_RIGHT, y: SRC_B, side: "right" },
      { x: bx, y: Y_A, side: "left" },
      { x: bx, y: Y_B, side: "left" },
    ],
    tagX: Math.round((O_RIGHT + bx) / 2),
  };
}

export function TopologyVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
  const [w, setW] = useState(620);
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
          el.setAttribute("transform", `translate(0 ${((1 - rise) * 5).toFixed(2)})`);
        }
      };

      const drawLane = (k: string, len: number, p: number, master: number) => {
        const el = E.get(k);
        if (el) {
          el.setAttribute("stroke-dashoffset", (len * (1 - p)).toFixed(2));
          el.setAttribute("opacity", p <= 0 ? "0" : master.toFixed(3));
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

      const park = [0, 0];

      const apply = (t: number) => {
        const L = layoutRef.current;
        const master = 1 - ramp(t, 11200, 11900);

        park[0] = 0;
        park[1] = 0;
        const act = [0, 0, 0];
        for (let i = 0; i < FLOWS.length; i++) {
          const f = FLOWS[i];
          const g = L.routes[i];
          const tA = f.dep + f.legA;
          const tR = tA + f.rest;
          const tB = tR + f.legB;
          let u = 0;
          let op = 0;
          if (t >= f.dep && t < tA) {
            u = g.restU * easeInOutCubic(ramp(t, f.dep, tA));
            op = Math.min((t - f.dep) / 150, 1);
          } else if (t >= tA && t < tR) {
            u = g.restU;
            op = 0.95;
          } else if (t >= tR && t < tB) {
            u = g.restU + (1 - g.restU) * easeInOutCubic(ramp(t, tR, tB));
            op = 1 - ramp(t, tB - 160, tB);
          }
          park[i] = ramp(t, tA, tA + 120) * (1 - ramp(t, tR, tR + 180));
          placePulse(`f${i}`, g.poly, u, op);

          if (op > 0.02) {
            const [px] = pointAt(g.poly, u);
            setO(`vg${i}`, clamp01(1 - Math.abs(px - L.viaX) / 44) * 0.7);
          } else {
            setO(`vg${i}`, 0);
          }

          setRing(`ar${i}`, ((t - ARR[i] + T) % T) / 700, 3, 11);
          const dv = loopFlash(t, ARR[i], 1100);
          if (dv > act[f.dst]) {
            act[f.dst] = dv;
          }
          const sv = 0.35 * loopFlash(t, f.dep, 700);
          if (sv > act[0]) {
            act[0] = sv;
          }
        }

        setO("pk0", park[0] * 0.95 * master);
        setO("pk1", park[1] * 0.95 * master);

        for (let i = 0; i < DECL.length; i++) {
          const d = DECL[i];
          const rv = ramp(t, d.rowIn, d.rowIn + 200) * (1 - ramp(t, d.rowHold, d.rowOut));
          setO(`r${i}lit`, Math.max(rv * 0.9, act[i + 1] * 0.9));
          setO(`r${i}echo`, Math.max(rv * 0.7, act[i + 1] * 0.9));
          drawLane(`ln${i}`, L.routes[i].poly.total, easeInOutCubic(ramp(t, d.laneA, d.laneB)), master);
          const pv = easeOutCubic(ramp(t, d.pill, d.pill + 500));
          setPop(`pill${i}`, pv * master, pv);
          const vv = easeOutCubic(ramp(t, d.via, d.via + 450));
          setPop(`via${i}`, vv * master, vv);
        }

        setO("note", 0.85 * ramp(t, 4000, 4600) * master);

        for (let p = 0; p < 3; p++) {
          setO(`pw${p}`, act[p] * 0.07);
          setO(`pe${p}`, act[p] * 0.55);
          setO(`pl${p}`, act[p] * 0.9);
          setO(`ph${p}`, act[p] * 0.5);
        }
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
      <circle ref={set(p + "glow")} r={6} fill={color} opacity={0.2} filter="url(#topo-soft)" />
      <circle ref={set(p + "core")} r={2.5} fill={color} />
      <circle ref={set(p + "in")} r={1.1} fill={inner} />
    </g>
  );

  const L = layout;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[380px]"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg viewBox={`0 0 ${lw} ${H}`} width="100%" height={(H * w) / lw} className="block">
          <defs>
            <filter id="topo-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern id="topo-grid" width={28} height={28} patternUnits="userSpaceOnUse">
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect x={0} y={0} width={lw} height={H} fill="url(#topo-grid)" />

          {L.routes.map((r, i) => (
            <path
              key={`ln${i}`}
              ref={set(`ln${i}`)}
              d={r.drawD}
              fill="none"
              stroke={LANE_STROKE}
              strokeWidth={1.5}
              strokeLinejoin="round"
              strokeDasharray={r.poly.total}
              strokeDashoffset={0}
            />
          ))}

          {CONSUMERS.map((c, i) => (
            <g key={`via${i}`} ref={set(`via${i}`)} opacity={1}>
              <circle cx={L.viaX} cy={c.laneY} r={3.5} fill={NAVY} stroke={VIA_STROKE} strokeWidth={1.2} />
              <text
                x={L.viaX}
                y={i === 0 ? c.laneY - 12 : c.laneY + 20}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={8.5}
                fill={SILK_SOFT}
                opacity={0.75}
              >
                {c.exchange}
              </text>
            </g>
          ))}
          {CONSUMERS.map((c, i) => (
            <circle
              key={`vg${i}`}
              ref={set(`vg${i}`)}
              cx={L.viaX}
              cy={c.laneY}
              r={7}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              opacity={0}
            />
          ))}

          {L.pills.map((p, i) => (
            <g key={`pill${i}`} ref={set(`pill${i}`)} opacity={1}>
              <rect
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={p.h / 2}
                fill={NAVY}
                stroke={VIA_STROKE}
                strokeWidth={1}
              />
              <text
                x={p.x + p.w / 2}
                y={p.y + p.h / 2 + 3}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={9}
                fill={SLATE}
                fillOpacity={0.85}
              >
                {CONSUMERS[i].queue}
              </text>
            </g>
          ))}

          <text
            ref={set("note")}
            x={L.tagX}
            y={157}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.04em"
            fill={GREEN}
            opacity={0.85}
          >
            ✓ topology validated at startup
          </text>

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
              {i === 0 ? (
                <g>
                  <rect x={p.x + p.w - 30} y={p.y + p.h - 17} width={8} height={3} fill="rgba(154,172,200,0.12)" />
                  <rect x={p.x + p.w - 30} y={p.y + p.h - 11} width={8} height={3} fill="rgba(154,172,200,0.12)" />
                </g>
              ) : (
                <g>
                  <rect
                    x={p.x + 12}
                    y={p.y + 34}
                    width={p.w - 24}
                    height={26}
                    rx={5}
                    fill={SURFACE}
                    stroke={HAIR}
                    strokeWidth={1}
                  />
                  <rect x={p.x + 12} y={p.y + 39} width={3} height={16} rx={1.5} fill={CORAL} />
                  <text x={p.x + 25} y={p.y + 51} fontFamily={MONO_FONT} fontSize={9} fill={INK}>
                    {CONSUMERS[i - 1].handler}
                  </text>
                  <text
                    ref={set(`r${i - 1}lit`)}
                    x={p.x + 25}
                    y={p.y + 51}
                    fontFamily={MONO_FONT}
                    fontSize={9}
                    fill={CORAL_SOFT}
                    opacity={0}
                  >
                    {CONSUMERS[i - 1].handler}
                  </text>
                  <rect
                    ref={set(`r${i - 1}echo`)}
                    x={p.x + 12}
                    y={p.y + 34}
                    width={p.w - 24}
                    height={26}
                    rx={5}
                    fill="none"
                    stroke={CORAL}
                    strokeWidth={1.2}
                    opacity={0}
                  />
                </g>
              )}
              <rect ref={set(`pw${i}`)} x={p.x} y={p.y} width={p.w} height={p.h} rx={3} fill={CORAL} opacity={0} />
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
              <text x={p.x + 12} y={p.y + 18} fontFamily={MONO_FONT} fontSize={10} letterSpacing="0.2em" fill={SILK}>
                {p.title}
              </text>
              <circle cx={p.x + p.w - 10} cy={p.y + 10} r={2} fill={SILK} opacity={0.25} />
              <circle
                ref={set(`ph${i}`)}
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={6}
                fill="none"
                stroke={CORAL}
                strokeWidth={1.5}
                filter="url(#topo-soft)"
                opacity={0}
              />
              <circle ref={set(`pl${i}`)} cx={p.x + p.w - 10} cy={p.y + 10} r={2} fill={CORAL} opacity={0} />
            </g>
          ))}

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

          {L.routes.map((r, i) => (
            <circle
              key={`ar${i}`}
              ref={set(`ar${i}`)}
              cx={r.end[0]}
              cy={r.end[1]}
              r={3}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              opacity={0}
            />
          ))}

          {FLOWS.map((_, i) => pulseGlyph(`f${i}`, CORAL, CORAL_SOFT))}
        </svg>
      </div>
    </div>
  );
}
