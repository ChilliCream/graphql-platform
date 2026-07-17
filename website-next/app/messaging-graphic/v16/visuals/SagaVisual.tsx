"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
} from "../palette";

interface Pt {
  x: number;
  y: number;
}

interface Path {
  total: number;
  at(d: number): Pt;
}

interface LabelSpec {
  x: number;
  y: number;
  anchor: "middle" | "end";
  text: string;
}

interface Geom {
  railY: number;
  compY: number;
  lanes: { d: string; alpha: number }[];
  vias: Pt[];
  labels: LabelSpec[];
  pts: {
    seg1: Pt[];
    seg2: Pt[];
    seg2a: Pt[];
    drop: Pt[];
    ret: Pt[];
  };
}

interface Step {
  dur: number;
  path?: Path;
  enter?: () => void;
}

interface Schedule {
  steps: Step[];
  total: number;
}

const CHIP_LABELS = [
  "REQUESTED",
  "PROCESSING",
  "REFUNDED",
  "COMPENSATE",
] as const;
const CHIP_X = [0.15, 0.5, 0.85, 0.5];

const LABEL_DIM = "rgba(154,172,200,0.7)";
const PKG_BORDER = "rgba(158,176,204,0.44)";
const SILK = "rgba(154,172,200,0.75)";
const GRID_DOT = "rgba(150,166,194,0.10)";
const VIA_STROKE = "rgba(164,180,208,0.55)";

const CHIP_STYLES = {
  idle: {
    color: "#62748e",
    borderColor: PKG_BORDER,
    boxShadow: "none",
  },
  visited: {
    color: CYAN + "b3",
    borderColor: CYAN + "2e",
    boxShadow: "none",
  },
  active: {
    color: CYAN,
    borderColor: CYAN + "66",
    boxShadow: `0 0 14px ${CYAN}33`,
  },
  amber: {
    color: AMBER,
    borderColor: AMBER + "66",
    boxShadow: `0 0 14px ${AMBER}33`,
  },
  final: {
    color: GREEN,
    borderColor: GREEN + "66",
    boxShadow: `0 0 16px ${GREEN}40`,
  },
} as const;

type ChipState = keyof typeof CHIP_STYLES;

const TRAIL = [
  { off: 6, r: 2.2, o: 0.3 },
  { off: 12, r: 2.0, o: 0.2 },
  { off: 18, r: 1.8, o: 0.12 },
  { off: 24, r: 1.6, o: 0.06 },
];

function makePath(pts: Pt[]): Path {
  const cum: number[] = [0];
  let total = 0;
  for (let i = 1; i < pts.length; i++) {
    total += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    cum.push(total);
  }
  return {
    total,
    at(d: number): Pt {
      const t = Math.min(Math.max(d, 0), total);
      let i = 1;
      while (i < cum.length - 1 && cum[i] < t) {
        i++;
      }
      const len = cum[i] - cum[i - 1] || 1;
      const f = (t - cum[i - 1]) / len;
      return {
        x: pts[i - 1].x + (pts[i].x - pts[i - 1].x) * f,
        y: pts[i - 1].y + (pts[i].y - pts[i - 1].y) * f,
      };
    },
  };
}

function toD(pts: Pt[]): string {
  return pts.map((p, i) => `${i === 0 ? "M" : "L"}${p.x} ${p.y}`).join(" ");
}

function endOf(pts: Pt[]): Pt {
  return pts[pts.length - 1];
}

function easeInOut(t: number): number {
  return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
}

