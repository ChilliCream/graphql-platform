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
  SLATE,
} from "../palette";

// Master loop. The story is the wait: the message leaves ORDERS immediately
// but parks in the SCHEDULED slot while the clock arc fills and the countdown
// runs to zero; only then does it move on to the queue and the handler.
const T = 12000;
const H = 190;
// Below this width the panels, the scheduled slot and the queue slot get too
// cramped to read, so we lay out at MIN_W and scale down via the viewBox.
const MIN_W = 560;

const INK = "#a1a3af";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.7)";
const GRID_DOT = "rgba(150,166,194,0.10)";

const M = 8;
const PW1 = 150; // ORDERS SERVICE panel width
const PW2 = 200; // NOTIFICATIONS SERVICE panel width
const PANEL_Y = 46;
const PANEL_H = 104;
const ROW_TOP = 85;
const ROW_H = 30;
const LANE_Y = 100;
const SLOT_W = 66;
const SLOT_H = 18;
const QSLOT_W = 56;
const QSLOT_H = 14;
const CLOCK_R = 10;
// The countdown starts at this many "minutes" and runs to zero over the hold.
const CD_START = 30;

interface Layout {
  readonly px2: number;
  readonly rowX1: number;
  readonly rowX2: number;
  readonly slotL: number;
  readonly slotR: number;
  readonly entryS: number;
  readonly frontS: number;
  readonly clockX: number;
  readonly qL: number;
  readonly qR: number;
  readonly entryQ: number;
  readonly frontQ: number;
  readonly pubX0: number;
  readonly dlvEnd: number;
  readonly cdX: number;
}

