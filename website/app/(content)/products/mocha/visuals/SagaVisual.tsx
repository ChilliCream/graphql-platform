"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
} from "@/src/components/mocha/palette";

interface Pt {
  readonly x: number;
  readonly y: number;
}

interface Path {
  readonly total: number;
  at(d: number): Pt;
}

interface Step {
  readonly dur: number;
  readonly path?: Path;
  readonly fadeIn?: boolean;
  readonly fadeOut?: boolean;
  readonly enter?: () => void;
}

interface Schedule {
  readonly steps: readonly Step[];
  readonly total: number;
}

// Below MIN_W the rail is too short for the event names to clear the state
// panels, so we lay out at MIN_W and scale the whole stage down via the
// SVG viewBox.
const MIN_W = 640;
const H = 280;
const RAIL_Y = 100;
const COMP_Y = 222;
const PANEL_H = 40;

const SURFACE = "#0c1322";
const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const SILK = "rgba(154,172,200,0.75)";
const LABEL_DIM = "rgba(154,172,200,0.7)";
const IDLE_TEXT = "#62748e";

// Silkscreen title, border echo, and the top-right activity LED per state.
const CHIP_STYLES = {
  idle: { text: IDLE_TEXT, edge: CYAN, edgeOp: 0, led: CYAN, ledOp: 0 },
  visited: { text: CYAN + "b3", edge: CYAN, edgeOp: 0.2, led: CYAN, ledOp: 0 },
  active: { text: CYAN, edge: CYAN, edgeOp: 0.55, led: CYAN, ledOp: 0.9 },
  amber: { text: AMBER, edge: AMBER, edgeOp: 0.55, led: AMBER, ledOp: 0.9 },
  final: { text: GREEN, edge: GREEN, edgeOp: 0.6, led: GREEN, ledOp: 0.9 },
} as const;

type ChipState = keyof typeof CHIP_STYLES;

interface Box {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
}

interface PanelBox extends Box {
  readonly title: string;
}

interface Pin {
  readonly x: number;
  readonly y: number;
  readonly side: "left" | "right" | "top" | "bottom";
}

interface LabelSpec {
  readonly x: number;
  readonly y: number;
  readonly anchor: "middle" | "end";
  readonly text: string;
}

interface Layout {
  readonly panels: readonly PanelBox[];
  readonly lanes: readonly string[];
  readonly pts: {
    readonly seg1: readonly Pt[];
    readonly seg2: readonly Pt[];
    readonly seg2a: readonly Pt[];
    readonly drop: readonly Pt[];
    readonly ret: readonly Pt[];
  };
  readonly pill: Box;
  readonly vias: readonly Pt[];
  readonly pins: readonly Pin[];
  readonly labels: readonly LabelSpec[];
  readonly finalX: number;
}

function makePath(pts: readonly Pt[]): Path {
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

function toD(pts: readonly Pt[]): string {
  return pts.map((p, i) => `${i === 0 ? "M" : "L"}${p.x} ${p.y}`).join(" ");
}

function endOf(pts: readonly Pt[]): Pt {
  return pts[pts.length - 1];
}

function easeInOut(t: number): number {
  return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
}

function easeOutCubic(u: number): number {
  return 1 - Math.pow(1 - u, 3);
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
      {[-1, 0, 1].map((i) =>
        side === "left" || side === "right" ? (
          <rect
            key={i}
            x={side === "left" ? x - 3.5 : x}
            y={y + i * 5 - 1}
            width={3.5}
            height={2}
            fill={PAD_FILL}
          />
        ) : (
          <rect
            key={i}
            x={x + i * 5 - 1}
            y={side === "top" ? y - 3.5 : y}
            width={2}
            height={3.5}
            fill={PAD_FILL}
          />
        ),
      )}
    </g>
  );
}

