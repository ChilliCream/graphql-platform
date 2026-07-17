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
  VIOLET,
} from "../palette";

type Pt = readonly [number, number];

interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

// Master loop: three declared handlers materialize their broker topology one
// by one, then a green validation beat and one coral pulse prove the derived
// plumbing is live.
const T = 13200;
const H = 264;
// Below this width the code panel and the derived queue pills get too cramped
// to read, so we lay out at MIN_W and scale the stage down via the viewBox.
const MIN_W = 560;

const INK = "#a1a3af";
const DIM = "#62748e";
const CYAN_SOFT = "#b7e8f7";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(139,160,188,0.25)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const FRAME_STROKE = "rgba(139,160,188,0.3)";

// YOUR CODE panel: three declared rows, fixed width.
const PX = 8;
const PW = 202;
const P_RIGHT = PX + PW;
const PY = 50;
const PH = 144;
const ROW_X = PX + 12;
const ROW_W = PW - 24;
const ROW_H = 28;
const ROW_TOPS = [78, 116, 154] as const;

// BROKER region: one derived row per declaration.
const FRAME_X = 228;
const FRAME_Y = 24;
const FRAME_H = 222;
const BRX = 240;
const Y_A = 78;
const Y_B = 140;
const Y_C = 206;
const PILL_H = 18;
const REPLY_W = 58;
const REPLY_Y = 166;
const REPLY_H = 16;

const ROWS = [
  { label: "OrderPlacedHandler", bar: CYAN, soft: CYAN_SOFT, tint: 0.05 },
  { label: "GetProductHandler", bar: CYAN, soft: CYAN_SOFT, tint: 0.05 },
  // A message the service SENDS, not a handler: coral accent.
  {
    label: "ReserveInventoryCommand",
    bar: CORAL,
    soft: CORAL_SOFT,
    tint: 0.06,
  },
] as const;

// Row lit windows: [fade-in start, fade-in end, fade-out start, fade-out end].
const LIT = [
  [300, 450, 1600, 2300],
  [2600, 2750, 3900, 4600],
  [5000, 5150, 6300, 7000],
] as const;

