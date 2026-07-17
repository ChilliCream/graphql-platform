"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import { CORAL, CORAL_SOFT, CYAN, MONO_FONT, VIOLET } from "../palette";

// The transports registration code lives in a separate block on the page;
// this card is the two-rail diagram alone.

type Pt = readonly [number, number];

interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

// Master loop. The Event Hub stream advances one dot spacing every 100ms
// (DOT_GAP / V_STREAM), and 2800 is an exact multiple of 100, so both the
// coral pulse and the dense stream are seamless across the wrap.
const T = 2800;
const H = 210;
// Below this width the 10px handler rows no longer fit next to the chips, so
// we lay out at MIN_W and scale the whole stage down via the SVG viewBox.
const MIN_W = 540;

const PULSE_MS = 1100;
const DOT_GAP = 10;
const V_STREAM = 0.1; // px per ms -> 100px/s firehose
const MAX_DOTS = 28;
const COUNT_BASE = 2481302;
const TICK_MS = 115; // ~9 counter ticks per second

const INK = "#a1a3af";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_DIM = "rgba(154,172,200,0.7)";
const GRID_DOT = "rgba(150,166,194,0.10)";

const CHIP_X = 8;
const CHIP_W = 96;
const CHIP_H = 26;
const CHIP_R = CHIP_X + CHIP_W;
const C1_Y = 51; // RabbitMQ chip center
const C2_Y = 158; // Event Hub chip center
const PANEL_Y = 10;
const PANEL_H = 190;
const ROW_H = 30;
const R1_TOP = 44;
const R2_TOP = 123;
const R1_Y = R1_TOP + ROW_H / 2;
const R2_Y = R2_TOP + ROW_H / 2;

// Widest row content: "DeviceTelemetryHandler" + gap + "2 481 302".
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
  const rowW = pw - 24;
  const bx1 = CHIP_R + Math.round(run * 0.6);
  const bx2 = CHIP_R + Math.round(run * 0.4);
  // 45-degree PCB bends: rail 1 steps down 8px, rail 2 steps up 20px.
  const pts1: readonly Pt[] = [
    [CHIP_R, C1_Y],
    [bx1, C1_Y],
    [bx1 + 8, R1_Y],
    [px, R1_Y],
  ];
  const pts2: readonly Pt[] = [
    [CHIP_R, C2_Y],
    [bx2, C2_Y],
    [bx2 + 20, R2_Y],
    [px, R2_Y],
  ];
  return {
    px,
    pw,
    rowX: px + 12,
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
      // The initial render is the meaningful static frame: both rails drawn,
      // frozen dots on each, counter at a fixed value. Keep it.
      return;
    }

    const E = els;
    let raf = 0;
    let running = false;
    let inView = false;
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

    // t wraps at T for the loops; life accumulates forever for the counter.
    const apply = (t: number, life: number) => {
      const L = layoutRef.current;

      // Event Hub firehose: a dense train of dots at fixed spacing and
      // speed, always flowing. Deterministic, no per-dot state.
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

      // RabbitMQ rail: one deliberate coral pulse per loop.
      if (t < PULSE_MS) {
        const u = easeInOutCubic(t / PULSE_MS);
        placePulse("p1", L.p1, u, Math.min(t / 120, 1));
      } else {
        placePulse("p1", L.p1, 1, 0);
      }

      // Arrival at OrderPlacedHandler: ring flash + row border/label flash.
      setRing("ring", (t - PULSE_MS) / 650, 3, 12);
      const e =
        t < PULSE_MS + 100
          ? ramp(t, PULSE_MS, PULSE_MS + 100)
          : 1 - ramp(t, PULSE_MS + 100, PULSE_MS + 1000);
      setO("r1echo", Math.max(0, e) * 0.7);
      setO("r1lit", Math.max(0, e) * 0.9);

      // RabbitMQ chip lights as it emits (pre-glow before the wrap so the
      // handoff into the next pulse is continuous).
      const cl = Math.max(1 - ramp(t, 0, 390), ramp(t, T - 210, T));
      setO("c1lit", cl * 0.9);
      setO("c1glow", cl * 0.28);

      // DeviceTelemetryHandler absorbs the stream with a constant shimmer,
      // never per-dot flashes. 3 cycles per loop keeps the wrap seamless.
      const sh = 0.5 + 0.5 * Math.sin(((t / T) * 2 - 0.5) * Math.PI * 3);
      setO("shim", 0.05 + 0.05 * sh);
      setO("c2glow", 0.12 + 0.08 * sh);

      // Events-today counter ticks up steadily while the card is on screen.
      const n = COUNT_BASE + Math.floor(life / TICK_MS);
      if (n !== countCache) {
        countCache = n;
        const el = E.get("count");
        if (el) {
          el.textContent = fmtCount(n);
        }
      }
    };

    let t = 0;
    let life = 0;
    let last = 0;

    const step = (now: number) => {
      const dt = Math.min(now - last, 50);
      last = now;
      t = (t + dt) % T;
      life += dt;
      apply(t, life);
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
  // Static-frame positions: coral pulse frozen mid-rail with its trail.
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

          {/* pad-dot substrate behind everything */}
          <rect
            x={0}
            y={0}
            width={lw}
            height={H}
            fill="url(#transports-grid)"
          />

          {/* ── shared service panel ───────────────────────────────── */}
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

          {/* OrderPlacedHandler row (fed by RabbitMQ) */}
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

          {/* DeviceTelemetryHandler row (fed by Event Hub) */}
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

          {/* ── copper rails, one per transport ────────────────────── */}
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

          {/* pin rows at the chip exits and where the rails dock into the
              panel; the middle pad of each row carries the rail */}
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

          {/* Event Hub firehose: dense deterministic dot train */}
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

          {/* RabbitMQ rail: deliberate coral pulse with trail */}
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

          {/* arrival ring at the OrderPlacedHandler entry */}
          <circle
            ref={set("ring")}
            cx={L.px}
            cy={R1_Y}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          {/* ── transport chips, drawn last so pulses dip underneath ── */}
          <text
            x={CHIP_X}
            y={26}
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.18em"
            fill={SILK}
          >
            TRANSPORTS
          </text>

          {/* RabbitMQ chip */}
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

          {/* Event Hub chip */}
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
