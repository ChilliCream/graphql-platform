"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  SLATE,
} from "../palette";

type Pt = readonly [number, number];

interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

const T = 16000;
const H = 240;
// Below this width the queue slots and chips get too cramped to read, so we
// lay out at MIN_W and scale the whole stage down via the SVG viewBox.
const MIN_W = 560;

const TRUNK_MS = 364;
const BRANCH_MS = 448;
const FLIGHT = TRUNK_MS + BRANCH_MS;
const ENTRY_MS = 364;
const CONSUME_MS = 392;
const SLIDE_MS = 350;
const FLASH_MS = 770;

// The publisher emits one event every ~1.7s for the first nine seconds; the
// tail of the loop belongs to the consumers working through their queues.
const PUBS = [380, 2040, 3700, 5360, 7020, 8680] as const;
const ARR = PUBS.map((p) => p + FLIGHT);

// Consume start times per row, FIFO against ARR (first arrival lands at
// 1192). Search drains on arrival, billing works at ~3.9s per message then
// clears its backlog, notifications is offline mid-loop and catches up in a
// quick burst afterwards.
const CONSUMES: readonly (readonly number[])[] = [
  ARR,
  [1200, 5180, 9100, 11470, 12980, 14490],
  [1200, 8080, 8530, 8980, 9430, 9740],
];

const OFF_START = 2490;
const OFF_END = 8000;
const CAUGHT_UP = 10420;

const ROW_Y = [58, 130, 202] as const;
const MID_Y = 130;
const PUB = { x: 8, y: MID_Y - 22, w: 104, h: 44 } as const;
const EX_X = 170;
const BEND_X = 200; // where the outer branches turn toward their rows
const ELBOW_R = 10; // rounded elbow corner radius
const SLOT_W = 90;
const SLOT_H = 14;
const CHIP_W = 140;
const CHIP_H = 28;
const Q_GAP = 11;

const SUBSCRIBERS = [
  { name: "SEARCH SVC", tag: "in step" },
  { name: "BILLING SVC", tag: "own pace" },
  // Initial tag doubles as the reduced-motion frame for this row.
  { name: "NOTIFICATIONS SVC", tag: "catching up" },
] as const;

// Reduced-motion static frame: queues holding 1 / 2 / 3 dots, chips lit.
const STATIC_DOTS = [1, 2, 3] as const;
const STATIC_LIT = [0.6, 0.3, 0.45] as const;

const NTAGS = [
  { t: "in step", f: SLATE, o: 0.55 },
  { t: "offline", f: AMBER, o: 0.95 },
  { t: "catching up", f: GREEN, o: 0.95 },
  { t: "nothing lost", f: GREEN, o: 0.6 },
] as const;

