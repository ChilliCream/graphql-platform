"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  MONO_FONT,
  NAVY,
  SLATE,
} from "../palette";

// Two identical 7.5s fill -> flush cycles; the second one also flashes the
// AMBER "or after 1s" timeout tag at flush time, so the loop is 15s total.
const T = 15000;
const CYCLE = 7500;
const H = 160;
// Below this width the accumulator slot and the service panel get too cramped
// to read, so we lay out at MIN_W and scale the whole stage down via viewBox.
const MIN_W = 520;

const DOTS = 12;
const EMIT0 = 150; // first dot leaves the left edge
const STEP = 240; // steady stream: one event every ~240ms
const FLIGHT = 1450; // left edge -> parked position in the slot
const FLUSH = 4450; // counter reads 100 at ~4240; flush fires shortly after
const COMPRESS = 450; // parked dots compress into one batch block
const TRAVEL = 1500; // block travels into the handler row
const ARRIVE = FLUSH + COMPRESS + TRAVEL;

const INK = "#a1a3af";
const DIM = "#62748e";
const ACCENT = "#5eead4";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(139,160,188,0.25)";
const LANE_STROKE = "rgba(139,160,188,0.4)";

const MID_Y = 78;
const SLOT_W = 140;
const SLOT_H = 18;
const Q_GAP = 11;
const PANEL_W = 236;
const PANEL_TOP = MID_Y - 45;
const PANEL_H = 72;
const ROW_H = 34;
const ROW_Y = MID_Y - ROW_H / 2;
const BLOCK_W = 18;
const BLOCK_H = 11;

interface Seg {
  readonly t: string;
  readonly f: string;
}

const ROW_SEGS: readonly Seg[] = [
  { t: "IBatchEventHandler", f: ACCENT },
  { t: "<OrderPlaced>", f: INK },
];

// Animation pacing: only 12 dot slots are drawn, but the counter climbs about
// +8 per landed dot (round(k * 100 / 12)), so it reads 100 on the 12th
// arrival. The batch fills far faster than we could legibly draw 100
// individual dots.
const COUNT_AT = Array.from({ length: DOTS + 1 }, (_, k) =>
  Math.round((k * 100) / DOTS),
);

// Reduced-motion static frame: a half-full slot at "48 / 100" (6 dots at +8
// per dot) next to the idle billing-service panel.
const STATIC_FILL = 6;

interface Layout {
  readonly slotL: number;
  readonly slotR: number;
  readonly front: number;
  readonly panelX: number;
  readonly rowX: number;
  readonly rowW: number;
  readonly blockEnd: number;
}