function buildLayout(lw: number): Layout {
  const x1R = M + PW1;
  const px2 = lw - M - PW2;
  // Whatever is left between the panels splits evenly around the scheduled
  // slot, the clock gate on the lane, and the queue slot.
  const g = (px2 - x1R - SLOT_W - 2 * CLOCK_R - QSLOT_W) / 4;
  const slotL = x1R + g;
  const slotR = slotL + SLOT_W;
  const clockX = slotR + g + CLOCK_R;
  const qL = clockX + CLOCK_R + g;
  const rowX1 = M + 12;
  const rowX2 = px2 + 12;
  return {
    px2,
    rowX1,
    rowX2,
    slotL,
    slotR,
    entryS: slotL + 8,
    frontS: slotR - 9,
    clockX,
    qL,
    qR: qL + QSLOT_W,
    entryQ: qL + 7,
    frontQ: qL + QSLOT_W - 9,
    // Pulses start inside the message row and end inside the handler row, so
    // every handoff is seamless.
    pubX0: rowX1 + 14,
    dlvEnd: rowX2 + 14,
    cdX: (slotL + clockX + CLOCK_R) / 2,
  };
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

export function SchedulingVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const [w, setW] = useState(660);
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
      // The initial render is the meaningful static frame: the dot parked in
      // the scheduled slot, the clock arc ~60% filled, the countdown at 12.
      return;
    }

    const E = els;
    let raf = 0;
    let running = false;
    let inView = false;
    let numCache = -1;
    let arcCache = -1;

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
      x0: number,
      x1: number,
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
      const d = clamp01(u) * (x1 - x0);
      const x = x0 + d;
      setDot(p + "core", x, LANE_Y, coreR);
      setDot(p + "in", x, LANE_Y, coreR * 0.45);
      setDot(p + "glow", x, LANE_Y, Math.max(0.6, coreR * 2.4));
      for (let k = 1; k <= 3; k++) {
        const dk = d - 7 * k;
        const el = E.get(p + "t" + k);
        if (el) {
          if (dk <= 2) {
            el.setAttribute("opacity", "0");
          } else {
            el.setAttribute("cx", (x0 + dk).toFixed(2));
            el.setAttribute("cy", LANE_Y.toFixed(2));
            el.setAttribute("opacity", (0.45 - 0.12 * k).toFixed(2));
          }
        }
      }
    };

    const hidePulse = (p: string) => setO(p, 0);

    const apply = (t: number) => {
      const L = layoutRef.current;

      // 1 · schedule beat: the API tag pops and the message row echoes.
      const tp = easeOutCubic(ramp(t, 450, 900));
      setPop("schedTag", tp * 0.9 * (1 - ramp(t, 2600, 3200)), tp);
      const e1 = t < 700 ? ramp(t, 450, 700) : 1 - ramp(t, 700, 1600);
      setO("rowEcho", Math.max(0, e1) * 0.7);

      // 2 · coral pulse: ORDERS row -> scheduled slot.
      if (t >= 600 && t < 2100) {
        const u = easeInOutCubic(ramp(t, 600, 2100));
        placePulse(
          "pub",
          L.pubX0,
          L.entryS,
          u,
          Math.min((t - 600) / 150, 1),
          2.5,
        );
      } else {
        hidePulse("pub");
      }
      setRing("ringS", (t - 2100) / 700, 2, 9);

      // 3 · the dot parks and slides to the front of the slot.
      const sdot = E.get("sdot");
      if (sdot) {
        if (t >= 2100 && t < 7400) {
          const u = easeOutCubic(ramp(t, 2100, 2550));
          setDot("sdot", L.entryS + (L.frontS - L.entryS) * u, LANE_Y);
          sdot.setAttribute("opacity", "0.95");
        } else {
          sdot.setAttribute("opacity", "0");
        }
      }

      // 4 · the hold: countdown 30 -> 0 while the clock arc fills.
      const cp = ramp(t, 2700, 7000);
      const av = Math.round(cp * 200) / 2;
      if (av !== arcCache) {
        arcCache = av;
        const arc = E.get("cdArc");
        if (arc) {
          arc.setAttribute("stroke-dasharray", `${av} 100`);
        }
      }
      // The spent arc dissolves after the release so the clock reads idle
      // again for the rest of the loop.
      setO("cdArc", 1 - ramp(t, 7950, 8650));
      const n = Math.ceil(CD_START * (1 - cp));
      if (n !== numCache) {
        numCache = n;
        const el = E.get("cdNum");
        if (el) {
          el.textContent = String(n);
        }
      }
      setO("cdown", ramp(t, 2300, 2700) * (1 - ramp(t, 7200, 7700)) * 0.95);

      // 5 · zero: subtle green release flash on the clock and the slot.
      const gf = t < 7400 ? ramp(t, 7200, 7400) : 1 - ramp(t, 7400, 7950);
      setO("arcFlash", Math.max(0, gf) * 0.9);
      setO("slotFlash", Math.max(0, gf) * 0.55);
      setRing("ringG", (t - 7200) / 700, CLOCK_R, 12);

      // 6 · released: slot front -> under the clock gate -> queue entry.
      if (t >= 7400 && t < 8850) {
        const u = easeInOutCubic(ramp(t, 7400, 8850));
        placePulse(
          "rel",
          L.frontS,
          L.entryQ,
          u,
          Math.min((t - 7400) / 150, 1),
          2.5,
        );
      } else {
        hidePulse("rel");
      }
      setRing("ringQ", (t - 8850) / 700, 2, 8);

      // 7 · brief queue beat before delivery.
      const qdot = E.get("qdot");
      if (qdot) {
        if (t >= 8850 && t < 9600) {
          const u = easeOutCubic(ramp(t, 8850, 9300));
          setDot("qdot", L.entryQ + (L.frontQ - L.entryQ) * u, LANE_Y);
          qdot.setAttribute("opacity", "0.95");
        } else {
          qdot.setAttribute("opacity", "0");
        }
      }

      // 8 · delivery: queue -> SendWelcomeEmailHandler, then the row flashes.
      if (t >= 9600 && t < 11050) {
        const u = easeInOutCubic(ramp(t, 9600, 11050));
        const r = 2.5 * (1 - ramp(t, 10930, 11050));
        placePulse(
          "dlv",
          L.frontQ,
          L.dlvEnd,
          u,
          Math.min((t - 9600) / 150, 1),
          r,
        );
      } else {
        hidePulse("dlv");
      }
      setRing("ring2", (t - 11050) / 700, 3, 12);
      const w2 = t < 11200 ? ramp(t, 11050, 11200) : 1 - ramp(t, 11500, 11950);
      setO("h2echo", Math.max(0, w2) * 0.7);
      setO("h2lit", Math.max(0, w2) * 0.9);
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
      <circle ref={set(p + "t3")} r={1.4} fill={CORAL} opacity={0} />
      <circle ref={set(p + "t2")} r={1.7} fill={CORAL} opacity={0} />
      <circle ref={set(p + "t1")} r={2} fill={CORAL} opacity={0} />
      <circle
        ref={set(p + "glow")}
        r={6}
        fill={CORAL}
        opacity={0.22}
        filter="url(#sched-soft)"
      />
      <circle ref={set(p + "core")} r={2.5} fill={CORAL} />
      <circle ref={set(p + "in")} r={1.1} fill={CORAL_SOFT} />
    </g>
  );

  const L = layout;
  const x1R = M + PW1;

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
              id="sched-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern
              id="sched-pads"
              width={28}
              height={28}
              patternUnits="userSpaceOnUse"
            >
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          {/* substrate: faint pad-dot grid behind everything */}
          <rect x={0} y={0} width={lw} height={H} fill="url(#sched-pads)" />

          {/* ── copper lanes: panel -> slot -> clock gate -> queue -> panel ── */}
          <path
            d={`M${x1R} ${LANE_Y} H${L.slotL}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={`M${L.slotR} ${LANE_Y} H${L.qL}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={`M${L.qR} ${LANE_Y} H${L.px2}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />

          {/* vias at the slot mouths and exits */}
          {(
            [
              ["v-slot-l", L.slotL],
              ["v-slot-r", L.slotR],
              ["v-q-l", L.qL],
              ["v-q-r", L.qR],
            ] as const
          ).map(([k, x]) => (
            <circle
              key={k}
              cx={x}
              cy={LANE_Y}
              r={2.5}
              fill={NAVY}
              stroke={VIA_STROKE}
              strokeWidth={1}
            />
          ))}

          {/* pin-row docks at the two panel edges */}
          {(
            [
              ["pins-orders", x1R, 1],
              ["pins-notif", L.px2, -1],
            ] as const
          ).map(([k, x, side]) => (
            <g key={k}>
              {[-1, 0, 1].map((i) => (
                <rect
                  key={i}
                  x={side === 1 ? x : x - 3.5}
                  y={LANE_Y + 5 * i - 1}
                  width={3.5}
                  height={2}
                  fill={PAD_FILL}
                />
              ))}
            </g>
          ))}

          {/* ── SCHEDULED holding slot ─────────────────────────────── */}
          <rect
            x={L.slotL}
            y={LANE_Y - SLOT_H / 2}
            width={SLOT_W}
            height={SLOT_H}
            rx={SLOT_H / 2}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
            strokeDasharray="4 3"
          />
          <rect
            ref={set("slotFlash")}
            x={L.slotL}
            y={LANE_Y - SLOT_H / 2}
            width={SLOT_W}
            height={SLOT_H}
            rx={SLOT_H / 2}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.2}
            strokeDasharray="4 3"
            opacity={0}
          />
          <text
            x={(L.slotL + L.slotR) / 2}
            y={LANE_Y + SLOT_H / 2 + 15}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={11}
            letterSpacing="0.16em"
            fill={SILK_SOFT}
          >
            SCHEDULED
          </text>

          {/* countdown above the slot; the static frame shows 12 min left */}
          <g ref={set("cdown")} opacity={0.95}>
            <text
              x={L.cdX}
              y={76}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10.5}
            >
              <tspan fill={SILK_SOFT}>{"delivers in "}</tspan>
              <tspan ref={set("cdNum")} fill={AMBER}>
                12
              </tspan>
              <tspan fill={SILK_SOFT}>{" min"}</tspan>
            </text>
          </g>

          {/* ── queue slot feeding NOTIFICATIONS ───────────────────── */}
          <rect
            x={L.qL}
            y={LANE_Y - QSLOT_H / 2}
            width={QSLOT_W}
            height={QSLOT_H}
            rx={QSLOT_H / 2}
            fill={NAVY}
            stroke={VIA_STROKE}
            strokeWidth={1}
          />
          <text
            x={(L.qL + L.qR) / 2}
            y={LANE_Y + SLOT_H / 2 + 15}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.08em"
            fill={SILK_SOFT}
          >
            welcome-emails
          </text>

          {/* ── ORDERS SERVICE panel ───────────────────────────────── */}
          <rect
            x={M}
            y={PANEL_Y}
            width={PW1}
            height={PANEL_H}
            rx={3}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <circle cx={M + 7} cy={PANEL_Y + 7} r={1.2} fill={SILK} />
          <text
            x={M + 12}
            y={PANEL_Y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill={SILK}
          >
            ORDERS SERVICE
          </text>
          <rect
            x={L.rowX1}
            y={ROW_TOP}
            width={PW1 - 24}
            height={ROW_H}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX1}
            y={ROW_TOP}
            width={PW1 - 24}
            height={ROW_H}
            rx={5}
            fill={CORAL}
            opacity={0.06}
          />
          <rect
            x={L.rowX1}
            y={ROW_TOP + 5}
            width={3}
            height={ROW_H - 10}
            rx={1.5}
            fill={CORAL}
          />
          <text
            x={L.rowX1 + 13}
            y={ROW_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={CORAL_SOFT}
          >
            WelcomeEmail
          </text>
          <rect
            ref={set("rowEcho")}
            x={L.rowX1}
            y={ROW_TOP}
            width={PW1 - 24}
            height={ROW_H}
            rx={5}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />
          {/* schedule beat tag near the departure */}
          <text
            ref={set("schedTag")}
            x={M + PW1 / 2}
            y={PANEL_Y + PANEL_H + 16}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9.5}
            letterSpacing="0.04em"
            fill={SILK_SOFT}
            opacity={0.55}
          >
            SchedulePublishAsync
          </text>

          {/* ── NOTIFICATIONS SERVICE panel ────────────────────────── */}
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
          <circle cx={L.px2 + 7} cy={PANEL_Y + 7} r={1.2} fill={SILK} />
          <text
            x={L.px2 + 12}
            y={PANEL_Y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.18em"
            fill={SILK}
          >
            NOTIFICATIONS SERVICE
          </text>
          <rect
            x={L.rowX2}
            y={ROW_TOP}
            width={PW2 - 24}
            height={ROW_H}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX2}
            y={ROW_TOP}
            width={PW2 - 24}
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
            y={ROW_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={INK}
          >
            SendWelcomeEmailHandler
          </text>
          <text
            ref={set("h2lit")}
            x={L.rowX2 + 13}
            y={ROW_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={10}
            fill={CORAL_SOFT}
            opacity={0}
          >
            SendWelcomeEmailHandler
          </text>
          <rect
            ref={set("h2echo")}
            x={L.rowX2}
            y={ROW_TOP}
            width={PW2 - 24}
            height={ROW_H}
            rx={5}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />

          {/* ── pulses ─────────────────────────────────────────────── */}
          {pulseGlyph("pub")}
          {pulseGlyph("rel")}
          {pulseGlyph("dlv")}

          {/* parked message; the reduced-motion frame shows it waiting */}
          <circle
            ref={set("sdot")}
            cx={L.frontS}
            cy={LANE_Y}
            r={2.5}
            fill={CORAL}
            opacity={0.95}
          />
          <circle
            ref={set("qdot")}
            cx={L.frontQ}
            cy={LANE_Y}
            r={2.5}
            fill={CORAL}
            opacity={0}
          />

          {/* arrival rings */}
          <circle
            ref={set("ringS")}
            cx={L.slotL}
            cy={LANE_Y}
            r={2}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringQ")}
            cx={L.qL}
            cy={LANE_Y}
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

          {/* ── clock gate, drawn last so the release dips under it ── */}
          <circle
            cx={L.clockX}
            cy={LANE_Y}
            r={CLOCK_R}
            fill={SURFACE}
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <line
            x1={L.clockX}
            y1={LANE_Y - CLOCK_R + 0.5}
            x2={L.clockX}
            y2={LANE_Y - CLOCK_R + 3.5}
            stroke={SLATE}
            strokeWidth={1}
            opacity={0.5}
          />
          <circle
            cx={L.clockX}
            cy={LANE_Y}
            r={1.2}
            fill={SLATE}
            opacity={0.6}
          />
          {/* the arc fills as the hold elapses; static frame shows ~60% */}
          <circle
            ref={set("cdArc")}
            cx={L.clockX}
            cy={LANE_Y}
            r={CLOCK_R}
            pathLength={100}
            fill="none"
            stroke={AMBER}
            strokeWidth={2}
            strokeLinecap="round"
            strokeDasharray="60 100"
            transform={`rotate(-90 ${L.clockX} ${LANE_Y})`}
          />
          <circle
            ref={set("arcFlash")}
            cx={L.clockX}
            cy={LANE_Y}
            r={CLOCK_R}
            fill="none"
            stroke={GREEN}
            strokeWidth={2}
            opacity={0}
          />
          <circle
            ref={set("ringG")}
            cx={L.clockX}
            cy={LANE_Y}
            r={CLOCK_R}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.5}
            opacity={0}
          />
        </svg>
      </div>
    </div>
  );
}
