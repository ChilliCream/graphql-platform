"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
  SLATE,
} from "../palette";

type Pt = readonly [number, number];

interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

// Master loop. One request/response beat per loop up front, then the fabric
// keeps trading coral messages between service pairs until the wrap.
const T = 14400;
const H = 360;
// Below this width the four panels and the fabric get too cramped to read,
// so we lay out at MIN_W and scale the whole stage down via the SVG viewBox.
const MIN_W = 760;

const DIM = "#62748e";
const SURFACE = "#0c1322";
const PANEL_STROKE = "rgba(139,160,188,0.25)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const FURN_STROKE = "rgba(139,160,188,0.45)";
const VIA_STROKE = "rgba(139,160,188,0.5)";

// ── the one visible request/response beat ──────────────────────────
const REQ_DEP = 300;
const REQ_ARR = 1800;
const RES_DEP = 2600;
const RES_ARR = 4000;

// ── client / orders anatomy ────────────────────────────────────────
const CHIP_X = 8;
const CHIP_W = 66;
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

// Panel indices: 0 ORDERS · 1 BILLING · 2 SHIPPING · 3 SEARCH. Staggered so
// something is always in flight somewhere on the fabric.
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
}

interface Layout {
  readonly clientR: number;
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
  readonly vias: readonly Pt[];
  readonly noteX: number;
  readonly tag2X: number;
}

function measure(pts: readonly Pt[]): Polyline {
  const lens: number[] = [];
  let total = 0;
  for (let i = 0; i < pts.length - 1; i++) {
    const len = Math.hypot(
      pts[i + 1][0] - pts[i][0],
      pts[i + 1][1] - pts[i][1],
    );
    lens.push(len);
    total += len;
  }
  return { pts, lens, total };
}

function pointAt(p: Polyline, u: number): Pt {
  const target = clamp01(u) * p.total;
  let acc = 0;
  for (let i = 0; i < p.lens.length; i++) {
    if (target <= acc + p.lens[i] || i === p.lens.length - 1) {
      const t = p.lens[i] === 0 ? 0 : (target - acc) / p.lens[i];
      const [ax, ay] = p.pts[i];
      const [bx, by] = p.pts[i + 1];
      return [ax + (bx - ax) * t, ay + (by - ay) * t];
    }
    acc += p.lens[i];
  }
  return p.pts[p.pts.length - 1];
}