function buildLayout(lw: number): Layout {
  const panelX = lw - PANEL_W - 6;
  const slotR = panelX - 46;
  const rowX = panelX + 9;
  return {
    slotL: slotR - SLOT_W,
    slotR,
    front: slotR - 9,
    panelX,
    rowX,
    rowW: PANEL_W - 18,
    blockEnd: rowX + 24,
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

export function BatchVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const [w, setW] = useState(480);
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
    let cntCache = -1;
    let atagCache = -1;

    const setO = (k: string, v: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("opacity", v.toFixed(3));
      }
    };

    const apply = (t: number) => {
      const L = layoutRef.current;
      const cyc = t >= CYCLE ? 1 : 0;
      const ct = t - cyc * CYCLE;

      // stream dots in from the left edge, park them side by side in the slot
      let landed = 0;
      for (let j = 0; j < DOTS; j++) {
        const e = EMIT0 + j * STEP;
        const a = e + FLIGHT;
        if (ct >= a) {
          landed += 1;
        }
        const el = E.get(`d${j}`);
        if (!el) {
          continue;
        }
        const target = L.front - Q_GAP * j;
        let op = 0;
        let x = 0;
        if (ct >= e && ct < FLUSH + COMPRESS) {
          if (ct < a) {
            const u = (ct - e) / FLIGHT;
            x = -4 + (target + 4) * u;
            op = Math.min((ct - e) / 120, 1) * 0.95;
          } else if (ct < FLUSH) {
            x = target;
            op = 0.95;
          } else {
            // flush: dots slide to the front and dissolve into the block
            const u = easeInOutCubic(ramp(ct, FLUSH, FLUSH + COMPRESS));
            x = target + (L.front - target) * u;
            op = 0.95 * (1 - ramp(ct, FLUSH + 190, FLUSH + COMPRESS - 65));
          }
        }
        if (op <= 0.01) {
          el.setAttribute("opacity", "0");
        } else {
          el.setAttribute("opacity", op.toFixed(3));
          el.setAttribute("cx", x.toFixed(2));
        }
      }

      // counter ticks per landed dot, resets when the batch leaves the slot
      const count = ct >= FLUSH + COMPRESS ? 0 : COUNT_AT[landed];
      if (count !== cntCache) {
        cntCache = count;
        const el = E.get("cnt");
        if (el) {
          el.textContent = String(count);
        }
      }

      // batch block: compress in place, then travel into the handler row
      let bop = 0;
      let bx = 0;
      let bw = BLOCK_W;
      let bh = BLOCK_H;
      if (ct >= FLUSH && ct < FLUSH + COMPRESS) {
        const u = easeInOutCubic(ramp(ct, FLUSH, FLUSH + COMPRESS));
        const spanL = L.front - Q_GAP * (DOTS - 1) - 6;
        const right = L.slotR - 2;
        bx = spanL + (right - BLOCK_W - spanL) * u;
        bw = right - bx;
        bh = 7 + 4 * u;
        bop = ramp(ct, FLUSH + 65, FLUSH + 260);
      } else if (ct >= FLUSH + COMPRESS && ct < ARRIVE) {
        const u = easeInOutCubic(ramp(ct, FLUSH + COMPRESS, ARRIVE));
        const startC = L.slotR - 2 - BLOCK_W / 2;
        const c = startC + (L.blockEnd - startC) * u;
        bx = c - BLOCK_W / 2;
        bop = 1 - ramp(ct, FLUSH + COMPRESS + TRAVEL * 0.76, ARRIVE);
      }
      const blk = E.get("blk");
      if (blk) {
        if (bop <= 0.01) {
          blk.setAttribute("opacity", "0");
        } else {
          blk.setAttribute("opacity", bop.toFixed(3));
          for (const k of ["blkc", "blkg"]) {
            const r = E.get(k);
            if (r) {
              r.setAttribute("x", bx.toFixed(2));
              r.setAttribute("width", Math.max(0, bw).toFixed(2));
              r.setAttribute("y", (MID_Y - bh / 2).toFixed(2));
              r.setAttribute("height", bh.toFixed(2));
            }
          }
        }
      }

      // arrival ring at the panel border, decaying over ~0.9s
      const rs = (ct - ARRIVE) / 900;
      const ring = E.get("ring");
      if (ring) {
        if (rs < 0 || rs >= 1) {
          ring.setAttribute("opacity", "0");
        } else {
          ring.setAttribute("r", (3 + 13 * easeOutCubic(rs)).toFixed(2));
          ring.setAttribute("opacity", (0.55 * (1 - rs)).toFixed(3));
        }
      }

      // handler row flash + HandleAsync tag under the panel
      const f =
        ramp(ct, ARRIVE, ARRIVE + 150) *
        (1 - easeInOutCubic(ramp(ct, ARRIVE + 200, ARRIVE + 950)));
      setO("hEcho", f * 0.75);
      const hop =
        ramp(ct, ARRIVE, ARRIVE + 220) *
        (1 - ramp(ct, ARRIVE + 800, ARRIVE + 1050));
      setO("htag", hop * 0.95);

      // "or after 1s" timeout tag blinks AMBER at flush on alternate cycles
      let ast = 0;
      if (cyc === 1 && ct >= FLUSH - 100 && ct < FLUSH + 740) {
        ast = Math.floor((ct - (FLUSH - 100)) / 180) % 2 === 0 ? 1 : 2;
      }
      if (ast !== atagCache) {
        atagCache = ast;
        const el = E.get("atag");
        if (el) {
          el.setAttribute("fill", ast === 0 ? SLATE : AMBER);
          el.setAttribute(
            "opacity",
            ast === 0 ? "0.35" : ast === 1 ? "0.95" : "0.4",
          );
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

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[320px]"
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
              id="batch-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
          </defs>

          {/* copper lanes: left edge -> slot, slot -> service panel */}
          <path
            d={`M0 ${MID_Y} H${L.slotL}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          <path
            d={`M${L.slotR} ${MID_Y} H${L.panelX}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />

          {/* vias at the slot mouth, slot exit and panel dock */}
          {[L.slotL, L.slotR, L.panelX].map((vx) => (
            <circle
              key={vx}
              cx={vx}
              cy={MID_Y}
              r={2}
              fill={NAVY}
              stroke="rgba(139,160,188,0.5)"
              strokeWidth={1.2}
            />
          ))}

          {/* accumulator slot */}
          <rect
            x={L.slotL}
            y={MID_Y - SLOT_H / 2}
            width={SLOT_W}
            height={SLOT_H}
            rx={SLOT_H / 2}
            fill="rgba(139,160,188,0.05)"
            stroke="rgba(139,160,188,0.3)"
            strokeWidth={1}
          />
          <text
            x={L.slotL + SLOT_W / 2}
            y={MID_Y + 26}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.1em"
            fill={SLATE}
            fillOpacity={0.45}
          >
            BATCH ACCUMULATOR
          </text>

          {/* fill counter above the slot */}
          <text x={L.slotL} y={MID_Y - 20} fontFamily={MONO_FONT} fontSize={10}>
            <tspan ref={set("cnt")} fill={CORAL_SOFT}>
              48
            </tspan>
            <tspan fill={SLATE} fillOpacity={0.55}>
              {" / 100"}
            </tspan>
          </text>

          {/* timeout tag: flashes AMBER at flush on alternate cycles */}
          <text
            ref={set("atag")}
            x={L.slotR}
            y={MID_Y - 20}
            textAnchor="end"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={SLATE}
            opacity={0.35}
          >
            or after 1s
          </text>

          {/* event dots: in flight on the lane, then parked in the slot */}
          {Array.from({ length: DOTS }, (_, j) => (
            <circle
              key={`d${j}`}
              ref={set(`d${j}`)}
              cx={L.front - Q_GAP * j}
              cy={MID_Y}
              r={2.5}
              fill={CORAL}
              opacity={j < STATIC_FILL ? 0.95 : 0}
            />
          ))}

          {/* ── billing service panel ─────────────────────────────── */}
          <rect
            x={L.panelX}
            y={PANEL_TOP}
            width={PANEL_W}
            height={PANEL_H}
            rx={10}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={L.panelX + 12}
            y={PANEL_TOP + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={DIM}
          >
            BILLING SERVICE
          </text>

          {/* handler row inside the panel */}
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
            y={ROW_Y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
          >
            {ROW_SEGS.map((s, i) => (
              <tspan key={i} fill={s.f}>
                {s.t}
              </tspan>
            ))}
          </text>
          <g ref={set("hEcho")} opacity={0}>
            <rect
              x={L.rowX}
              y={ROW_Y}
              width={L.rowW}
              height={ROW_H}
              rx={5}
              fill={CYAN}
              opacity={0.08}
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
            />
          </g>

          {/* batch block, drawn last so it rides over the lane and the row */}
          <g ref={set("blk")} opacity={0}>
            <rect
              ref={set("blkg")}
              x={L.slotR - 2 - BLOCK_W}
              y={MID_Y - BLOCK_H / 2}
              width={BLOCK_W}
              height={BLOCK_H}
              rx={3.5}
              fill={CORAL}
              filter="url(#batch-soft)"
              opacity={0.35}
            />
            <rect
              ref={set("blkc")}
              x={L.slotR - 2 - BLOCK_W}
              y={MID_Y - BLOCK_H / 2}
              width={BLOCK_W}
              height={BLOCK_H}
              rx={3.5}
              fill={CORAL}
              stroke={CORAL_SOFT}
              strokeOpacity={0.5}
              strokeWidth={0.8}
            />
          </g>

          {/* arrival ring at the panel border */}
          <circle
            ref={set("ring")}
            cx={L.panelX}
            cy={MID_Y}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />

          {/* one call, whole batch */}
          <text
            ref={set("htag")}
            x={L.panelX + PANEL_W / 2}
            y={PANEL_TOP + PANEL_H + 14}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.04em"
            fill={CYAN}
            opacity={0}
          >
            HandleAsync(batch · 100)
          </text>
        </svg>
      </div>
    </div>
  );
}