interface Layout {
  readonly frameW: number;
  readonly ringX: number;
  readonly pillX: number;
  readonly pillW: number;
  readonly pillR: number;
  readonly replyX: number;
  readonly dpEnd: number;
  readonly lnA1: Polyline;
  readonly lnA2: Polyline;
  readonly lnB1: Polyline;
  readonly lnB2: Polyline;
  readonly lnC1: Polyline;
  readonly lnC2: Polyline;
  readonly stub: Polyline;
  readonly proof: Polyline;
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

function laneD(p: Polyline): string {
  return p.pts.map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`).join(" ");
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

function buildLayout(lw: number): Layout {
  const brw = lw - PX - BRX;
  const ringX = BRX + 44 + brw * 0.04;
  const pillW = Math.max(120, Math.min(156, brw * 0.38));
  const pillX = BRX + brw - pillW - brw * 0.18;
  const pillR = pillX + pillW;
  const stubX = pillR - 29;
  return {
    frameW: lw - PX - FRAME_X,
    ringX,
    pillX,
    pillW,
    pillR,
    replyX: pillR - REPLY_W,
    dpEnd: pillX + 28,
    // Binding lanes run from the YOUR CODE panel edge into the broker with
    // 45-degree bends onto each derived row.
    lnA1: measure([
      [P_RIGHT, 92],
      [218, 92],
      [232, Y_A],
      [ringX - 10, Y_A],
    ]),
    lnA2: measure([
      [ringX + 10, Y_A],
      [pillX, Y_A],
    ]),
    lnB1: measure([
      [P_RIGHT, 130],
      [218, 130],
      [228, Y_B],
      [ringX - 10, Y_B],
    ]),
    lnB2: measure([
      [ringX + 10, Y_B],
      [pillX, Y_B],
    ]),
    lnC1: measure([
      [P_RIGHT, 168],
      [218, 168],
      [256, Y_C],
      [ringX - 10, Y_C],
    ]),
    lnC2: measure([
      [ringX + 10, Y_C],
      [pillX + 28, Y_C],
    ]),
    stub: measure([
      [stubX, Y_B + PILL_H / 2],
      [stubX, REPLY_Y],
    ]),
    // The proof pulse rides the order-placed row end-to-end: panel edge,
    // through the exchange ring, into the derived queue.
    proof: measure([
      [P_RIGHT, 92],
      [218, 92],
      [232, Y_A],
      [pillR - 12, Y_A],
    ]),
  };
}

export function TopologyVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
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

  useEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      // The initial render is the meaningful final frame: the full topology
      // drawn and the validated tag visible. Keep it static.
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

    const drawLane = (k: string, len: number, p: number, master: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("stroke-dashoffset", (len * (1 - p)).toFixed(2));
        el.setAttribute("opacity", p <= 0 ? "0" : master.toFixed(3));
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

    const apply = (t: number) => {
      const L = layoutRef.current;
      const master = 1 - ramp(t, 12100, 12800);

      // Each code row highlights in turn while its topology materializes.
      for (let i = 0; i < 3; i++) {
        const [a, b, c, d] = LIT[i];
        const v = ramp(t, a, b) * (1 - ramp(t, c, d));
        setO(`r${i}lit`, v * 0.9);
        setO(`r${i}echo`, v * 0.7);
      }

      // 1 · OrderPlacedHandler derives exchange + queue.
      drawLane(
        "lnA1",
        L.lnA1.total,
        easeInOutCubic(ramp(t, 600, 1350)),
        master,
      );
      const exA = easeOutCubic(ramp(t, 1150, 1600));
      setPop("exA", exA * master, exA);
      drawLane(
        "lnA2",
        L.lnA2.total,
        easeInOutCubic(ramp(t, 1450, 1950)),
        master,
      );
      const qA = easeOutCubic(ramp(t, 1850, 2350));
      setPop("qA", qA * master, qA);

      // 2 · GetProductHandler derives exchange + queue + reply slot.
      drawLane(
        "lnB1",
        L.lnB1.total,
        easeInOutCubic(ramp(t, 2900, 3650)),
        master,
      );
      const exB = easeOutCubic(ramp(t, 3450, 3900));
      setPop("exB", exB * master, exB);
      drawLane(
        "lnB2",
        L.lnB2.total,
        easeInOutCubic(ramp(t, 3750, 4250)),
        master,
      );
      const qB = easeOutCubic(ramp(t, 4150, 4650));
      setPop("qB", qB * master, qB);
      drawLane(
        "stub",
        L.stub.total,
        easeInOutCubic(ramp(t, 4550, 4850)),
        master,
      );
      const rq = easeOutCubic(ramp(t, 4750, 5200));
      setPop("rq", rq * master, rq);

      // 3 · the sent command derives its dispatch binding.
      drawLane(
        "lnC1",
        L.lnC1.total,
        easeInOutCubic(ramp(t, 5300, 6200)),
        master,
      );
      const exC = easeOutCubic(ramp(t, 6000, 6450));
      setPop("exC", exC * master, exC);
      drawLane(
        "lnC2",
        L.lnC2.total,
        easeInOutCubic(ramp(t, 6300, 6800)),
        master,
      );
      setO("viaC", easeOutCubic(ramp(t, 6650, 7000)) * master);

      // 4 · validated at startup: green frame flash, tag holds.
      const bf =
        easeOutCubic(ramp(t, 7300, 7550)) *
        (1 - easeInOutCubic(ramp(t, 7550, 8400)));
      setO("bflash", bf * 0.5);
      const vp = easeOutCubic(ramp(t, 7350, 7900));
      setPop("vtag", vp * 0.95 * master, vp);

      // 5 · one coral pulse proves the plumbing end-to-end.
      if (t >= 8100 && t < 9900) {
        const u = easeInOutCubic(ramp(t, 8100, 9900));
        const r = 2.5 * (1 - ramp(t, 9800, 9900));
        placePulse("pf", L.proof, u, Math.min((t - 8100) / 150, 1), r);
      } else {
        setO("pf", 0);
      }
      const pe = ramp(t, 8600, 8750) * (1 - ramp(t, 8750, 9350));
      setO("exAe", pe * 0.8);
      const qe = ramp(t, 9750, 9900) * (1 - ramp(t, 9900, 10700));
      setO("qAe", qe * 0.75);
      const aring = E.get("aring");
      if (aring) {
        const s = (t - 9900) / 800;
        if (s < 0 || s >= 1) {
          aring.setAttribute("opacity", "0");
        } else {
          aring.setAttribute("r", (3 + 10 * easeOutCubic(s)).toFixed(2));
          aring.setAttribute("opacity", (0.5 * (1 - s)).toFixed(3));
        }
      }
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

  const L = layout;

  const lane = (k: string, p: Polyline) => (
    <path
      key={k}
      ref={set(k)}
      d={laneD(p)}
      fill="none"
      stroke={LANE_STROKE}
      strokeWidth={1.5}
      strokeLinejoin="round"
      strokeDasharray={p.total}
      strokeDashoffset={0}
    />
  );

  const exchange = (
    k: string,
    x: number,
    y: number,
    label: string,
    labelDy: number,
  ) => (
    <g key={k} ref={set(k)} opacity={1}>
      <circle
        cx={x}
        cy={y}
        r={9}
        fill={NAVY}
        stroke="rgba(139,160,188,0.6)"
        strokeWidth={1.2}
      />
      <circle
        cx={x}
        cy={y}
        r={5.4}
        fill="none"
        stroke={VIOLET}
        strokeWidth={1}
      />
      <circle cx={x} cy={y} r={1.8} fill={VIOLET} opacity={0.9} />
      <text
        x={x}
        y={y + labelDy}
        textAnchor="middle"
        fontFamily={MONO_FONT}
        fontSize={9}
        fill={VIOLET}
        opacity={0.9}
      >
        {label}
      </text>
    </g>
  );

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[380px]"
    >
      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${H}`}
          width="100%"
          height={(H * w) / lw}
          className="block"
        >
          <defs>
            <filter id="topo-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* ── BROKER region: the derived topology lives here ─────── */}
          <rect
            x={FRAME_X}
            y={FRAME_Y}
            width={L.frameW}
            height={FRAME_H}
            rx={12}
            fill="rgba(139,160,188,0.02)"
            stroke={FRAME_STROKE}
            strokeWidth={1}
            strokeDasharray="5 5"
          />
          <rect
            ref={set("bflash")}
            x={FRAME_X}
            y={FRAME_Y}
            width={L.frameW}
            height={FRAME_H}
            rx={12}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.4}
            strokeDasharray="5 5"
            opacity={0}
          />
          <text
            x={FRAME_X + 14}
            y={FRAME_Y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.22em"
            fill={DIM}
          >
            BROKER
          </text>
          {/* the reduced-motion frame keeps the validated tag visible */}
          <g ref={set("vtag")} opacity={0.95}>
            <text
              x={FRAME_X + 92}
              y={FRAME_Y + 20}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.02em"
              fill={GREEN}
            >
              ✓ topology validated at startup
            </text>
          </g>

          {/* ── binding lanes, drawn in as each row declares itself ── */}
          {lane("lnA1", L.lnA1)}
          {lane("lnA2", L.lnA2)}
          {lane("lnB1", L.lnB1)}
          {lane("lnB2", L.lnB2)}
          {lane("lnC1", L.lnC1)}
          {lane("lnC2", L.lnC2)}
          {lane("stub", L.stub)}

          {/* vias where the bindings dock at the panel edge */}
          {ROW_TOPS.map((top) => (
            <circle
              key={top}
              cx={P_RIGHT}
              cy={top + ROW_H / 2}
              r={2}
              fill={NAVY}
              stroke="rgba(139,160,188,0.5)"
              strokeWidth={1.2}
            />
          ))}
          {/* dispatch via: reserve-inventory routes onward from here */}
          <circle
            ref={set("viaC")}
            cx={L.dpEnd}
            cy={Y_C}
            r={2.2}
            fill={NAVY}
            stroke="rgba(139,160,188,0.5)"
            strokeWidth={1.2}
            opacity={1}
          />

          {/* ── derived queue slots ────────────────────────────────── */}
          <g ref={set("qA")} opacity={1}>
            <rect
              x={L.pillX}
              y={Y_A - PILL_H / 2}
              width={L.pillW}
              height={PILL_H}
              rx={PILL_H / 2}
              fill="rgba(139,160,188,0.05)"
              stroke="rgba(139,160,188,0.35)"
              strokeWidth={1}
            />
            <text
              x={L.pillX + L.pillW / 2}
              y={Y_A + 3}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              fill={SLATE}
              fillOpacity={0.85}
            >
              orders.order-placed
            </text>
          </g>
          <rect
            ref={set("qAe")}
            x={L.pillX}
            y={Y_A - PILL_H / 2}
            width={L.pillW}
            height={PILL_H}
            rx={PILL_H / 2}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />
          <g ref={set("qB")} opacity={1}>
            <rect
              x={L.pillX}
              y={Y_B - PILL_H / 2}
              width={L.pillW}
              height={PILL_H}
              rx={PILL_H / 2}
              fill="rgba(139,160,188,0.05)"
              stroke="rgba(139,160,188,0.35)"
              strokeWidth={1}
            />
            <text
              x={L.pillX + L.pillW / 2}
              y={Y_B + 3}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              fill={SLATE}
              fillOpacity={0.85}
            >
              orders.get-product
            </text>
          </g>
          {/* small reply-queue slot hanging off the get-product queue */}
          <g ref={set("rq")} opacity={1}>
            <rect
              x={L.replyX}
              y={REPLY_Y}
              width={REPLY_W}
              height={REPLY_H}
              rx={REPLY_H / 2}
              fill="rgba(139,160,188,0.05)"
              stroke="rgba(139,160,188,0.3)"
              strokeWidth={1}
            />
            <text
              x={L.replyX + REPLY_W / 2}
              y={REPLY_Y + 11}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              fill={SLATE}
              fillOpacity={0.7}
            >
              reply
            </text>
          </g>

          {/* ── proof pulse, under the exchange rings ──────────────── */}
          <g ref={set("pf")} opacity={0}>
            <circle ref={set("pft3")} r={1.4} fill={CORAL} opacity={0} />
            <circle ref={set("pft2")} r={1.7} fill={CORAL} opacity={0} />
            <circle ref={set("pft1")} r={2} fill={CORAL} opacity={0} />
            <circle
              ref={set("pfglow")}
              r={6}
              fill={CORAL}
              opacity={0.22}
              filter="url(#topo-soft)"
            />
            <circle ref={set("pfcore")} r={2.5} fill={CORAL} />
            <circle ref={set("pfin")} r={1.1} fill={CORAL_SOFT} />
          </g>
          <circle
            ref={set("aring")}
            cx={L.pillR - 12}
            cy={Y_A}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          {/* ── derived exchange rings (double-ringed vias) ────────── */}
          {exchange("exA", L.ringX, Y_A, "order-placed", 22)}
          <circle
            ref={set("exAe")}
            cx={L.ringX}
            cy={Y_A}
            r={9}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.4}
            opacity={0}
          />
          {exchange("exB", L.ringX, Y_B, "get-product", 22)}
          {exchange("exC", L.ringX, Y_C, "reserve-inventory", 22)}

          {/* ── YOUR CODE panel: the only thing you write ──────────── */}
          <rect
            x={PX}
            y={PY}
            width={PW}
            height={PH}
            rx={10}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={PX + 12}
            y={PY + 18}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill={DIM}
          >
            YOUR CODE
          </text>
          {ROWS.map((r, i) => {
            const top = ROW_TOPS[i];
            return (
              <g key={r.label}>
                <rect
                  x={ROW_X}
                  y={top}
                  width={ROW_W}
                  height={ROW_H}
                  rx={5}
                  fill={SURFACE}
                  stroke={HAIR}
                  strokeWidth={1}
                />
                <rect
                  x={ROW_X}
                  y={top}
                  width={ROW_W}
                  height={ROW_H}
                  rx={5}
                  fill={r.bar}
                  opacity={r.tint}
                />
                <rect
                  x={ROW_X}
                  y={top + 5}
                  width={3}
                  height={ROW_H - 10}
                  rx={1.5}
                  fill={r.bar}
                />
                <text
                  x={ROW_X + 13}
                  y={top + 18}
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  fill={INK}
                >
                  {r.label}
                </text>
                <text
                  ref={set(`r${i}lit`)}
                  x={ROW_X + 13}
                  y={top + 18}
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  fill={r.soft}
                  opacity={0}
                >
                  {r.label}
                </text>
                <rect
                  ref={set(`r${i}echo`)}
                  x={ROW_X}
                  y={top}
                  width={ROW_W}
                  height={ROW_H}
                  rx={5}
                  fill="none"
                  stroke={r.bar}
                  strokeWidth={1.2}
                  opacity={0}
                />
              </g>
            );
          })}
        </svg>
      </div>
    </div>
  );
}