function laneD(pts: readonly Pt[]): string {
  return pts.map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`).join(" ");
}

function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

function ramp(t: number, a: number, b: number): number {
  return clamp01((t - a) / (b - a));
}

function easeOutCubic(u: number): number {
  return 1 - Math.pow(1 - u, 3);
}

function easeInOutCubic(u: number): number {
  return u < 0.5 ? 4 * u * u * u : 1 - Math.pow(-2 * u + 2, 3) / 2;
}

// Flash that survives the loop wrap: elapsed time is taken modulo T, so a
// glow that peaks near the end of the loop decays into the next one.
function loopFlash(t: number, at: number, fall: number): number {
  const e = (t - at + T) % T;
  return e < 200 ? e / 200 : Math.max(0, 1 - (e - 200) / fall);
}

// Fraction of the polyline at horizontal position px on its final segment,
// used to park a pulse in a queue pill that sits on the arrival run.
function uAtLastSeg(poly: Polyline, px: number): number {
  const end = poly.pts[poly.pts.length - 1];
  return (poly.total - Math.abs(end[0] - px)) / poly.total;
}

function buildLayout(lw: number): Layout {
  const m = 8;
  const clientR = CHIP_X + CHIP_W;
  const run = Math.max(64, Math.min(110, Math.round(lw * 0.085)));

  // Four panels of slightly varied size, arranged organically: orders
  // center-left, billing top-right, shipping bottom-center, search far right.
  const ox = clientR + run;
  const ow = Math.max(150, Math.min(200, Math.round(lw * 0.175)));
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

  // Fabric attach points on the panel edges.
  const bBotX = bx + Math.round(bw * 0.4);
  const sTopX = sx + Math.round(sw * 0.55);
  const oBotX = ox + Math.round(ow * 0.6);
  const qTopX = qx + Math.round(qw * 0.4);
  const qBotX = qx + Math.round(qw * 0.5);

  const mkFlow = (pts: readonly Pt[], restX?: number): FlowGeo => {
    const poly = measure(pts);
    return {
      poly,
      d: laneD(pts),
      restU: restX === undefined ? 0 : uAtLastSeg(poly, restX),
      end: pts[pts.length - 1],
    };
  };

  // orders→billing, billing→shipping, orders→search, shipping→orders,
  // billing→search, search→shipping. 45-degree bends throughout; three of
  // the routes rest briefly in an anonymous queue pill before delivery.
  const flows: readonly FlowGeo[] = [
    mkFlow(
      [
        [oR, 124],
        [oR + 28, 124],
        [oR + 80, 72],
        [bx, 72],
      ],
      bx - 38,
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
      qx - 60,
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
      sR + 58,
    ),
  ];

  const pills: readonly Box[] = [
    { x: bx - 60, y: 65, w: 44, h: 14 },
    { x: qx - 82, y: 169, w: 44, h: 14 },
    { x: sR + 36, y: 291, w: 44, h: 14 },
  ];

  const vias: readonly Pt[] = [
    [clientR, REQ_Y],
    [clientR, RES_Y],
    [ox, REQ_Y],
    [ox, RES_Y],
    [oR, 124],
    [bx, 72],
    [bBotX, 112],
    [sTopX, 258],
    [oR, 176],
    [qx, 176],
    [sx, 298],
    [oBotX, 212],
    [bR, 88],
    [qTopX, 138],
    [qBotX, 234],
    [sR, 298],
  ];

  return {
    clientR,
    ox,
    panels,
    req: measure([
      [clientR, REQ_Y],
      [ox, REQ_Y],
    ]),
    res: measure([
      [ox, RES_Y],
      [clientR, RES_Y],
    ]),
    flows,
    pills,
    pipe: { x: bBotX - 5, y: 188, w: 10, h: 40 },
    c1: [oR + 56, 176],
    c1b: [oR + 86, 176],
    c3: [bBotX, 234],
    vias,
    noteX: Math.round((clientR + ox) / 2),
    tag2X: Math.round((oR + bx) / 2),
  };
}

export function WhyMessagingVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
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

  useEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      // The initial render is the meaningful static frame: the whole mesh
      // drawn, tags visible, a few dots resting in the queue pills. Keep it.
      return;
    }

    const E = els;
    let raf = 0;
    let running = false;
    let inView = false;

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

      // stage tags brighten with their beat, then settle back
      setO("z1", 0.6 + 0.4 * ramp(t, 300, 800) * (1 - ramp(t, 4400, 5400)));
      setO("z2", 0.6 + 0.4 * ramp(t, 2000, 2600) * (1 - ramp(t, 13000, 14000)));

      // the static parked dots belong to the reduced-motion frame only;
      // while animating, resting pulses play that role
      setO("pk0", 0);
      setO("pk1", 0);
      setO("pk2", 0);

      // beat 01: one cyan request to one service, green answer right back
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
      setO("clEcho", loopFlash(t, RES_ARR, 1600) * 0.8);
      setO("note", 0.65 * ramp(t, 4100, 4800) * (1 - ramp(t, 13400, 14200)));

      // ORDERS handles the request: cyan busy shimmer
      const oc = loopFlash(t, REQ_ARR, 1400);
      setO("ocw", oc * 0.07);
      setO("oce", oc * 0.5);

      // beat 02: the fabric trades coral messages on staggered schedules
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
            // resting in the queue pill
            u = g.restU;
            op = 0.95;
          } else if (t >= tR && t < tB) {
            u = g.restU + (1 - g.restU) * easeInOutCubic(ramp(t, tR, tB));
            op = 1 - ramp(t, tR + f.legB * 0.55, tB);
          }
        }
        placePulse(`f${i}`, g.poly, u, op);

        // ring clusters light up as a pulse passes through them
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

      // faint activity shimmer on busy panels (interior wash + border)
      for (let p = 0; p < 4; p++) {
        setO(`pw${p}`, act[p] * 0.07);
        setO(`pe${p}`, act[p] * 0.55);
      }
      setO("c1g", clamp01(litA) * 0.7);
      setO("c1bg", clamp01(litB) * 0.7);
      setO("c3g", clamp01(litC) * 0.7);
    };

    let t = 0;
    let last = 0;

    const step = (now: number) => {
      const dt = Math.min(now - last, 50);
      last = now;
      t = (t + dt) % T;
      apply(t);
      raf = requestAnimationFrame(step);
    };
    const sync = () => {
      const should = inView && !document.hidden;
      if (should && !running) {
        running = true;
        last = performance.now();
        raf = requestAnimationFrame(step);
      } else if (!should && running) {
        running = false;
        cancelAnimationFrame(raf);
      }
    };
    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1].isIntersecting;
        sync();
      },
      { threshold: 0.2 },
    );
    io.observe(root);
    document.addEventListener("visibilitychange", sync);
    return () => {
      io.disconnect();
      document.removeEventListener("visibilitychange", sync);
      cancelAnimationFrame(raf);
    };
  }, [els]);

  const set = (k: string) => (node: SVGElement | null) => {
    els.set(k, node);
  };

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
          </defs>

          {/* ── the two stage tags ─────────────────────────────────── */}
          <text
            ref={set("z1")}
            x={8}
            y={124}
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.1em"
            opacity={0.85}
          >
            <tspan fill={CORAL}>01</tspan>
            <tspan fill={DIM}> · ONE REQUEST</tspan>
          </text>
          <text
            ref={set("z2")}
            x={L.tag2X}
            y={22}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.1em"
            opacity={0.85}
          >
            <tspan fill={CORAL}>02</tspan>
            <tspan fill={DIM}> · SERVICES TALK OVER MESSAGING</tspan>
          </text>

          {/* ── request / response lanes ───────────────────────────── */}
          <path
            d={`M${L.clientR} ${REQ_Y} H${L.ox}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={`M${L.clientR} ${RES_Y} H${L.ox}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />

          {/* ── the messaging fabric: anonymous copper topology ────── */}
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

          {/* short pipe segment on the billing→shipping drop */}
          <rect
            x={L.pipe.x}
            y={L.pipe.y}
            width={L.pipe.w}
            height={L.pipe.h}
            rx={L.pipe.w / 2}
            fill="rgba(139,160,188,0.06)"
            stroke={FURN_STROKE}
            strokeWidth={1}
          />

          {/* unlabeled queue pills */}
          {L.pills.map((p, i) => (
            <rect
              key={`pill${i}`}
              x={p.x}
              y={p.y}
              width={p.w}
              height={p.h}
              rx={p.h / 2}
              fill="rgba(139,160,188,0.05)"
              stroke="rgba(139,160,188,0.3)"
              strokeWidth={1}
            />
          ))}

          {/* chained ring cluster on the orders→search run */}
          <circle
            cx={L.c1[0]}
            cy={L.c1[1]}
            r={8}
            fill="none"
            stroke={FURN_STROKE}
            strokeWidth={1}
          />
          <circle
            cx={L.c1[0]}
            cy={L.c1[1]}
            r={3}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />
          <circle
            cx={L.c1b[0]}
            cy={L.c1b[1]}
            r={5}
            fill="none"
            stroke={FURN_STROKE}
            strokeWidth={1}
          />
          <circle
            cx={L.c1b[0]}
            cy={L.c1b[1]}
            r={1.8}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
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

          {/* nested ring cluster at the fabric junction */}
          <circle
            cx={L.c3[0]}
            cy={L.c3[1]}
            r={7}
            fill="none"
            stroke={FURN_STROKE}
            strokeWidth={1}
          />
          <circle
            cx={L.c3[0]}
            cy={L.c3[1]}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
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

          {/* via rings at lane endpoints */}
          {L.vias.map(([x, y], i) => (
            <circle
              key={`via${i}`}
              cx={x}
              cy={y}
              r={2}
              fill={NAVY}
              stroke={VIA_STROKE}
              strokeWidth={1.2}
            />
          ))}

          {/* annotation under the response lane */}
          <text
            ref={set("note")}
            x={L.noteX}
            y={226}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.08em"
            fill={DIM}
            opacity={0.65}
          >
            answered in milliseconds
          </text>

          {/* ── the four service panels, interiors clean ───────────── */}
          {L.panels.map((p, i) => (
            <g key={p.title}>
              <rect
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={10}
                fill="rgba(139,160,188,0.03)"
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
              <rect
                ref={set(`pw${i}`)}
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={10}
                fill={CORAL}
                opacity={0}
              />
              <rect
                ref={set(`pe${i}`)}
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={10}
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
                fill={DIM}
              >
                {p.title}
              </text>
            </g>
          ))}

          {/* ORDERS busy shimmer while it handles the one request */}
          <rect
            ref={set("ocw")}
            x={L.panels[0].x}
            y={L.panels[0].y}
            width={L.panels[0].w}
            height={L.panels[0].h}
            rx={10}
            fill={CYAN}
            opacity={0}
          />
          <rect
            ref={set("oce")}
            x={L.panels[0].x}
            y={L.panels[0].y}
            width={L.panels[0].w}
            height={L.panels[0].h}
            rx={10}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />

          {/* dots resting in the pills (the reduced-motion frame) */}
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

          {/* ── pulses in flight ───────────────────────────────────── */}
          {pulseGlyph("rq", CYAN, "#c9eef9")}
          {pulseGlyph("rs", GREEN, "#a7f3d0")}
          {FLOWS.map((_, i) => pulseGlyph(`f${i}`, CORAL, CORAL_SOFT))}

          {/* arrival rings */}
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
            cx={L.clientR}
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

          {/* ── client chip ────────────────────────────────────────── */}
          <rect
            x={CHIP_X}
            y={CHIP_Y}
            width={CHIP_W}
            height={CHIP_H}
            rx={6}
            fill={SURFACE}
            stroke={SLATE + "59"}
            strokeWidth={1}
          />
          <rect
            ref={set("clEcho")}
            x={CHIP_X}
            y={CHIP_Y}
            width={CHIP_W}
            height={CHIP_H}
            rx={6}
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
            fill={SLATE}
            opacity={0.85}
          >
            CLIENT
          </text>
        </svg>
      </div>
    </div>
  );
}