export function SagaVisual() {
  const wrapRef = useRef<HTMLDivElement>(null);
  const chipRefs = useRef<Array<HTMLDivElement | null>>([]);
  const labelRefs = useRef<Array<SVGTextElement | null>>([]);
  const trailRefs = useRef<Array<SVGCircleElement | null>>([]);
  const tokenGRef = useRef<SVGGElement>(null);
  const glowRef = useRef<SVGCircleElement>(null);
  const coreRef = useRef<SVGCircleElement>(null);
  const innerRef = useRef<SVGCircleElement>(null);
  const ringRef = useRef<SVGCircleElement>(null);
  const finalRef = useRef<HTMLDivElement>(null);

  const [geom, setGeom] = useState<Geom | null>(null);

  useLayoutEffect(() => {
    const wrap = wrapRef.current;
    if (!wrap) {
      return;
    }
    const measure = () => {
      const w = wrap.clientWidth;
      const h = wrap.clientHeight;
      if (w < 40 || h < 40) {
        return;
      }
      const railY = Math.round(h * 0.34);
      const compY = Math.round(h * 0.76);
      const box = CHIP_X.map((fr, i) => {
        const el = chipRefs.current[i];
        return {
          cx: fr * w,
          hw: el ? el.offsetWidth / 2 : 40,
          hh: el ? el.offsetHeight / 2 : 13,
        };
      });
      const [d0, c1, p2, m3] = box;
      const pLeft = p2.cx - p2.hw - 2;
      const jx = Math.max(
        Math.min(
          Math.max(c1.cx + c1.hw + 26, m3.cx + m3.hw + 40),
          p2.cx - p2.hw - 14,
        ),
        m3.cx + m3.hw + 14,
      );

      const seg1: Pt[] = [
        { x: d0.cx + d0.hw + 2, y: railY },
        { x: c1.cx - c1.hw - 2, y: railY },
      ];
      const seg2: Pt[] = [
        { x: c1.cx + c1.hw + 2, y: railY },
        { x: pLeft, y: railY },
      ];
      const seg2a: Pt[] = [
        { x: c1.cx + c1.hw + 2, y: railY },
        { x: jx, y: railY },
      ];
      const drop: Pt[] = [
        { x: jx, y: railY },
        { x: jx, y: compY - 10 },
        { x: jx - 10, y: compY },
        { x: m3.cx + m3.hw + 2, y: compY },
      ];
      const ret: Pt[] = [
        { x: m3.cx - m3.hw - 2, y: compY },
        { x: d0.cx + 10, y: compY },
        { x: d0.cx, y: compY - 10 },
        { x: d0.cx, y: railY + d0.hh + 2 },
      ];

      setGeom({
        railY,
        compY,
        lanes: [
          { d: toD(seg1), alpha: 0.4 },
          { d: toD(seg2), alpha: 0.4 },
          { d: toD(drop), alpha: 0.32 },
          { d: toD(ret), alpha: 0.32 },
        ],
        vias: [
          seg1[0],
          seg1[1],
          seg2[0],
          seg2[1],
          { x: jx, y: railY },
          endOf(drop),
          ret[0],
          endOf(ret),
        ],
        // Below ~460px the rail is too short for the event names to clear
        // the chips, so the labels drop and the chips carry the story alone.
        labels:
          w < 460
            ? []
            : [
                {
                  x: (seg1[0].x + seg1[1].x) / 2,
                  y: railY - 9,
                  anchor: "middle",
                  text: "ProcessRefund",
                },
                {
                  x: Math.min((seg2[0].x + pLeft) / 2, pLeft - 48),
                  y: railY - 9,
                  anchor: "middle",
                  text: "RefundCompleted",
                },
                {
                  x: jx - 9,
                  y: (railY + compY) / 2 + 3,
                  anchor: "end",
                  text: "RefundFailed",
                },
              ],
        pts: { seg1, seg2, seg2a, drop, ret },
      });
    };
    measure();
    const ro = new ResizeObserver(measure);
    ro.observe(wrap);
    return () => ro.disconnect();
  }, []);

  useEffect(() => {
    const wrap = wrapRef.current;
    if (!wrap || !geom) {
      return;
    }

    const setChip = (i: number, state: ChipState) => {
      const el = chipRefs.current[i];
      if (!el) {
        return;
      }
      const s = CHIP_STYLES[state];
      el.style.color = s.color;
      el.style.borderColor = s.borderColor;
      el.style.boxShadow = s.boxShadow;
    };

    const setFinalTag = (on: boolean) => {
      if (finalRef.current) {
        finalRef.current.style.opacity = on ? "1" : "0";
      }
    };

    const setLabel = (i: number, fill: string) => {
      const el = labelRefs.current[i];
      if (el) {
        el.style.fill = fill;
      }
    };

    const hideToken = () => {
      if (tokenGRef.current) {
        tokenGRef.current.style.opacity = "0";
      }
    };

    const showToken = (color: string) => {
      const g = tokenGRef.current;
      if (!g) {
        return;
      }
      g.style.opacity = "1";
      coreRef.current?.setAttribute("fill", color);
      glowRef.current?.setAttribute("fill", color);
      innerRef.current?.setAttribute(
        "fill",
        color === AMBER ? "#fde68a" : CORAL_SOFT,
      );
      for (const t of trailRefs.current) {
        t?.setAttribute("fill", color);
      }
    };

    // ring flash state
    let ringOn = false;
    let ringAge = 0;

    const flash = (p: Pt, color: string) => {
      const el = ringRef.current;
      if (!el) {
        return;
      }
      ringOn = true;
      ringAge = 0;
      el.setAttribute("cx", String(p.x));
      el.setAttribute("cy", String(p.y));
      el.setAttribute("stroke", color);
    };

    const moveToken = (p: Pt, dist: number, path: Path) => {
      coreRef.current?.setAttribute("cx", String(p.x));
      coreRef.current?.setAttribute("cy", String(p.y));
      innerRef.current?.setAttribute("cx", String(p.x));
      innerRef.current?.setAttribute("cy", String(p.y));
      glowRef.current?.setAttribute("cx", String(p.x));
      glowRef.current?.setAttribute("cy", String(p.y));
      trailRefs.current.forEach((el, k) => {
        if (!el) {
          return;
        }
        const dd = dist - TRAIL[k].off;
        if (dd < 0) {
          el.style.opacity = "0";
          return;
        }
        el.style.opacity = String(TRAIL[k].o);
        const tp = path.at(dd);
        el.setAttribute("cx", String(tp.x));
        el.setAttribute("cy", String(tp.y));
      });
    };

    const resetCycle = () => {
      setChip(0, "idle");
      setChip(1, "idle");
      setChip(2, "idle");
      setChip(3, "idle");
      setFinalTag(false);
      setLabel(0, LABEL_DIM);
      setLabel(1, LABEL_DIM);
      setLabel(2, LABEL_DIM);
      hideToken();
    };

    const paths = {
      seg1: makePath(geom.pts.seg1),
      seg2: makePath(geom.pts.seg2),
      seg2a: makePath(geom.pts.seg2a),
      drop: makePath(geom.pts.drop),
      ret: makePath(geom.pts.ret),
    };

    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;

    resetCycle();

    if (reduced) {
      // static final frame: happy path completed
      setChip(0, "visited");
      setChip(1, "visited");
      setChip(2, "final");
      setFinalTag(true);
      return;
    }

    const sched = (steps: Step[]): Schedule => ({
      steps,
      total: steps.reduce((s, x) => s + x.dur, 0),
    });

    const happy = sched([
      {
        dur: 675,
        enter: () => {
          setChip(0, "active");
          hideToken();
        },
      },
      {
        dur: 1200,
        path: paths.seg1,
        enter: () => {
          setChip(0, "visited");
          showToken(CORAL);
          setLabel(0, CORAL);
        },
      },
      {
        dur: 675,
        enter: () => {
          setChip(1, "active");
          flash(endOf(geom.pts.seg1), CYAN);
          hideToken();
          setLabel(0, LABEL_DIM);
        },
      },
      {
        dur: 1200,
        path: paths.seg2,
        enter: () => {
          setChip(1, "visited");
          showToken(CORAL);
          setLabel(1, CORAL);
        },
      },
      {
        dur: 1500,
        enter: () => {
          setChip(2, "final");
          setFinalTag(true);
          flash(endOf(geom.pts.seg2), GREEN);
          hideToken();
          setLabel(1, LABEL_DIM);
        },
      },
    ]);

    const fail = sched([
      {
        dur: 675,
        enter: () => {
          setChip(0, "active");
          hideToken();
        },
      },
      {
        dur: 1200,
        path: paths.seg1,
        enter: () => {
          setChip(0, "visited");
          showToken(CORAL);
          setLabel(0, CORAL);
        },
      },
      {
        dur: 675,
        enter: () => {
          setChip(1, "active");
          flash(endOf(geom.pts.seg1), CYAN);
          hideToken();
          setLabel(0, LABEL_DIM);
        },
      },
      {
        dur: 390,
        path: paths.seg2a,
        enter: () => {
          setChip(1, "visited");
          showToken(CORAL);
        },
      },
      {
        dur: 780,
        path: paths.drop,
        enter: () => {
          showToken(AMBER);
          setLabel(2, AMBER);
        },
      },
      {
        dur: 780,
        enter: () => {
          setChip(3, "amber");
          flash(endOf(geom.pts.drop), AMBER);
          hideToken();
          setLabel(2, LABEL_DIM);
        },
      },
      {
        dur: 1125,
        path: paths.ret,
        enter: () => {
          showToken(AMBER);
        },
      },
      {
        dur: 375,
        enter: () => {
          flash(endOf(geom.pts.ret), CYAN);
          hideToken();
        },
      },
    ]);

    let raf = 0;
    let running = false;
    let inView = false;
    let last = 0;
    let elapsed = 0;
    let cycle = 0;
    let prevStep = -1;

    const scheduleFor = (c: number) => (c % 3 === 2 ? fail : happy);

    const frame = (now: number) => {
      if (!running) {
        return;
      }
      const dt = Math.min(now - last, 100);
      last = now;
      elapsed += dt;

      let sch = scheduleFor(cycle);
      while (elapsed >= sch.total) {
        elapsed -= sch.total;
        cycle += 1;
        prevStep = -1;
        resetCycle();
        sch = scheduleFor(cycle);
      }

      let t = elapsed;
      let idx = 0;
      while (idx < sch.steps.length - 1 && t >= sch.steps[idx].dur) {
        t -= sch.steps[idx].dur;
        idx++;
      }
      if (idx > prevStep) {
        for (let i = prevStep + 1; i <= idx; i++) {
          sch.steps[i].enter?.();
        }
        prevStep = idx;
      }

      const st = sch.steps[idx];
      if (st.path) {
        const f = easeInOut(Math.min(1, t / st.dur));
        const dist = f * st.path.total;
        moveToken(st.path.at(dist), dist, st.path);
      }

      if (ringOn) {
        ringAge += dt;
        const q = ringAge / 900;
        const el = ringRef.current;
        if (el) {
          if (q >= 1) {
            ringOn = false;
            el.setAttribute("opacity", "0");
          } else {
            el.setAttribute("r", String(3 + 12 * q));
            el.setAttribute("opacity", String(0.45 * (1 - q)));
          }
        }
      }

      raf = requestAnimationFrame(frame);
    };

    const start = () => {
      if (running) {
        return;
      }
      running = true;
      last = performance.now();
      raf = requestAnimationFrame(frame);
    };
    const stop = () => {
      running = false;
      cancelAnimationFrame(raf);
    };
    const update = () => {
      if (inView && !document.hidden) {
        start();
      } else {
        stop();
      }
    };

    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1]?.isIntersecting ?? false;
        update();
      },
      { threshold: 0.15 },
    );
    io.observe(wrap);
    document.addEventListener("visibilitychange", update);

    return () => {
      stop();
      io.disconnect();
      document.removeEventListener("visibilitychange", update);
    };
  }, [geom]);

  const railY = geom?.railY ?? 103;
  const compY = geom?.compY ?? 231;
  const chipTops = [railY, railY, railY, compY];

  return (
    <div className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-6 backdrop-blur sm:h-[380px]">
      <div
        ref={wrapRef}
        aria-hidden="true"
        className="relative min-h-[280px] flex-1"
      >
        <svg className="absolute inset-0 h-full w-full overflow-visible">
          <defs>
            <pattern
              id="saga-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>
          {/* pad-dot substrate behind everything */}
          <rect x={0} y={0} width="100%" height="100%" fill="url(#saga-grid)" />
          {geom && (
            <g>
              {geom.lanes.map((lane, i) => (
                <path
                  key={i}
                  d={lane.d}
                  fill="none"
                  stroke={`rgba(139,160,188,${lane.alpha})`}
                  strokeWidth={1.5}
                  strokeLinejoin="round"
                />
              ))}
              {geom.vias.map((v, i) => (
                <circle
                  key={i}
                  cx={v.x}
                  cy={v.y}
                  r={2.5}
                  fill={NAVY}
                  stroke={VIA_STROKE}
                  strokeWidth={1.2}
                />
              ))}
              {geom.labels.map((l, i) => (
                <text
                  key={l.text}
                  ref={(el) => {
                    labelRefs.current[i] = el;
                  }}
                  x={l.x}
                  y={l.y}
                  textAnchor={l.anchor}
                  fontSize={9.5}
                  letterSpacing="0.06em"
                  style={{
                    fill: LABEL_DIM,
                    fontFamily: MONO_FONT,
                    transition: "fill .25s",
                  }}
                >
                  {l.text}
                </text>
              ))}
              <g
                ref={tokenGRef}
                style={{ opacity: 0, transition: "opacity .18s" }}
              >
                {TRAIL.map((tr, k) => (
                  <circle
                    key={k}
                    ref={(el) => {
                      trailRefs.current[k] = el;
                    }}
                    r={tr.r}
                    fill={CORAL}
                    style={{ opacity: 0 }}
                  />
                ))}
                <circle
                  ref={glowRef}
                  r={8}
                  fill={CORAL}
                  opacity={0.16}
                  filter="url(#saga-token-blur)"
                />
                <circle ref={coreRef} r={2.6} fill={CORAL} />
                <circle ref={innerRef} r={1.1} fill={CORAL_SOFT} />
              </g>
              <circle
                ref={ringRef}
                r={0}
                fill="none"
                strokeWidth={1.5}
                opacity={0}
              />
              <defs>
                <filter
                  id="saga-token-blur"
                  x="-150%"
                  y="-150%"
                  width="400%"
                  height="400%"
                >
                  <feGaussianBlur stdDeviation={3} />
                </filter>
              </defs>
            </g>
          )}
        </svg>
        {CHIP_LABELS.map((label, i) => (
          <div
            key={label}
            ref={(el) => {
              chipRefs.current[i] = el;
            }}
            className="bg-cc-surface text-cc-nav-label absolute -translate-x-1/2 -translate-y-1/2 rounded-[3px] border px-2.5 py-1.5 font-mono text-[0.7rem] tracking-[0.12em] whitespace-nowrap uppercase"
            style={{
              left: `${CHIP_X[i] * 100}%`,
              top: chipTops[i],
              borderColor: PKG_BORDER,
              transition: "color .3s, border-color .3s, box-shadow .3s",
            }}
          >
            <span
              aria-hidden
              className="pointer-events-none absolute top-[3px] left-[3px] h-[2.4px] w-[2.4px] rounded-full"
              style={{ background: SILK }}
            />
            {label}
          </div>
        ))}
        <div
          ref={finalRef}
          className="absolute -translate-x-1/2 font-mono text-[0.6rem] tracking-[0.26em] uppercase opacity-0"
          style={{
            left: "85%",
            top: railY - 34,
            color: GREEN,
            transition: "opacity .3s",
          }}
        >
          FINAL
        </div>
      </div>
    </div>
  );
}
