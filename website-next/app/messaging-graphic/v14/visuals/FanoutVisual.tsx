"use client";

import { useEffect, useRef } from "react";

import { CORAL, CYAN, MONO_FONT, NAVY, SLATE } from "../palette";

type Pt = readonly [number, number];

interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

const PERIOD = 3000;
const TRUNK_END = 700;
const BRANCH_END = 1500;

const SUB_X = 468;
const SUB_W = 144;
const SUB_Y = [70, 130, 190] as const;

const TRUNK_PTS: readonly Pt[] = [
  [134, 130],
  [300, 130],
];

const BRANCH_LANES = [
  {
    id: "top",
    pts: [
      [300, 130],
      [360, 70],
      [468, 70],
    ] as readonly Pt[],
  },
  {
    id: "mid",
    pts: [
      [300, 130],
      [468, 130],
    ] as readonly Pt[],
  },
  {
    id: "bot",
    pts: [
      [300, 130],
      [360, 190],
      [468, 190],
    ] as readonly Pt[],
  },
] as const;

const SUBSCRIBERS = [
  { name: "SEARCH", tag: "in-proc" },
  { name: "NOTIFICATIONS", tag: "rabbitmq" },
  { name: "ANALYTICS", tag: "postgres" },
] as const;

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

const TRUNK = measure(TRUNK_PTS);
const BRANCHES = BRANCH_LANES.map((lane) => measure(lane.pts));

function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
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

