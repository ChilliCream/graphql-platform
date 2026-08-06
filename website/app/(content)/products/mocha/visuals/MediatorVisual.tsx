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

// Master loop. One mediator beat per loop: a coral command pulse leaves the
// SendAsync pad, rides the request lane through the middleware packages and
// docks at the HANDLER's pin row (arrival ring, activity LED, cyan wash while
// the boxed handler row processes). Only then does a green result pulse
// emerge from the handler's lower dock and ride the response lane back out,
// cooling each middleware package as it passes. No pulse dies mid-lane: the
// coral head ends in the handler dock's arrival ring, the green head ends in
// the exit ring at the left edge.
const T = 7200;
const REQ_DEP = 400; // command pulse leaves the SendAsync pad
const REQ_ARR = 2600; // command docks at the HANDLER pin row
const RES_DEP = 3600; // processing done: result departs the lower dock
const RES_ARR = 5600; // result exits at the left edge

// Below MIN_W the four packages get too cramped to read, so the board is
// laid out at MIN_W and the whole stage scales down via the SVG viewBox.
const MIN_W = 640;
const H = 252;
const LANE_X = 14; // left end of both copper lanes
const PANELS_X = 108; // gutter keeps room for the SendAsync / result labels
const MARGIN_R = 12;
const GAP = 22; // gap between packages, hosts the chevron arrowheads
const HANDLER_EXTRA = 126; // HANDLER is wider to fit its boxed handler row
const PANEL_Y = 24;
const PANEL_H = 204;
const REQ_Y = 92; // request lane, upper third of the packages
const RES_Y = 160; // response lane, lower third of the packages
const ROW_H = 32;
const ROW_Y = (REQ_Y + RES_Y) / 2 - ROW_H / 2; // handler row between the lanes

const MW_LABELS = ["TELEMETRY", "TRANSACTION", "VALIDATION"] as const;
const HANDLER_ROW = "PlaceOrderCommandHandler";

const SURFACE = "#0c1322";
const GRID_DOT = "rgba(150,166,194,0.10)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const PANEL_FILL = "rgba(139,160,188,0.03)";
const HANDLER_FILL = "rgba(22,185,228,0.04)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const CHEV_STROKE = "rgba(139,160,188,0.6)";
const HAIR = "rgba(139,160,188,0.22)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.7)";
const INK = "#a1a3af";
const HANDLER_LIT = "#a5e3f6";

interface Panel {
  readonly label: string;
  readonly x: number;
  readonly w: number;
}

interface Pin {
  readonly x: number;
  readonly y: number;
  readonly side: "left" | "right" | "top" | "bottom";
}

interface Layout {
  readonly panels: readonly Panel[];
  readonly hx: number;
  readonly hw: number;
  readonly hcx: number;
  readonly rowX: number;
  readonly rowW: number;
  readonly rowFont: number;
  readonly req: Polyline;
  readonly res: Polyline;
  readonly chevXs: readonly number[];
  readonly crossXs: readonly number[];
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
  const avail = lw - PANELS_X - MARGIN_R - 3 * GAP;
  const mwW = Math.floor((avail - HANDLER_EXTRA) / 4);
  const hw = avail - 3 * mwW;
  const panels: readonly Panel[] = MW_LABELS.map((label, i) => ({
    label,
    x: PANELS_X + i * (mwW + GAP),
    w: mwW,
  }));
  const hx = PANELS_X + 3 * (mwW + GAP);
  const rowX = hx + 10;
  const rowW = hw - 20;
  return {
    panels,
    hx,
    hw,
    hcx: hx + hw / 2,
    rowX,
    rowW,
    rowFont: Math.min(10, (rowW - 16) / (HANDLER_ROW.length * 0.635)),
    // Both lanes dock at the HANDLER's left edge: command in on the upper
    // lane, result out on the lower one.
    req: measure([
      [LANE_X, REQ_Y],
      [hx, REQ_Y],
    ]),
    res: measure([
      [hx, RES_Y],
      [LANE_X, RES_Y],
    ]),
    // One chevron on the entry segment plus one in each inter-package gap.
    chevXs: [
      (LANE_X + PANELS_X) / 2,
      ...panels.map((p) => p.x + p.w + GAP / 2),
    ],
    // Pass-through pads where the lanes cross the middleware package edges.
    crossXs: panels.flatMap((p) => [p.x, p.x + p.w]),
    // Proper pin rows only where the lanes actually dock: at the HANDLER.
    pins: [
      { x: hx, y: REQ_Y, side: "left" },
      { x: hx, y: RES_Y, side: "left" },
    ],
  };
}

