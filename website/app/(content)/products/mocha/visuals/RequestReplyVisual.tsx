"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
} from "@/src/components/mocha/palette";

type Pt = readonly [number, number];

interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

// One causal loop. ORDERS SVC emits the coral request pulse onto the out
// lane; it rests a beat in the queue pill, docks at CATALOG SERVICE and dips
// straight into the GetProductHandler row (arrival ring, LED, brief wash).
// The SAME handler, still lit, emits the green reply from its lower edge:
// out through the reply dock, a beat in the return pill, back along the
// lower lane to ORDERS SVC, which gets its arrival ring and the typed
// ProductResponse.
const T = 10000;
const REQ_DEP = 500;
const REQ_PILL = 1600;
const REST = 350;
const REQ_ARR = 2950;
const REP_DEP = 5100;
const REP_PILL = 6000;
const REP_ARR = 7350;

// Below this width the two panels and the lanes get too cramped to read, so
// we lay out at MIN_W and scale the whole stage down via the SVG viewBox.
const MIN_W = 460;
const DIAG_H = 150;

const DIM = "#62748e";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const PAD_FILL = "rgba(158,176,204,0.34)";
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

interface Pin {
  readonly x: number;
  readonly y: number;
  readonly side: "left" | "right";
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
  // request: out lane, dock, 45-degree dip into the handler row's top edge
  readonly req: Polyline;
  // reply: out of the handler row's lower edge, 45-degree drop to the reply
  // dock, then the return lane back to the requester
  readonly rep: Polyline;
  readonly reqRestU: number;
  readonly repRestU: number;
  readonly pillReq: Box;
  readonly pillRep: Box;
  readonly pins: readonly Pin[];
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

interface PinRowProps {
  readonly pin: Pin;
}

// A dock at a package edge: three surface pads at 5px pitch. The connected
// middle pad carries the lane; its neighbours complete the pin row.
function PinRow({ pin }: PinRowProps) {
  const { x, y, side } = pin;
  return (
    <g>
      {[-1, 0, 1].map((i) => (
        <rect
          key={i}
          x={side === "left" ? x - 3.5 : x}
          y={y + i * 5 - 1}
          width={3.5}
          height={2}
          fill={PAD_FILL}
        />
      ))}
    </g>
  );
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
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const [w, setW] = useState(520);
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
      const cw = entries[entries.length - 1]?.contentRect.width;
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
      // The initial render is the meaningful static frame: both lanes drawn
      // with a message resting in each queue pill and the handler row lit
      // softly. Keep it.
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

      // ORDERS SVC handles the send: wash, border and LED warm up as the
      // request leaves, and again when the reply comes back in.
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

      // the correlation tag brightens by proximity as either pulse passes it
      let corr = 0;

      // coral request: out lane, a beat in the queue pill, then the dip that
      // delivers it INTO the handler row
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
          (1 - L.reqRestU) * easeInOutCubic(ramp(t, REQ_PILL + REST, REQ_ARR));
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

      // arrival INTO the handler: ring at the row's entry via, then the
      // handler row, panel border and LED stay warm through the work beat,
      // fading only after the reply has left the same row
      setRing("ringH", ((t - REQ_ARR + T) % T) / 700, 3, 11);
      const hw =
        easeOutCubic(ramp(t, REQ_ARR - 40, REQ_ARR + 220)) *
        (1 - easeInOutCubic(ramp(t, REP_DEP + 150, REP_DEP + 800)));
      setO("hFx", hw * 0.9);
      setO("pw1", hw * 0.07);
      setO("pe1", hw * 0.55);
      setO("pl1", hw * 0.9);
      setO("ph1", hw * 0.5);

      // green reply: the SAME handler emits it from its lower edge while
      // still lit; it drops to the reply dock, rests a beat in the return
      // pill and rides the lower lane home
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
          (1 - L.repRestU) * easeInOutCubic(ramp(t, REP_PILL + REST, REP_ARR));
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

      // reply arrival back at ORDERS SVC: green ring, typed response
      setRing("ringL", ((t - REP_ARR + T) % T) / 700, 3, 11);
      const resp =
        easeOutCubic(ramp(t, REP_ARR, REP_ARR + 320)) *
        (1 - ramp(t, 9100, 9600));
      setO("resp", resp * 0.9);

      // awaiting… sits under ORDERS SVC while the request is out
      const aw =
        easeOutCubic(ramp(t, 700, 1000)) *
        (1 - ramp(t, REP_ARR - 100, REP_ARR + 160));
      setO("await", aw * 0.55);
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

    // Paint the phase-0 frame so the static JSX defaults never flash.
    apply(0);

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

          {/* substrate: faint pad-dot grid behind everything */}
          <rect
            x={0}
            y={0}
            width={lw}
            height={DIAG_H}
            fill="url(#reqrep-grid)"
          />

          {/* ── copper lanes: request out (with its dip into the handler),
              reply back (from the handler's lower edge) ───────────────── */}
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

          {/* unlabeled queues as plated slots, one per lane */}
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

          {/* vias where the traces meet the handler row's edges */}
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

          {/* pin rows where the lanes dock at the two panels */}
          {L.pins.map((pin, i) => (
            <PinRow key={`pin${i}`} pin={pin} />
          ))}

          {/* correlation tag between the lanes, dim at rest */}
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

          {/* awaiting state under ORDERS SVC while the request is out */}
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

          {/* the typed response flashes next to the panel on reply arrival,
              on its own line below the corr tag so the two never collide */}
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

          {/* ── ORDERS SVC panel (requester) ───────────────────────── */}
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
            {/* faint PCB furniture in the lower-right: pad pair */}
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
            {/* activity LED: dim silk dot at rest, coral while the panel
                handles a message (send and reply-arrival beats) */}
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

          {/* ── CATALOG SERVICE panel (responder) ──────────────────── */}
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
            {/* faint PCB furniture in the lower-right: hatch patch */}
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
            {/* activity LED, lit while the handler works the request */}
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

          {/* handler row inside the panel: the request lands here and the
              reply leaves from here */}
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

          {/* dots parked in the pills: the reduced-motion frame shows them
              at rest; while animating they light during each rest beat */}
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

          {/* arrival rings: coral where the request lands in the handler,
              green where the reply returns to the requester */}
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

          {/* ── pulses in flight ───────────────────────────────────── */}
          {pulseGlyph("rq", CORAL, CORAL_SOFT)}
          {pulseGlyph("rp", GREEN, REPLY_SOFT)}
        </svg>
      </div>
    </div>
  );
}