interface Layout {
  readonly chipX: number;
  readonly slotL: number;
  readonly slotR: number;
  readonly entry: number;
  readonly front: number;
  readonly chipEnter: number;
  readonly trunk: Polyline;
  readonly branches: readonly Polyline[];
  readonly branchD: readonly string[];
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

// Rounded 90-degree elbow: horizontal at y0, a quarter arc into a vertical
// run, and a quarter arc back out onto a horizontal at y1. The pair spans
// 2 * ELBOW_R horizontally, starting at x.
function elbowD(x: number, y0: number, y1: number): string {
  const s = y1 > y0 ? 1 : -1;
  return (
    `A ${ELBOW_R} ${ELBOW_R} 0 0 ${s > 0 ? 1 : 0} ${x + ELBOW_R} ${y0 + s * ELBOW_R} ` +
    `V ${y1 - s * ELBOW_R} ` +
    `A ${ELBOW_R} ${ELBOW_R} 0 0 ${s > 0 ? 0 : 1} ${x + 2 * ELBOW_R} ${y1}`
  );
}

// Polyline approximation of the same elbow pair (arc midpoints included) so
// pulses track the painted lane closely.
function elbowPts(x: number, y0: number, y1: number): Pt[] {
  const s = y1 > y0 ? 1 : -1;
  const r = ELBOW_R;
  return [
    [x, y0],
    [x + 0.7071 * r, y0 + s * 0.2929 * r],
    [x + r, y0 + s * r],
    [x + r, y1 - s * r],
    [x + 1.2929 * r, y1 - s * 0.2929 * r],
    [x + 2 * r, y1],
  ];
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

function mixColor(a: string, b: string, t: number): string {
  const pa = parseInt(a.slice(1), 16);
  const pb = parseInt(b.slice(1), 16);
  const ch = (shift: number) => {
    const ca = (pa >> shift) & 0xff;
    const cb = (pb >> shift) & 0xff;
    return Math.round(ca + (cb - ca) * t);
  };
  return `rgb(${ch(16)}, ${ch(8)}, ${ch(0)})`;
}

function buildLayout(lw: number): Layout {
  const chipX = lw - CHIP_W - 8;
  const slotR = chipX - 18;
  const slotL = slotR - SLOT_W;
  const entry = slotL + 7;
  return {
    chipX,
    slotL,
    slotR,
    entry,
    front: slotR - 9,
    chipEnter: chipX + 10,
    trunk: measure([
      [PUB.x + PUB.w, MID_Y],
      [EX_X, MID_Y],
    ]),
    // Pulse polylines run a little past the painted lane, to the point where
    // the queue dot takes over, so the handoff is seamless.
    branches: ROW_Y.map((y, r) =>
      r === 1
        ? measure([
            [EX_X, MID_Y],
            [entry, MID_Y],
          ])
        : measure([[EX_X, MID_Y], ...elbowPts(BEND_X, MID_Y, y), [entry, y]]),
    ),
    branchD: ROW_Y.map((y, r) =>
      r === 1
        ? `M${EX_X} ${MID_Y} H${slotL}`
        : `M${EX_X} ${MID_Y} H${BEND_X} ${elbowD(BEND_X, MID_Y, y)} H${slotL}`,
    ),
  };
}

export function FanoutVisual() {
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
      // The initial render is the meaningful static frame; keep it.
      return;
    }

    const E = els;
    let raf = 0;
    let running = false;
    let inView = false;
    let tagCache = -1;

    const setO = (k: string, v: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("opacity", v.toFixed(3));
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

    const setPart = (k: string, x: number, y: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("cx", x.toFixed(2));
        el.setAttribute("cy", y.toFixed(2));
      }
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
      const [x, y] = pointAt(poly, u);
      setPart(p + "core", x, y);
      setPart(p + "in", x, y);
      setPart(p + "glow", x, y);
      for (let k = 1; k <= 2; k++) {
        const uu = u - 0.07 * k;
        const el = E.get(p + "t" + k);
        if (el) {
          if (uu <= 0) {
            el.setAttribute("opacity", "0");
          } else {
            const [tx, ty] = pointAt(poly, uu);
            el.setAttribute("cx", tx.toFixed(2));
            el.setAttribute("cy", ty.toFixed(2));
            el.setAttribute("opacity", k === 1 ? "0.3" : "0.16");
          }
        }
      }
    };

    const setChip = (r: number, f: number, off: number) => {
      const base = E.get(`c${r}b`);
      const glow = E.get(`c${r}g`);
      const name = E.get(`c${r}n`);
      const color = mixColor(SLATE, CYAN, f);
      if (base) {
        base.setAttribute("stroke", color);
        base.setAttribute(
          "stroke-opacity",
          ((0.3 + 0.5 * f) * (1 - 0.55 * off)).toFixed(3),
        );
      }
      if (glow) {
        glow.setAttribute("opacity", (0.22 * f).toFixed(3));
      }
      if (name) {
        name.setAttribute("fill", color);
        name.setAttribute(
          "fill-opacity",
          ((0.75 + 0.25 * f) * (1 - 0.5 * off)).toFixed(3),
        );
      }
    };

    const apply = (t: number) => {
      const L = layoutRef.current;

      // publisher pulse in flight: trunk, then three branches at once
      let ai = -1;
      for (let k = 0; k < PUBS.length; k++) {
        if (t >= PUBS[k]) {
          ai = k;
        } else {
          break;
        }
      }
      let glow = 0.1;
      let exS = -1;
      if (ai >= 0) {
        const local = t - PUBS[ai];
        glow = 0.1 + 0.16 * clamp01(1 - local / 340);
        if (local < TRUNK_MS) {
          placePulse("tk", L.trunk, local / TRUNK_MS, Math.min(local / 110, 1));
        } else {
          placePulse("tk", L.trunk, 1, 0);
        }
        const bu = (local - TRUNK_MS) / BRANCH_MS;
        for (let r = 0; r < 3; r++) {
          placePulse(
            `bp${r}`,
            L.branches[r],
            clamp01(bu),
            bu >= 0 && bu < 1 ? 1 : 0,
          );
        }
        exS = (local - TRUNK_MS) / 590;
      } else {
        placePulse("tk", L.trunk, 0, 0);
        for (let r = 0; r < 3; r++) {
          placePulse(`bp${r}`, L.branches[r], 0, 0);
        }
      }
      setO("pg", glow);
      setRing("exf", exS, 2.5, 9);

      // arrival rings at the queue mouths (fan-out lands everywhere at once)
      let aS = -1;
      for (let k = 0; k < ARR.length; k++) {
        if (t >= ARR[k]) {
          aS = (t - ARR[k]) / 590;
        } else {
          break;
        }
      }
      for (let r = 0; r < 3; r++) {
        setRing(`ar${r}`, aS, 2, 8);
      }

      // notifications outage window
      const off =
        ramp(t, OFF_START, OFF_START + 350) *
        (1 - ramp(t, OFF_END, OFF_END + 350));

      for (let r = 0; r < 3; r++) {
        const cons = CONSUMES[r];
        const rowY = ROW_Y[r];

        // queue dots: fly in, wait in the slot, slide forward, get consumed
        for (let j = 0; j < PUBS.length; j++) {
          const el = E.get(`d${r}-${j}`);
          if (!el) {
            continue;
          }
          const a = ARR[j];
          const c = cons[j];
          let op = 0;
          let x = 0;
          if (t >= a && t < c + CONSUME_MS) {
            if (t < c) {
              let idx = 0;
              for (let i = 0; i < j; i++) {
                const ci = cons[i];
                if (t < ci) {
                  idx += 1;
                } else if (t < ci + SLIDE_MS) {
                  idx += 1 - easeOutCubic((t - ci) / SLIDE_MS);
                }
              }
              const target = L.front - Q_GAP * idx;
              const u = easeOutCubic(clamp01((t - a) / ENTRY_MS));
              x = L.entry + (target - L.entry) * u;
              op = 0.95;
            } else {
              const start =
                L.entry +
                (L.front - L.entry) * easeOutCubic(clamp01((c - a) / ENTRY_MS));
              const u = easeInOutCubic(clamp01((t - c) / CONSUME_MS));
              x = start + (L.chipEnter - start) * u;
              op = 0.95 * (1 - ramp(t, c + CONSUME_MS * 0.72, c + CONSUME_MS));
            }
          }
          if (op <= 0.01) {
            el.setAttribute("opacity", "0");
          } else {
            el.setAttribute("opacity", op.toFixed(3));
            el.setAttribute("cx", x.toFixed(2));
            el.setAttribute("cy", String(rowY));
          }
        }

        // each consumed dot flashes its chip briefly
        let f = 0;
        for (let j = 0; j < cons.length; j++) {
          const s = (t - (cons[j] + CONSUME_MS)) / FLASH_MS;
          if (s >= 0 && s < 1) {
            const v = Math.pow(1 - s, 1.6);
            if (v > f) {
              f = v;
            }
          }
        }
        if (r === 2) {
          setChip(r, f * (1 - off), off);
        } else {
          setChip(r, f, 0);
        }
      }

      // notifications tag: in step -> offline -> catching up -> nothing lost
      const bucket =
        t >= CAUGHT_UP ? 3 : t >= OFF_END ? 2 : t >= OFF_START ? 1 : 0;
      if (bucket !== tagCache) {
        tagCache = bucket;
        const el = E.get("ntag");
        if (el) {
          el.textContent = NTAGS[bucket].t;
          el.setAttribute("fill", NTAGS[bucket].f);
          el.setAttribute("opacity", String(NTAGS[bucket].o));
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

  const pulseGlyph = (p: string) => (
    <g key={p} ref={set(p)} opacity={0}>
      <circle ref={set(p + "t2")} r={1.6} fill={CORAL} opacity={0} />
      <circle ref={set(p + "t1")} r={2} fill={CORAL} opacity={0} />
      <circle
        ref={set(p + "glow")}
        r={6}
        fill={CORAL}
        opacity={0.2}
        filter="url(#fanout-soft)"
      />
      <circle ref={set(p + "core")} r={2.5} fill={CORAL} />
      <circle ref={set(p + "in")} r={1.1} fill={CORAL_SOFT} />
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
        <svg
          viewBox={`0 0 ${lw} ${H}`}
          width="100%"
          height={(H * w) / lw}
          className="block"
        >
          <defs>
            <filter
              id="fanout-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* lanes: trunk, three branches, three queue exits; every lane
              docks flush into a chip or tray border */}
          <path
            d={laneD(L.trunk.pts)}
            fill="none"
            stroke="rgba(139,160,188,0.45)"
            strokeWidth={1.75}
          />
          {L.branchD.map((d) => (
            <path
              key={d}
              d={d}
              fill="none"
              stroke="rgba(139,160,188,0.45)"
              strokeWidth={1.75}
            />
          ))}
          {ROW_Y.map((y) => (
            <path
              key={y}
              d={`M${L.slotR} ${y} H${L.chipX}`}
              fill="none"
              stroke="rgba(139,160,188,0.45)"
              strokeWidth={1.75}
            />
          ))}

          {/* exchange junction dot */}
          <circle cx={EX_X} cy={MID_Y} r={2} fill="rgba(139,160,188,0.45)" />
          <text
            x={EX_X}
            y={MID_Y + 17}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.08em"
            fill={SLATE}
            fillOpacity={0.45}
          >
            exchange
          </text>

          {/* queue trays: one per subscriber */}
          {ROW_Y.map((y) => (
            <rect
              key={y}
              x={L.slotL}
              y={y - SLOT_H / 2}
              width={SLOT_W}
              height={SLOT_H}
              rx={4}
              fill="rgba(139,160,188,0.07)"
              stroke="rgba(139,160,188,0.28)"
              strokeWidth={1}
            />
          ))}

          {/* queue dots */}
          {ROW_Y.map((y, r) => (
            <g key={y}>
              {PUBS.map((p, j) => (
                <circle
                  key={p}
                  ref={set(`d${r}-${j}`)}
                  cx={L.front - Q_GAP * j}
                  cy={y}
                  r={2.5}
                  fill={CORAL}
                  opacity={j < STATIC_DOTS[r] ? 0.95 : 0}
                />
              ))}
            </g>
          ))}

          {/* message pulses in flight */}
          {pulseGlyph("tk")}
          {pulseGlyph("bp0")}
          {pulseGlyph("bp1")}
          {pulseGlyph("bp2")}

          {/* flash rings */}
          <circle
            ref={set("exf")}
            cx={EX_X}
            cy={MID_Y}
            r={2.5}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          {ROW_Y.map((y, r) => (
            <circle
              key={y}
              ref={set(`ar${r}`)}
              cx={L.slotL}
              cy={y}
              r={2}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              opacity={0}
            />
          ))}

          {/* publisher chip */}
          <g>
            <rect
              ref={set("pg")}
              x={PUB.x}
              y={PUB.y}
              width={PUB.w}
              height={PUB.h}
              rx={10}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              opacity={0.18}
              filter="url(#fanout-soft)"
            />
            <rect
              x={PUB.x}
              y={PUB.y}
              width={PUB.w}
              height={PUB.h}
              rx={10}
              fill="#0c1322"
              stroke={CORAL}
              strokeOpacity={0.4}
              strokeWidth={1}
            />
            <text
              x={PUB.x + PUB.w / 2}
              y={MID_Y - 4}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing={1.2}
              fill={CORAL}
            >
              ORDERS SVC
            </text>
            <text
              x={PUB.x + PUB.w / 2}
              y={MID_Y + 11}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing={1}
              fill={SLATE}
              fillOpacity={0.5}
            >
              publisher
            </text>
          </g>

          {/* subscriber chips, drawn last so consumed dots dip underneath */}
          {SUBSCRIBERS.map((s, r) => {
            const y = ROW_Y[r];
            const lit = STATIC_LIT[r];
            const color = mixColor(SLATE, CYAN, lit);
            return (
              <g key={s.name}>
                <rect
                  ref={set(`c${r}g`)}
                  x={L.chipX}
                  y={y - CHIP_H / 2}
                  width={CHIP_W}
                  height={CHIP_H}
                  rx={10}
                  fill="none"
                  stroke={CYAN}
                  strokeWidth={1.5}
                  opacity={0.22 * lit}
                  filter="url(#fanout-soft)"
                />
                <rect
                  ref={set(`c${r}b`)}
                  x={L.chipX}
                  y={y - CHIP_H / 2}
                  width={CHIP_W}
                  height={CHIP_H}
                  rx={10}
                  fill="#0c1322"
                  stroke={color}
                  strokeOpacity={0.3 + 0.5 * lit}
                  strokeWidth={1}
                />
                <text
                  ref={set(`c${r}n`)}
                  x={L.chipX + CHIP_W / 2}
                  y={y + 3}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  letterSpacing={0.9}
                  fill={color}
                  fillOpacity={0.75 + 0.25 * lit}
                >
                  {s.name}
                </text>
                <text
                  ref={r === 2 ? set("ntag") : undefined}
                  x={L.chipX + CHIP_W / 2}
                  y={y + CHIP_H / 2 + 12}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={9}
                  letterSpacing={1}
                  fill={r === 2 ? GREEN : SLATE}
                  opacity={r === 2 ? 0.9 : 0.55}
                  style={{ transition: "fill .3s, opacity .3s" }}
                >
                  {s.tag}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
    </div>
  );
}