function buildLayout(lw: number): Layout {
  const m = 8;
  const py = RAIL_Y - PANEL_H / 2;

  // REQUESTED and REFUNDED hug the board edges; PROCESSING and COMPENSATE
  // share the center column. That leaves two wide rail spans where the
  // ProcessRefund / RefundCompleted silkscreen prints fully in the clear.
  const panels: readonly PanelBox[] = [
    { x: m, y: py, w: 108, h: PANEL_H, title: "REQUESTED" },
    {
      x: Math.round(lw / 2) - 58,
      y: py,
      w: 116,
      h: PANEL_H,
      title: "PROCESSING",
    },
    { x: lw - m - 100, y: py, w: 100, h: PANEL_H, title: "REFUNDED" },
    {
      x: Math.round(lw / 2) - 58,
      y: COMP_Y - PANEL_H / 2,
      w: 116,
      h: PANEL_H,
      title: "COMPENSATE",
    },
  ];
  const [req, proc, refd, comp] = panels;
  const reqR = req.x + req.w;
  const reqCx = req.x + req.w / 2;
  const procR = proc.x + proc.w;
  const compR = comp.x + comp.w;

  // The failure branch tees off the rail a fixed run right of PROCESSING:
  // room below for the queue pill on the compensation approach, and the
  // RefundFailed label prints in open board left of the drop.
  const jx = Math.round(lw / 2) + 132;

  const seg1: readonly Pt[] = [
    { x: reqR, y: RAIL_Y },
    { x: proc.x, y: RAIL_Y },
  ];
  const seg2: readonly Pt[] = [
    { x: procR, y: RAIL_Y },
    { x: refd.x, y: RAIL_Y },
  ];
  const seg2a: readonly Pt[] = [
    { x: procR, y: RAIL_Y },
    { x: jx, y: RAIL_Y },
  ];
  const drop: readonly Pt[] = [
    { x: jx, y: RAIL_Y },
    { x: jx, y: COMP_Y - 10 },
    { x: jx - 10, y: COMP_Y },
    { x: compR, y: COMP_Y },
  ];
  const ret: readonly Pt[] = [
    { x: comp.x, y: COMP_Y },
    { x: reqCx + 10, y: COMP_Y },
    { x: reqCx, y: COMP_Y - 10 },
    { x: reqCx, y: py + PANEL_H },
  ];

  return {
    panels,
    lanes: [toD(seg1), toD(seg2), toD(drop), toD(ret)],
    pts: { seg1, seg2, seg2a, drop, ret },
    // the failed message rides through an anonymous queue slot on its way
    // into COMPENSATE
    pill: {
      x: Math.round((compR + jx - 10) / 2) - 22,
      y: COMP_Y - 7,
      w: 44,
      h: 14,
    },
    // tee via where the failure branch leaves the rail, plus two vias
    // stitched along the long compensation return run
    vias: [
      { x: jx, y: RAIL_Y },
      { x: reqCx + 112, y: COMP_Y },
      { x: reqCx + 142, y: COMP_Y },
    ],
    pins: [
      { x: reqR, y: RAIL_Y, side: "right" },
      { x: proc.x, y: RAIL_Y, side: "left" },
      { x: procR, y: RAIL_Y, side: "right" },
      { x: refd.x, y: RAIL_Y, side: "left" },
      { x: compR, y: COMP_Y, side: "right" },
      { x: comp.x, y: COMP_Y, side: "left" },
      { x: reqCx, y: py + PANEL_H, side: "bottom" },
    ],
    labels: [
      {
        x: Math.round((reqR + proc.x) / 2),
        y: RAIL_Y - 12,
        anchor: "middle",
        text: "ProcessRefund",
      },
      {
        x: Math.round((procR + refd.x) / 2),
        y: RAIL_Y - 12,
        anchor: "middle",
        text: "RefundCompleted",
      },
      {
        x: jx - 12,
        y: Math.round((RAIL_Y + COMP_Y) / 2) + 3,
        anchor: "end",
        text: "RefundFailed",
      },
    ],
    finalX: refd.x + refd.w / 2,
  };
}