export function MediatorVisual() {
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
      // The initial render is the meaningful static frame: board fully
      // drawn, HANDLER softly lit, result label printed green. Keep it.
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

    // per-package warmth, persisted across frames so attack/decay stays
    // smooth (no per-frame allocation)
    const warm = [0, 0, 0];

    const apply = (t: number, dt: number) => {
      const L = layoutRef.current;

      // command pulse: SendAsync pad → HANDLER dock, the full lane, ending
      // in the arrival ring (never mid-lane)
      let px = -1e4;
      if (t >= REQ_DEP && t < REQ_ARR) {
        const u = easeInOutCubic(ramp(t, REQ_DEP, REQ_ARR));
        px = pointAt(L.req, u)[0];
        placePulse(
          "rq",
          L.req,
          u,
          Math.min((t - REQ_DEP) / 150, 1) *
            (1 - ramp(t, REQ_ARR - 160, REQ_ARR)),
        );
      } else {
        placePulse("rq", L.req, 0, 0);
      }
      setRing("rga", ((t - REQ_ARR + T) % T) / 700, 3, 11);

      // result pulse: emerges from the handler's lower dock only after the
      // processing dwell, exits into the ring at the left edge
      let gx = 1e4;
      if (t >= RES_DEP && t < RES_ARR) {
        const u = easeInOutCubic(ramp(t, RES_DEP, RES_ARR));
        gx = pointAt(L.res, u)[0];
        placePulse(
          "rs",
          L.res,
          u,
          Math.min((t - RES_DEP) / 150, 1) *
            (1 - ramp(t, RES_ARR - 160, RES_ARR)),
        );
      } else {
        placePulse("rs", L.res, 0, 0);
      }
      setRing("rgx", ((t - RES_ARR + T) % T) / 700, 3, 11);

      // how far the command has come, how far the result has gone back out
      const reachX = t >= REQ_ARR ? L.hx : px;
      const greenX = t >= RES_ARR ? -1e4 : gx;

      // middleware packages brighten while a pulse is inside, hold half-warm
      // while their scope stays open (command passed, result still pending),
      // and cool once the result has passed back through
      for (let i = 0; i < 3; i++) {
        const x0 = L.panels[i].x;
        const x1 = x0 + L.panels[i].w;
        let target = 0;
        if (greenX <= x0 - 2) {
          target = 0;
        } else if (greenX <= x1 + 2) {
          target = 1;
        } else if (reachX >= x1 + 2) {
          target = 0.55;
        } else if (reachX >= x0 + 2) {
          target = 1;
        }
        warm[i] =
          target > warm[i]
            ? Math.min(target, warm[i] + dt / 70)
            : Math.max(target, warm[i] - dt / 450);
        setO(`pw${i}`, warm[i] * 0.07);
        setO(`pe${i}`, warm[i] * 0.55);
        setO(`pl${i}`, warm[i] * 0.9);
        setO(`ph${i}`, warm[i] * 0.5);
      }

      // HANDLER work window: the attack begins just before the command
      // docks and peaks shortly after; decay starts only once the result
      // has visibly left the lower dock, so the same lit handler emits it
      const hact =
        easeOutCubic(ramp(t, REQ_ARR - 40, REQ_ARR + 220)) *
        (1 - easeInOutCubic(ramp(t, RES_DEP + 150, RES_DEP + 800)));
      setO("hcw", hact * 0.07);
      setO("hce", hact * 0.55);
      setO("plc", hact * 0.9);
      setO("phc", hact * 0.5);
      setO("hrow", hact * 0.9);

      // the result label prints green as the pulse exits, then fades
      setO(
        "rtag",
        ramp(t, RES_ARR - 100, RES_ARR + 150) * (1 - ramp(t, 6500, 7100)),
      );
    };

    // paint the phase-0 frame so the lit static defaults never flash
    apply(0, 0);

    let t = 0;
    let last = 0;

    const step = (now: number) => {
      const dt = Math.min(now - last, 50);
      last = now;
      t = (t + dt) % T;
      apply(t, dt);
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
        filter="url(#mediator-soft)"
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
              id="mediator-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="mediator-grid"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          {/* substrate: the same pad-dot grid as the hero board */}
          <rect x={0} y={0} width={lw} height={H} fill="url(#mediator-grid)" />

          {/* ── copper lanes: command in on top, result out below ──── */}
          <path
            d={`M${LANE_X} ${REQ_Y} H${L.hx}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={`M${LANE_X} ${RES_Y} H${L.hx}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />

          {/* vias where the lanes meet the left edge */}
          {[REQ_Y, RES_Y].map((vy) => (
            <circle
              key={`via-${vy}`}
              cx={LANE_X}
              cy={vy}
              r={2.5}
              fill={NAVY}
              stroke={VIA_STROKE}
              strokeWidth={1.2}
            />
          ))}

          {/* chevron arrowheads: right on the request lane, left on the
              response lane */}
          {L.chevXs.map((cx) => (
            <g
              key={`chev-${cx}`}
              fill="none"
              stroke={CHEV_STROKE}
              strokeWidth={1.3}
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path
                d={`M ${cx - 3} ${REQ_Y - 3.4} L ${cx + 2.8} ${REQ_Y} L ${cx - 3} ${REQ_Y + 3.4}`}
              />
              <path
                d={`M ${cx + 3} ${RES_Y - 3.4} L ${cx - 2.8} ${RES_Y} L ${cx + 3} ${RES_Y + 3.4}`}
              />
            </g>
          ))}

          {/* pass-through pads where the lanes cross middleware edges */}
          {L.crossXs.map((ex) =>
            [REQ_Y, RES_Y].map((py) => (
              <rect
                key={`pad-${ex}-${py}`}
                x={ex - 1}
                y={py - 1.75}
                width={2}
                height={3.5}
                fill={PAD_FILL}
              />
            )),
          )}

          {/* pin rows where the lanes dock at the HANDLER's edge */}
          {L.pins.map((pin, i) => (
            <PinRow key={`pin${i}`} pin={pin} />
          ))}

          {/* ── the three middleware packages ──────────────────────── */}
          {L.panels.map((p, i) => (
            <g key={p.label}>
              <rect
                x={p.x}
                y={PANEL_Y}
                width={p.w}
                height={PANEL_H}
                rx={3}
                fill={PANEL_FILL}
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
              <circle cx={p.x + 6} cy={PANEL_Y + 6} r={1.2} fill={SILK} />
              {/* faint PCB furniture in the lower-right: pad pair on even
                  packages, hatch patch on odd ones */}
              {i % 2 === 0 ? (
                <g>
                  <rect
                    x={p.x + p.w - 30}
                    y={PANEL_Y + PANEL_H - 17}
                    width={8}
                    height={3}
                    fill="rgba(154,172,200,0.12)"
                  />
                  <rect
                    x={p.x + p.w - 30}
                    y={PANEL_Y + PANEL_H - 11}
                    width={8}
                    height={3}
                    fill="rgba(154,172,200,0.12)"
                  />
                </g>
              ) : (
                <path
                  d={`M${p.x + p.w - 30} ${PANEL_Y + PANEL_H - 8} l7 -7 M${p.x + p.w - 24} ${PANEL_Y + PANEL_H - 8} l7 -7 M${p.x + p.w - 18} ${PANEL_Y + PANEL_H - 8} l7 -7`}
                  stroke="rgba(154,172,200,0.11)"
                  strokeWidth={1}
                  fill="none"
                />
              )}
              <rect
                ref={set(`pw${i}`)}
                x={p.x}
                y={PANEL_Y}
                width={p.w}
                height={PANEL_H}
                rx={3}
                fill={CORAL}
                opacity={0}
              />
              <rect
                ref={set(`pe${i}`)}
                x={p.x}
                y={PANEL_Y}
                width={p.w}
                height={PANEL_H}
                rx={3}
                fill="none"
                stroke={CORAL}
                strokeWidth={1.2}
                opacity={0}
              />
              {/* silkscreen title between the two lane passes */}
              <text
                x={p.x + p.w / 2}
                y={(REQ_Y + RES_Y) / 2 + 3.5}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.04em"
                fill={SILK}
              >
                {p.label}
              </text>
              {/* activity LED: dim silk dot at rest, coral while the
                  package handles a message (same window as the washes) */}
              <circle
                cx={p.x + p.w - 10}
                cy={PANEL_Y + 10}
                r={2}
                fill={SILK}
                opacity={0.25}
              />
              <circle
                ref={set(`ph${i}`)}
                cx={p.x + p.w - 10}
                cy={PANEL_Y + 10}
                r={6}
                fill="none"
                stroke={CORAL}
                strokeWidth={1.5}
                filter="url(#mediator-soft)"
                opacity={0}
              />
              <circle
                ref={set(`pl${i}`)}
                cx={p.x + p.w - 10}
                cy={PANEL_Y + 10}
                r={2}
                fill={CORAL}
                opacity={0}
              />
            </g>
          ))}

          {/* ── the HANDLER package, where both lanes dock ─────────── */}
          <g>
            <rect
              x={L.hx}
              y={PANEL_Y}
              width={L.hw}
              height={PANEL_H}
              rx={3}
              fill={HANDLER_FILL}
              stroke={PANEL_STROKE}
              strokeWidth={1}
            />
            <circle cx={L.hx + 6} cy={PANEL_Y + 6} r={1.2} fill={SILK} />
            <path
              d={`M${L.hx + L.hw - 30} ${PANEL_Y + PANEL_H - 8} l7 -7 M${L.hx + L.hw - 24} ${PANEL_Y + PANEL_H - 8} l7 -7 M${L.hx + L.hw - 18} ${PANEL_Y + PANEL_H - 8} l7 -7`}
              stroke="rgba(154,172,200,0.11)"
              strokeWidth={1}
              fill="none"
            />
            <rect
              ref={set("hcw")}
              x={L.hx}
              y={PANEL_Y}
              width={L.hw}
              height={PANEL_H}
              rx={3}
              fill={CYAN}
              opacity={0}
            />
            <rect
              ref={set("hce")}
              x={L.hx}
              y={PANEL_Y}
              width={L.hw}
              height={PANEL_H}
              rx={3}
              fill="none"
              stroke={CYAN}
              strokeWidth={1.2}
              opacity={0.35}
            />
            <text
              x={L.hcx}
              y={PANEL_Y + 15}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.2em"
              fill={CYAN}
              opacity={0.75}
            >
              HANDLER
            </text>
            {/* the boxed handler row between the two docks */}
            <rect
              x={L.rowX}
              y={ROW_Y}
              width={L.rowW}
              height={ROW_H}
              rx={5}
              fill={SURFACE}
              stroke={HAIR}
              strokeWidth={1}
            />
            <rect
              x={L.rowX}
              y={ROW_Y + 5}
              width={3}
              height={ROW_H - 10}
              rx={1.5}
              fill={CYAN}
            />
            <text
              x={L.rowX + 13}
              y={ROW_Y + 20}
              fontFamily={MONO_FONT}
              fontSize={L.rowFont}
              fill={INK}
            >
              {HANDLER_ROW}
            </text>
            {/* lit overlay: brightens while the handler processes */}
            <g ref={set("hrow")} opacity={0.35}>
              <rect
                x={L.rowX}
                y={ROW_Y}
                width={L.rowW}
                height={ROW_H}
                rx={5}
                fill={CYAN}
                opacity={0.07}
              />
              <rect
                x={L.rowX}
                y={ROW_Y}
                width={L.rowW}
                height={ROW_H}
                rx={5}
                fill="none"
                stroke={CYAN}
                strokeWidth={1.2}
                opacity={0.8}
              />
              <text
                x={L.rowX + 13}
                y={ROW_Y + 20}
                fontFamily={MONO_FONT}
                fontSize={L.rowFont}
                fill={HANDLER_LIT}
              >
                {HANDLER_ROW}
              </text>
              <text
                x={L.hcx}
                y={PANEL_Y + 15}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.2em"
                fill={HANDLER_LIT}
              >
                HANDLER
              </text>
            </g>
            {/* activity LED: dim silk dot at rest, cyan for as long as
                the handler works */}
            <circle
              cx={L.hx + L.hw - 10}
              cy={PANEL_Y + 10}
              r={2}
              fill={SILK}
              opacity={0.25}
            />
            <circle
              ref={set("phc")}
              cx={L.hx + L.hw - 10}
              cy={PANEL_Y + 10}
              r={6}
              fill="none"
              stroke={CYAN}
              strokeWidth={1.5}
              filter="url(#mediator-soft)"
              opacity={0}
            />
            <circle
              ref={set("plc")}
              cx={L.hx + L.hw - 10}
              cy={PANEL_Y + 10}
              r={2}
              fill={CYAN}
              opacity={0}
            />
          </g>

          {/* ── lane labels at the left edge ───────────────────────── */}
          <text
            x={10}
            y={REQ_Y - 11}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.02em"
            fill={SILK_SOFT}
          >
            SendAsync
          </text>
          <text
            x={10}
            y={RES_Y + 20}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.02em"
            fill={SILK_SOFT}
          >
            PlaceOrderResult
          </text>
          <text
            ref={set("rtag")}
            x={10}
            y={RES_Y + 20}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.02em"
            fill={GREEN}
            opacity={0.45}
          >
            PlaceOrderResult
          </text>

          {/* arrival ring at the handler dock, exit ring at the edge */}
          <circle
            ref={set("rga")}
            cx={L.hx}
            cy={REQ_Y}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("rgx")}
            cx={LANE_X}
            cy={RES_Y}
            r={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.5}
            opacity={0}
          />

          {/* ── pulses in flight, the topmost layer ────────────────── */}
          {pulseGlyph("rq", CORAL, CORAL_SOFT)}
          {pulseGlyph("rs", GREEN, "#a7f3d0")}
        </svg>
      </div>
    </div>
  );
}