function laneD(pts: readonly Pt[]): string {
  return pts.map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`).join(" ");
}

interface PulseGlyphProps {
  readonly id: string;
}

function PulseGlyph({ id }: PulseGlyphProps) {
  return (
    <g data-pulse={id} opacity="0">
      <circle
        data-p="glow"
        r="6.5"
        fill={CORAL}
        opacity="0.16"
        filter="url(#fanout-soft)"
      />
      <circle data-p="t2" r="1.2" fill={CORAL} opacity="0.1" />
      <circle data-p="t1" r="1.6" fill={CORAL} opacity="0.2" />
      <circle data-p="t0" r="2" fill={CORAL} opacity="0.34" />
      <circle data-p="dot" r="2.5" fill={CORAL} />
    </g>
  );
}

export function FanoutVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    const svg = svgRef.current;
    if (!root || !svg) {
      return;
    }

    const pulse = (id: string) => {
      const g = svg.querySelector<SVGGElement>(`[data-pulse="${id}"]`);
      if (!g) {
        return null;
      }
      const grab = (name: string) =>
        g.querySelector<SVGCircleElement>(`[data-p="${name}"]`);
      const dot = grab("dot");
      const glow = grab("glow");
      const t0 = grab("t0");
      const t1 = grab("t1");
      const t2 = grab("t2");
      if (!dot || !glow || !t0 || !t1 || !t2) {
        return null;
      }
      return { g, dot, glow, trails: [t0, t1, t2] };
    };
    type PulseRefs = NonNullable<ReturnType<typeof pulse>>;

    const chip = (i: number) => {
      const g = svg.querySelector<SVGGElement>(`[data-sub="${i}"]`);
      if (!g) {
        return null;
      }
      const base = g.querySelector<SVGRectElement>('[data-c="base"]');
      const glow = g.querySelector<SVGRectElement>('[data-c="glow"]');
      const name = g.querySelector<SVGTextElement>('[data-c="name"]');
      const tag = g.querySelector<SVGTextElement>('[data-c="tag"]');
      if (!base || !glow || !name || !tag) {
        return null;
      }
      return { base, glow, name, tag };
    };
    type ChipRefs = NonNullable<ReturnType<typeof chip>>;

    const trunkPulse = pulse("trunk");
    const jFlash = svg.querySelector<SVGCircleElement>('[data-flash="j"]');
    const pubGlow = svg.querySelector<SVGRectElement>('[data-c="pub-glow"]');
    if (!trunkPulse || !jFlash || !pubGlow) {
      return;
    }

    const branchPulses: PulseRefs[] = [];
    const chips: ChipRefs[] = [];
    const arriveFlashes: SVGCircleElement[] = [];
    for (let i = 0; i < SUBSCRIBERS.length; i++) {
      const p = pulse(`b${i}`);
      const c = chip(i);
      const a = svg.querySelector<SVGCircleElement>(`[data-flash="a${i}"]`);
      if (!p || !c || !a) {
        return;
      }
      branchPulses.push(p);
      chips.push(c);
      arriveFlashes.push(a);
    }

    const setPulse = (
      p: PulseRefs,
      poly: Polyline,
      u: number,
      opacity: number,
    ) => {
      if (opacity <= 0) {
        p.g.setAttribute("opacity", "0");
        return;
      }
      p.g.setAttribute("opacity", opacity.toFixed(3));
      const [x, y] = pointAt(poly, u);
      for (const el of [p.dot, p.glow]) {
        el.setAttribute("cx", x.toFixed(2));
        el.setAttribute("cy", y.toFixed(2));
      }
      p.trails.forEach((el, k) => {
        const [tx, ty] = pointAt(poly, u - 0.055 * (k + 1));
        el.setAttribute("cx", tx.toFixed(2));
        el.setAttribute("cy", ty.toFixed(2));
      });
    };

    const setChip = (i: number, f: number) => {
      const c = chips[i];
      const color = mixColor(SLATE, CYAN, f);
      c.base.setAttribute("stroke", color);
      c.base.setAttribute("stroke-opacity", (0.3 + 0.5 * f).toFixed(3));
      c.glow.setAttribute("opacity", (0.24 * f).toFixed(3));
      c.name.setAttribute("fill", color);
      c.name.setAttribute("fill-opacity", (0.78 + 0.22 * f).toFixed(3));
      c.tag.setAttribute("fill-opacity", (0.5 + 0.2 * f).toFixed(3));
    };

    const setRing = (
      el: SVGCircleElement,
      s: number,
      r0: number,
      dr: number,
    ) => {
      if (s < 0 || s >= 1) {
        el.setAttribute("opacity", "0");
        return;
      }
      el.setAttribute("r", (r0 + dr * s).toFixed(2));
      el.setAttribute("opacity", (0.55 * (1 - s)).toFixed(3));
    };

    const draw = (t: number) => {
      // publisher glows a little brighter as the pulse departs
      const launch = clamp01(1 - t / 240);
      pubGlow.setAttribute("opacity", (0.12 + 0.14 * launch).toFixed(3));

      // trunk pulse: publish -> junction
      const uTrunk = clamp01(t / TRUNK_END);
      setPulse(
        trunkPulse,
        TRUNK,
        uTrunk,
        t < TRUNK_END ? Math.min(t / 80, 1) : 0,
      );

      // junction via flash as the pulse passes through
      setRing(jFlash, (t - TRUNK_END) / 500, 2.5, 8);

      // three branch pulses, normalized so all arrive simultaneously
      const uBranch = clamp01((t - TRUNK_END) / (BRANCH_END - TRUNK_END));
      const branchOpacity = t >= TRUNK_END && t < BRANCH_END ? 1 : 0;
      branchPulses.forEach((p, i) => {
        setPulse(p, BRANCHES[i], uBranch, branchOpacity);
      });

      // synchronized arrival: ring flashes and cyan chip flash, then decay
      const arrive = (t - BRANCH_END) / 600;
      for (const el of arriveFlashes) {
        setRing(el, arrive, 3, 13);
      }
      const decay = (t - BRANCH_END) / 850;
      const f = decay >= 0 && decay <= 1 ? Math.pow(1 - decay, 1.5) : 0;
      for (let i = 0; i < chips.length; i++) {
        setChip(i, f);
      }
    };

    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    if (reduced) {
      // static final frame: broadcast in flight, all subscribers lit
      setPulse(trunkPulse, TRUNK, 0, 0);
      branchPulses.forEach((p, i) => {
        setPulse(p, BRANCHES[i], 0.72, 1);
      });
      for (let i = 0; i < chips.length; i++) {
        setChip(i, 0.85);
      }
      jFlash.setAttribute("opacity", "0");
      pubGlow.setAttribute("opacity", "0.2");
      return;
    }

    let raf = 0;
    let last = 0;
    let phase = 0;
    let running = false;
    let inView = false;

    const frame = (now: number) => {
      if (!running) {
        return;
      }
      const dt = Math.min(now - last, 80);
      last = now;
      phase = (phase + dt) % PERIOD;
      draw(phase);
      raf = requestAnimationFrame(frame);
    };

    const update = () => {
      const should = inView && !document.hidden;
      if (should && !running) {
        running = true;
        last = performance.now();
        raf = requestAnimationFrame(frame);
      } else if (!should && running) {
        running = false;
        cancelAnimationFrame(raf);
      }
    };

    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1]?.isIntersecting ?? false;
        update();
      },
      { threshold: 0.2 },
    );
    io.observe(root);
    const onVisibility = () => update();
    document.addEventListener("visibilitychange", onVisibility);
    draw(0);

    return () => {
      running = false;
      cancelAnimationFrame(raf);
      io.disconnect();
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, []);

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[380px]"
    >
      <div className="flex min-h-0 flex-1 items-center">
        <svg
          ref={svgRef}
          viewBox="0 0 640 260"
          className="h-full w-full"
          preserveAspectRatio="xMidYMid meet"
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

          {/* copper lanes */}
          <path
            d={laneD(TRUNK_PTS)}
            fill="none"
            stroke="rgba(139,160,188,0.4)"
            strokeWidth={1.5}
          />
          {BRANCH_LANES.map((lane) => (
            <path
              key={lane.id}
              d={laneD(lane.pts)}
              fill="none"
              stroke="rgba(139,160,188,0.4)"
              strokeWidth={1.5}
              strokeLinejoin="miter"
            />
          ))}

          {/* junction via */}
          <circle
            cx={300}
            cy={130}
            r={2.5}
            fill={NAVY}
            stroke={SLATE}
            strokeOpacity={0.6}
            strokeWidth={1.5}
          />

          {/* publisher chip */}
          <g>
            <rect
              data-c="pub-glow"
              x={30}
              y={113}
              width={104}
              height={34}
              rx={6}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              opacity={0.12}
              filter="url(#fanout-soft)"
            />
            <rect
              x={30}
              y={113}
              width={104}
              height={34}
              rx={6}
              fill="#0c1322"
              stroke={CORAL}
              strokeOpacity={0.4}
              strokeWidth={1}
            />
            <text
              x={82}
              y={133.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10.5}
              letterSpacing={1.5}
              fill={CORAL}
            >
              PUBLISH
            </text>
          </g>

          {/* message label above the outgoing lane */}
          <text
            x={142}
            y={119}
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing={0.6}
            fill={CORAL}
            fillOpacity={0.85}
          >
            ReviewCreated
          </text>

          {/* subscriber chips */}
          {SUBSCRIBERS.map((s, i) => {
            const cy = SUB_Y[i];
            return (
              <g key={s.name} data-sub={i}>
                <rect
                  data-c="glow"
                  x={SUB_X}
                  y={cy - 20}
                  width={SUB_W}
                  height={40}
                  rx={6}
                  fill="none"
                  stroke={CYAN}
                  strokeWidth={1.5}
                  opacity={0}
                  filter="url(#fanout-soft)"
                />
                <rect
                  data-c="base"
                  x={SUB_X}
                  y={cy - 20}
                  width={SUB_W}
                  height={40}
                  rx={6}
                  fill="#0c1322"
                  stroke={SLATE}
                  strokeOpacity={0.3}
                  strokeWidth={1}
                />
                <text
                  data-c="name"
                  x={SUB_X + SUB_W / 2}
                  y={cy - 1}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  letterSpacing={1.4}
                  fill={SLATE}
                  fillOpacity={0.78}
                >
                  {s.name}
                </text>
                <text
                  data-c="tag"
                  x={SUB_X + SUB_W / 2}
                  y={cy + 12}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={8}
                  letterSpacing={1}
                  fill={SLATE}
                  fillOpacity={0.5}
                >
                  {s.tag}
                </text>
              </g>
            );
          })}

          {/* flash rings */}
          <circle
            data-flash="j"
            cx={300}
            cy={130}
            r={2.5}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          {SUB_Y.map((cy, i) => (
            <circle
              key={cy}
              data-flash={`a${i}`}
              cx={SUB_X}
              cy={cy}
              r={3}
              fill="none"
              stroke={CYAN}
              strokeWidth={1.5}
              opacity={0}
            />
          ))}

          {/* message pulses */}
          <PulseGlyph id="trunk" />
          <PulseGlyph id="b0" />
          <PulseGlyph id="b1" />
          <PulseGlyph id="b2" />
        </svg>
      </div>
      <div className="text-cc-nav-label mt-3 font-mono text-[0.6rem] tracking-[0.22em] uppercase">
        ONE PUBLISH · EVERY SUBSCRIBER · ON ITS OWN SCHEDULE
      </div>
    </div>
  );
}
