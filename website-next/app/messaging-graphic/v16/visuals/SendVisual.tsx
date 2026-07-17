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

// Master loop. The story is the simultaneity at 2700: the green response
// heads back to the client while the coral command parks in the queue, then
// a long readable beat before the queue is drained.
const T = 10000;
const H = 176;
// Below this width the two service panels and the queue slot get too cramped
// to read, so we lay out at MIN_W and scale the stage down via the viewBox.
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
// The queue sits on a side channel below the main axis: work parked off the
// request path. 45-degree bends drop the lane down and lift it back up.
const DIAG = 26;
const Q_Y = 117;
const SLOT_W = 70;
const SLOT_H = 14;
const PW1 = 158; // ORDERS SERVICE panel width
const PW2 = 196; // INVENTORY SERVICE panel width

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

function buildLayout(lw: number): Layout {
  const chipR = CL_X + CL_W;
  const flexTotal = lw - (chipR + PW1 + PW2 + 2 * M);
  const run1 = Math.max(40, Math.min(110, Math.round(flexTotal * 0.25)));
  const px1 = chipR + run1;
  const x1R = px1 + PW1;
  const px2 = lw - M - PW2;
  // Whatever is left between the panels splits evenly around the queue slot
  // and its two 45-degree bends.
  const q = (px2 - x1R - 2 * DIAG - SLOT_W) / 4;
  const bx1 = x1R + q;
  const slotL = bx1 + DIAG + q;
  const slotR = slotL + SLOT_W;
  const bx2 = slotR + q;
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
    // Pulse polylines run a little past the painted lanes, into the panels to
    // the handler rows (or to the queue entry / under the client chip), so
    // every handoff is seamless.
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
      [bx1, LANE_Y],
      [bx1 + DIAG, Q_Y],
      [entry, Q_Y],
    ]),
    dlv: measure([
      [front, Q_Y],
      [bx2, Q_Y],
      [bx2 + DIAG, LANE_Y],
      [px2, LANE_Y],
      [rowX2 + 14, LANE_Y],
    ]),
    lane1D: `M${chipR} ${LANE_Y} H${px1}`,
    lane2D: laneD([
      [x1R, LANE_Y],
      [bx1, LANE_Y],
      [bx1 + DIAG, Q_Y],
      [slotL, Q_Y],
    ]),
    lane3D: laneD([
      [slotR, Q_Y],
      [bx2, Q_Y],
      [bx2 + DIAG, LANE_Y],
      [px2, LANE_Y],
    ]),
  };
}

export function SendVisual() {
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
      // The initial render is the meaningful static frame: the response tag
      // already shown, the command parked in the queue. Keep it.
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

      // 1 · cyan request: client -> PlaceOrderHandler, then the row works.
      if (t >= 300 && t < 1800) {
        const u = easeInOutCubic(ramp(t, 300, 1800));
        const r = 2.5 * (1 - ramp(t, 1680, 1800));
        placePulse("req", L.req, u, Math.min((t - 300) / 150, 1), r);
      } else {
        hidePulse("req");
      }
      setRing("ring1", (t - 1800) / 700, 3, 12);
      const w1 = t < 1950 ? ramp(t, 1800, 1950) : 1 - ramp(t, 2700, 3450);
      setO("h1echo", Math.max(0, w1) * 0.7);
      setO("h1lit", Math.max(0, w1) * 0.9);

      // 2 · at 2700 TWO things leave at once. Green response back to the
      // client; it dips under the chip and the 200 tag pops in and holds.
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

      // ... while the coral command drops to the queue and parks.
      if (t >= 2700 && t < 4400) {
        const u = easeInOutCubic(ramp(t, 2700, 4400));
        placePulse("cmd", L.cmd, u, Math.min((t - 2700) / 150, 1), 2.5);
      } else {
        hidePulse("cmd");
      }
      setRing("ringQ", (t - 4400) / 650, 2, 8);

      // 3 · the dot slides to the front and just SITS there until 6900: the
      // caller is done, the work is not.
      const dot = E.get("qdot");
      if (dot) {
        if (t >= 4400 && t < 6900) {
          const u = easeOutCubic(ramp(t, 4400, 4900));
          setDot("qdot", L.entry + (L.front - L.entry) * u, Q_Y);
          dot.setAttribute("opacity", "0.95");
        } else {
          dot.setAttribute("opacity", "0");
        }
      }

      // 4 · the queue drains: the dot travels into INVENTORY and the handler
      // row flashes; "handled later" blinks below.
      if (t >= 6900 && t < 8400) {
        const u = easeInOutCubic(ramp(t, 6900, 8400));
        const r = 2.5 * (1 - ramp(t, 8280, 8400));
        placePulse("dlv", L.dlv, u, Math.min((t - 6900) / 150, 1), r);
      } else {
        hidePulse("dlv");
      }
      setRing("ring2", (t - 8400) / 700, 3, 12);
      const w2 = t < 8550 ? ramp(t, 8400, 8550) : 1 - ramp(t, 8850, 9600);
      setO("h2echo", Math.max(0, w2) * 0.7);
      setO("h2lit", Math.max(0, w2) * 0.9);

      const de = t - 8550;
      let dd = 0;
      if (de >= 0) {
        dd = de < 1050 ? (Math.floor(de / 350) % 2 === 0 ? 0.9 : 0.3) : 0.6;
      }
      setO("later", dd * master);
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

          {/* substrate: faint pad-dot grid behind everything */}
          <rect width={lw} height={H} fill="url(#send-pcb-grid)" />

          {/* ── copper lanes: request, then the queue side channel ──── */}
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

          {/* via ring at the queue exit; the docks get pin rows instead */}
          <circle
            cx={L.slotR}
            cy={Q_Y}
            r={2.5}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />

          {/* ── queue slot on the side channel ─────────────────────── */}
          <rect
            x={L.slotL}
            y={Q_Y - SLOT_H / 2}
            width={SLOT_W}
            height={SLOT_H}
            rx={SLOT_H / 2}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />
          <text
            x={(L.slotL + L.slotR) / 2}
            y={Q_Y + SLOT_H / 2 + 13}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={SILK_SOFT}
          >
            reserve-inventory
          </text>
          {/* parked command; the reduced-motion frame shows it waiting */}
          <circle
            ref={set("qdot")}
            cx={L.front}
            cy={Q_Y}
            r={2.5}
            fill={CORAL}
            opacity={0.95}
          />

          {/* ── ORDERS SERVICE panel ───────────────────────────────── */}
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
          {/* pin rows where the lanes dock at the package edges */}
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

          {/* ── INVENTORY SERVICE panel ────────────────────────────── */}
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

          {/* ── pulses ─────────────────────────────────────────────── */}
          {pulseGlyph("req", CYAN, CYAN_SOFT)}
          {pulseGlyph("resp", GREEN, GREEN_SOFT)}
          {pulseGlyph("cmd", CORAL, CORAL_SOFT)}
          {pulseGlyph("dlv", CORAL, CORAL_SOFT)}

          {/* arrival rings */}
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
            cy={Q_Y}
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

          {/* ── client chip, drawn last so the response dips under it ─ */}
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
          {/* pin-1 dot + pin row where the request lane docks */}
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
          {/* the reduced-motion frame keeps the 200 tag visible */}
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