export function SagaVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const [w, setW] = useState(1100);
  const lw = Math.max(w, MIN_W);
  const layout = useMemo(() => buildLayout(lw), [lw]);

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

    const E = els;
    const L = layout;

    const setChip = (i: number, state: ChipState) => {
      const s = CHIP_STYLES[state];
      // LED core, halo, and interior wash share one activity envelope
      // (canonical a*0.9 core, a*0.5 halo, a*0.07 wash)
      const a = s.ledOp > 0 ? 1 : 0;
      const title = E.get(`t${i}`);
      if (title) {
        title.style.fill = s.text;
      }
      const wash = E.get(`w${i}`);
      if (wash) {
        wash.style.fill = s.led;
        wash.style.opacity = String(a * 0.07);
      }
      const edge = E.get(`e${i}`);
      if (edge) {
        edge.style.stroke = s.edge;
        edge.style.opacity = String(s.edgeOp);
      }
      const led = E.get(`l${i}`);
      if (led) {
        led.style.fill = s.led;
        led.style.opacity = String(s.ledOp);
      }
      const halo = E.get(`h${i}`);
      if (halo) {
        halo.style.stroke = s.led;
        halo.style.opacity = String(a * 0.5);
      }
    };

    const setLabel = (i: number, fill: string) => {
      const el = E.get(`lb${i}`);
      if (el) {
        el.style.fill = fill;
      }
    };

    const setFinalTag = (on: boolean) => {
      const el = E.get("fin");
      if (el) {
        el.style.opacity = on ? "1" : "0";
      }
    };

    const hideToken = () => {
      const g = E.get("tk");
      if (g) {
        g.style.opacity = "0";
      }
    };

    const showToken = (color: string) => {
      const g = E.get("tk");
      if (!g) {
        return;
      }
      g.style.opacity = "1";
      E.get("tkcore")?.setAttribute("fill", color);
      E.get("tkglow")?.setAttribute("fill", color);
      E.get("tkt1")?.setAttribute("fill", color);
      E.get("tkt2")?.setAttribute("fill", color);
      E.get("tkin")?.setAttribute(
        "fill",
        color === AMBER ? "#fde68a" : CORAL_SOFT,
      );
    };

    // arrival ring flash state
    let ringOn = false;
    let ringAge = 0;

    const flash = (p: Pt, color: string) => {
      const el = E.get("ring");
      if (!el) {
        return;
      }
      ringOn = true;
      ringAge = 0;
      el.setAttribute("cx", String(p.x));
      el.setAttribute("cy", String(p.y));
      el.setAttribute("stroke", color);
    };

    const moveToken = (path: Path, dist: number) => {
      const head = path.at(dist);
      for (const k of ["tkcore", "tkin", "tkglow"]) {
        const el = E.get(k);
        if (el) {
          el.setAttribute("cx", head.x.toFixed(2));
          el.setAttribute("cy", head.y.toFixed(2));
        }
      }
      for (let k = 1; k <= 2; k++) {
        const el = E.get(`tkt${k}`);
        if (!el) {
          continue;
        }
        const dd = dist - 8 * k;
        if (dd <= 0) {
          el.setAttribute("opacity", "0");
        } else {
          const tp = path.at(dd);
          el.setAttribute("cx", tp.x.toFixed(2));
          el.setAttribute("cy", tp.y.toFixed(2));
          el.setAttribute("opacity", k === 1 ? "0.3" : "0.15");
        }
      }
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
      seg1: makePath(L.pts.seg1),
      seg2: makePath(L.pts.seg2),
      seg2a: makePath(L.pts.seg2a),
      drop: makePath(L.pts.drop),
      ret: makePath(L.pts.ret),
    };

    resetCycle();

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      // static final frame: happy path completed
      setChip(0, "visited");
      setChip(1, "visited");
      setChip(2, "final");
      setFinalTag(true);
      return;
    }

    const sched = (steps: readonly Step[]): Schedule => ({
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
        fadeIn: true,
        fadeOut: true,
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
          flash(endOf(L.pts.seg1), CYAN);
          hideToken();
          setLabel(0, LABEL_DIM);
        },
      },
      {
        dur: 1200,
        path: paths.seg2,
        fadeIn: true,
        fadeOut: true,
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
          flash(endOf(L.pts.seg2), GREEN);
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
        fadeIn: true,
        fadeOut: true,
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
          flash(endOf(L.pts.seg1), CYAN);
          hideToken();
          setLabel(0, LABEL_DIM);
        },
      },
      {
        dur: 390,
        path: paths.seg2a,
        // no fadeOut: the token rides straight through the rail tee
        fadeIn: true,
        enter: () => {
          setChip(1, "visited");
          showToken(CORAL);
        },
      },
      {
        dur: 780,
        path: paths.drop,
        // no fadeIn: continues in flight from the tee, absorbed at COMPENSATE
        fadeOut: true,
        enter: () => {
          showToken(AMBER);
          setLabel(2, AMBER);
        },
      },
      {
        dur: 780,
        enter: () => {
          setChip(3, "amber");
          flash(endOf(L.pts.drop), AMBER);
          hideToken();
          setLabel(2, LABEL_DIM);
        },
      },
      {
        dur: 1125,
        path: paths.ret,
        fadeIn: true,
        fadeOut: true,
        enter: () => {
          showToken(AMBER);
        },
      },
      {
        dur: 375,
        enter: () => {
          // the compensation lands back in REQUESTED, which re-activates
          setChip(0, "active");
          flash(endOf(L.pts.ret), CYAN);
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
        moveToken(st.path, f * st.path.total);
        // canonical travel envelope: fade in over 150ms at departure and
        // dissolve over the last 160ms so the pulse is absorbed exactly as
        // it reaches the box edge, never bouncing
        const g = E.get("tk");
        if (g) {
          let op = st.fadeIn ? Math.min(t / 150, 1) : 1;
          if (st.fadeOut) {
            const out = 1 - (t - (st.dur - 160)) / 160;
            op = Math.min(op, Math.min(Math.max(out, 0), 1));
          }
          g.style.opacity = op.toFixed(3);
        }
      }

      if (ringOn) {
        ringAge += dt;
        const q = ringAge / 700;
        const el = E.get("ring");
        if (el) {
          if (q >= 1) {
            ringOn = false;
            el.setAttribute("opacity", "0");
          } else {
            el.setAttribute("r", (3 + 11 * easeOutCubic(q)).toFixed(2));
            el.setAttribute("opacity", (0.5 * (1 - q)).toFixed(3));
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
    io.observe(root);
    document.addEventListener("visibilitychange", update);

    return () => {
      stop();
      io.disconnect();
      document.removeEventListener("visibilitychange", update);
    };
  }, [els, layout]);

  const set = (k: string) => (node: SVGElement | null) => {
    els.set(k, node);
  };

  const L = layout;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-6 backdrop-blur sm:h-[380px]"
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
            <filter id="saga-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="saga-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          {/* substrate: pad-dot grid */}
          <rect x={0} y={0} width={lw} height={H} fill="url(#saga-grid)" />

          {/* copper lanes: rail, failure drop, compensation return */}
          {L.lanes.map((d, i) => (
            <path
              key={`lane${i}`}
              d={d}
              fill="none"
              stroke={LANE_STROKE}
              strokeWidth={1.5}
              strokeLinejoin="round"
            />
          ))}

          {/* unlabeled queue as a plated slot on the compensation approach */}
          <rect
            x={L.pill.x}
            y={L.pill.y}
            width={L.pill.w}
            height={L.pill.h}
            rx={L.pill.h / 2}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />

          {/* vias at the rail tee and along the return run */}
          {L.vias.map((v, i) => (
            <circle
              key={`via${i}`}
              cx={v.x}
              cy={v.y}
              r={2.5}
              fill={NAVY}
              stroke={VIA_STROKE}
              strokeWidth={1.2}
            />
          ))}

          {/* pin rows where lanes dock at package edges */}
          {L.pins.map((pin, i) => (
            <PinRow key={`pin${i}`} pin={pin} />
          ))}

          {/* transition silkscreen, each printed in clear board space */}
          {L.labels.map((l, i) => (
            <text
              key={l.text}
              ref={set(`lb${i}`)}
              x={l.x}
              y={l.y}
              textAnchor={l.anchor}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.06em"
              style={{ fill: LABEL_DIM, transition: "fill .25s" }}
            >
              {l.text}
            </text>
          ))}

          {/* ── the four state panels ──────────────────────────────── */}
          {L.panels.map((p, i) => (
            <g key={p.title}>
              <rect
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={3}
                fill={SURFACE}
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
              {/* faint interior wash while the state is active */}
              <rect
                ref={set(`w${i}`)}
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={3}
                fill={CYAN}
                opacity={0}
                style={{ transition: "opacity .3s, fill .3s" }}
              />
              <rect
                ref={set(`e${i}`)}
                x={p.x}
                y={p.y}
                width={p.w}
                height={p.h}
                rx={3}
                fill="none"
                stroke={CYAN}
                strokeWidth={1.2}
                opacity={0}
                style={{ transition: "opacity .3s, stroke .3s" }}
              />
              <circle cx={p.x + 6} cy={p.y + 6} r={1.2} fill={SILK} />
              <text
                ref={set(`t${i}`)}
                x={p.x + p.w / 2}
                y={p.y + p.h / 2 + 3.5}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.14em"
                style={{ fill: IDLE_TEXT, transition: "fill .3s" }}
              >
                {p.title}
              </text>
              {/* activity LED: dim silk dot at rest, lit while the state
                  handles the message */}
              <circle
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={2}
                fill={SILK}
                opacity={0.25}
              />
              <circle
                ref={set(`h${i}`)}
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={6}
                fill="none"
                stroke={CYAN}
                strokeWidth={1.5}
                filter="url(#saga-soft)"
                opacity={0}
                style={{ transition: "opacity .3s, stroke .3s" }}
              />
              <circle
                ref={set(`l${i}`)}
                cx={p.x + p.w - 10}
                cy={p.y + 10}
                r={2}
                fill={CYAN}
                opacity={0}
                style={{ transition: "opacity .3s, fill .3s" }}
              />
            </g>
          ))}

          {/* the message in flight: soft-blur head with a short comet tail */}
          <g ref={set("tk")} opacity={0}>
            <circle ref={set("tkt2")} r={1.6} fill={CORAL} opacity={0} />
            <circle ref={set("tkt1")} r={2} fill={CORAL} opacity={0} />
            <circle
              ref={set("tkglow")}
              r={6}
              fill={CORAL}
              opacity={0.2}
              filter="url(#saga-soft)"
            />
            <circle ref={set("tkcore")} r={2.5} fill={CORAL} />
            <circle ref={set("tkin")} r={1.1} fill={CORAL_SOFT} />
          </g>

          {/* arrival ring */}
          <circle
            ref={set("ring")}
            r={0}
            fill="none"
            strokeWidth={1.5}
            opacity={0}
          />

          {/* FINAL silkscreen over the terminal state */}
          <text
            ref={set("fin")}
            x={L.finalX}
            y={L.panels[2].y - 16}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.26em"
            fill={GREEN}
            opacity={0}
            style={{ transition: "opacity .3s" }}
          >
            FINAL
          </text>
        </svg>
      </div>
    </div>
  );
}
