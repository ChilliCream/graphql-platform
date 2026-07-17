"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import { AMBER, CORAL, CORAL_SOFT, GREEN, MONO_FONT } from "../palette";

const T = 5000;
const H = 300;
// Below this width the lane geometry breaks (the INBOX chip meets the
// frame), so we lay out at MIN_W and scale the SVG down via its viewBox.
const MIN_W = 460;
const INK = "#a1a3af";
const DIM = "#62748e";
const ACCENT = "#5eead4";
const SURFACE = "#0c1322";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const HAIR = "rgba(139,160,188,0.22)";

interface Seg {
  readonly t: string;
  readonly f: string;
}

const ROW1_SEGS: readonly Seg[] = [
  { t: "INSERT", f: ACCENT },
  { t: " reviews (…)", f: INK },
];

const ROW2_SEGS: readonly Seg[] = [
  { t: "INSERT", f: ACCENT },
  { t: " outbox · ", f: INK },
  { t: "ReviewCreated", f: CORAL_SOFT },
  { t: " · id ", f: INK },
  { t: "7f3a", f: CORAL },
];

const ROW1_LEN = ROW1_SEGS.reduce((n, s) => n + s.t.length, 0);
const ROW2_LEN = ROW2_SEGS.reduce((n, s) => n + s.t.length, 0);

interface Layout {
  readonly chipX: number;
  readonly chipW: number;
  readonly chipCX: number;
  readonly frameX: number;
  readonly frameY: number;
  readonly frameW: number;
  readonly frameH: number;
  readonly frameR: number;
  readonly rowX: number;
  readonly rowW: number;
  readonly rowH: number;
  readonly row1Y: number;
  readonly row2Y: number;
  readonly startX: number;
  readonly startY: number;
  readonly cy: number;
  readonly rowFont: number;
  readonly pts: ReadonlyArray<readonly [number, number]>;
  readonly cum: readonly number[];
}

function buildLayout(w: number): Layout {
  const chipW = 76;
  const chipX = w - chipW - 10;
  const frameX = 10;
  const frameY = 74;
  const frameH = 170;
  const frameW = Math.max(240, Math.min(336, w - chipW - 150));
  const frameR = frameX + frameW;
  const rowX = frameX + 16;
  const rowW = frameW - 32;
  const rowH = 34;
  const row1Y = frameY + 40;
  const row2Y = frameY + 94;
  const startX = rowX + rowW;
  const startY = row2Y + rowH / 2;
  const span = chipX - frameR;
  const dy = Math.max(12, Math.min(46, span - 22));
  const h1 = Math.min(60, Math.max(6, Math.round((span - dy) * 0.35)));
  const bx = frameR + h1;
  const cy = startY - dy;
  const pts: ReadonlyArray<readonly [number, number]> = [
    [startX, startY],
    [bx, startY],
    [bx + dy, cy],
    [chipX, cy],
  ];
  const cum: number[] = [0];
  for (let i = 1; i < pts.length; i++) {
    cum.push(
      cum[i - 1] +
        Math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]),
    );
  }
  const rowFont = Math.min(10.5, (rowW - 18) / (ROW2_LEN * 0.635));
  return {
    chipX,
    chipW,
    chipCX: chipX + chipW / 2,
    frameX,
    frameY,
    frameW,
    frameH,
    frameR,
    rowX,
    rowW,
    rowH,
    row1Y,
    row2Y,
    startX,
    startY,
    cy,
    rowFont,
    pts,
    cum,
  };
}

function clamp01(v: number) {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

function ramp(t: number, a: number, b: number) {
  return clamp01((t - a) / (b - a));
}

function easeInOutCubic(u: number) {
  return u < 0.5 ? 4 * u * u * u : 1 - Math.pow(-2 * u + 2, 3) / 2;
}

function easeOutCubic(u: number) {
  return 1 - Math.pow(1 - u, 3);
}

function pointAt(layout: Layout, d: number): readonly [number, number] {
  const { pts, cum } = layout;
  const total = cum[cum.length - 1];
  const dd = Math.max(0, Math.min(total, d));
  let i = 1;
  while (i < cum.length - 1 && dd > cum[i]) {
    i++;
  }
  const segLen = cum[i] - cum[i - 1] || 1;
  const u = (dd - cum[i - 1]) / segLen;
  return [
    pts[i - 1][0] + (pts[i][0] - pts[i - 1][0]) * u,
    pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * u,
  ];
}

export function OutboxVisual() {
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
      // The initial render is the meaningful final frame; keep it static.
      return;
    }

    const E = els;
    let raf = 0;
    let running = false;
    let inView = false;
    let n1Cache = -1;
    let n2Cache = -1;

    const setO = (k: string, v: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("opacity", v.toFixed(3));
      }
    };

    const setDot = (k: string, x: number, y: number, r?: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("cx", x.toFixed(1));
        el.setAttribute("cy", y.toFixed(1));
        if (r !== undefined) {
          el.setAttribute("r", Math.max(0, r).toFixed(2));
        }
      }
    };

    const writeTyped = (prefix: string, segs: readonly Seg[], n: number) => {
      let cum = 0;
      for (let i = 0; i < segs.length; i++) {
        const el = E.get(prefix + i);
        if (el) {
          const len = Math.max(0, Math.min(segs[i].t.length, n - cum));
          el.textContent = segs[i].t.slice(0, len);
        }
        cum += segs[i].t.length;
      }
    };

    const placePulse = (
      p: string,
      d: number,
      groupOp: number,
      coreR: number,
    ) => {
      if (coreR <= 0.05 || groupOp <= 0.01) {
        setO(p, 0);
        return;
      }
      const L = layoutRef.current;
      setO(p, groupOp);
      const [x, y] = pointAt(L, d);
      setDot(p + "core", x, y, coreR);
      setDot(p + "inner", x, y, coreR * 0.45);
      setDot(p + "glow", x, y, Math.max(0.6, coreR * 2.4));
      for (let k = 1; k <= 3; k++) {
        const dk = d - k * 7;
        const [tx, ty] = pointAt(L, dk);
        const el = E.get(p + "t" + k);
        if (el) {
          el.setAttribute("cx", tx.toFixed(1));
          el.setAttribute("cy", ty.toFixed(1));
          el.setAttribute(
            "opacity",
            dk > 2 ? (0.5 - k * 0.13).toFixed(2) : "0",
          );
        }
      }
    };

    const apply = (t: number) => {
      const L = layoutRef.current;
      const total = L.cum[L.cum.length - 1];
      const master = 1 - ramp(t, 4860, 4990);

      // Phase 1: the two ledger rows type in, one after the other.
      setO("r1g", ramp(t, 150, 350) * master);
      setO("r2g", ramp(t, 650, 850) * master);
      const n1 = Math.round(ROW1_LEN * ramp(t, 150, 620));
      if (n1 !== n1Cache) {
        n1Cache = n1;
        writeTyped("r1s", ROW1_SEGS, n1);
      }
      const n2 = Math.round(ROW2_LEN * ramp(t, 650, 1260));
      if (n2 !== n2Cache) {
        n2Cache = n2;
        writeTyped("r2s", ROW2_SEGS, n2);
      }

      // Phase 2: single green commit flash on the transaction frame + badge.
      const flash =
        t < 1600
          ? easeOutCubic(ramp(t, 1450, 1600))
          : 1 - easeInOutCubic(ramp(t, 1600, 2080));
      setO("fxRect", flash * 0.9);
      setO("fxGlow", flash * 0.22);
      const b = easeOutCubic(ramp(t, 1620, 1900));
      const badge = E.get("badge");
      if (badge) {
        badge.setAttribute("opacity", (b * master).toFixed(3));
        badge.setAttribute(
          "transform",
          `translate(0 ${((1 - b) * 5).toFixed(2)})`,
        );
      }

      // Phase 3: coral delivery pulse, inbox processes it.
      if (t >= 2150 && t < 2900) {
        const u = easeInOutCubic(ramp(t, 2150, 2900));
        placePulse("p1", u * total, 1, 2.5);
      } else {
        setO("p1", 0);
      }
      const echo = t < 2260 ? ramp(t, 2160, 2260) : 1 - ramp(t, 2260, 2650);
      setO("r2echo", Math.max(0, echo) * 0.5);
      const ringU = ramp(t, 2900, 3560);
      const ring = E.get("ring");
      if (ring) {
        ring.setAttribute(
          "opacity",
          (ringU > 0 && ringU < 1 ? (1 - ringU) * 0.55 : 0).toFixed(3),
        );
        ring.setAttribute("r", (4 + 15 * easeOutCubic(ringU)).toFixed(2));
      }
      const lit =
        easeOutCubic(ramp(t, 2880, 2980)) *
        (1 - easeInOutCubic(ramp(t, 3100, 3850)));
      setO("chipLit", lit * 0.9);
      setO("chipLitText", lit);
      setO("chipGlow", lit * 0.25);
      setO("proc", ramp(t, 2950, 3250) * 0.95 * master);

      // Phase 4: amber redelivery of the same id dissolves at the inbox.
      const d0 = L.frameR - L.startX + 4;
      if (t >= 3550 && t < 4300) {
        const u = easeInOutCubic(ramp(t, 3550, 4300));
        placePulse(
          "p2",
          d0 + u * (total - d0),
          Math.min(1, (t - 3550) / 120),
          2.5,
        );
      } else if (t >= 4300 && t < 4620) {
        const u = ramp(t, 4300, 4620);
        placePulse("p2", total, 1 - u, 2.5 * (1 - u));
      } else {
        setO("p2", 0);
      }
      const e = t - 4300;
      let dd = 0;
      if (e >= 0) {
        dd = e < 780 ? (Math.floor(e / 130) % 2 === 0 ? 0.85 : 0.2) : 0.6;
      }
      setO("dedup", dd * master);
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
  const laneD = L.pts
    .map((p, i) => `${i === 0 ? "M" : "L"}${p[0]} ${p[1]}`)
    .join(" ");
  const chipY = L.cy - 15;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative h-auto w-full overflow-hidden rounded-2xl border backdrop-blur sm:h-[400px]"
    >
      <div className="flex h-full flex-col p-5">
        <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
          <svg
            viewBox={`0 0 ${lw} ${H}`}
            width="100%"
            height={(H * w) / lw}
            className="block"
          >
            <defs>
              <filter
                id="obxGlow"
                x="-300%"
                y="-300%"
                width="700%"
                height="700%"
              >
                <feGaussianBlur stdDeviation="2.6" />
              </filter>
              <filter id="obxSoft" x="-40%" y="-90%" width="180%" height="280%">
                <feGaussianBlur stdDeviation="2.2" />
              </filter>
              <filter
                id="obxFrameGlow"
                x="-12%"
                y="-20%"
                width="124%"
                height="140%"
              >
                <feGaussianBlur stdDeviation="2" />
              </filter>
            </defs>

            {/* copper lane */}
            <path
              d={laneD}
              fill="none"
              stroke={LANE_STROKE}
              strokeWidth={1.5}
            />
            {L.pts.slice(1, 3).map((p) => (
              <circle
                key={`${p[0]}-${p[1]}`}
                cx={p[0]}
                cy={p[1]}
                r={2.5}
                fill={SURFACE}
                stroke="rgba(139,160,188,0.5)"
                strokeWidth={1}
              />
            ))}

            {/* transaction frame */}
            <rect
              x={L.frameX}
              y={L.frameY}
              width={L.frameW}
              height={L.frameH}
              rx={10}
              fill="rgba(139,160,188,0.04)"
              stroke="rgba(139,160,188,0.45)"
              strokeWidth={1}
              strokeDasharray="5 5"
            />
            <rect
              ref={set("fxGlow")}
              x={L.frameX}
              y={L.frameY}
              width={L.frameW}
              height={L.frameH}
              rx={10}
              fill="none"
              stroke={GREEN}
              strokeWidth={3}
              strokeDasharray="5 5"
              filter="url(#obxFrameGlow)"
              opacity={0}
            />
            <rect
              ref={set("fxRect")}
              x={L.frameX}
              y={L.frameY}
              width={L.frameW}
              height={L.frameH}
              rx={10}
              fill="none"
              stroke={GREEN}
              strokeWidth={1}
              strokeDasharray="5 5"
              opacity={0}
            />
            <text
              x={L.frameX + 12}
              y={L.frameY + 18}
              fontFamily={MONO_FONT}
              fontSize={8}
              letterSpacing="0.2em"
              fill={DIM}
            >
              BEGIN
            </text>
            <text
              x={L.frameR - 12}
              y={L.frameY + L.frameH - 10}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8}
              letterSpacing="0.2em"
              fill={DIM}
            >
              COMMIT
            </text>

            {/* ledger rows */}
            <g ref={set("r1g")} opacity={1}>
              <rect
                x={L.rowX}
                y={L.row1Y}
                width={L.rowW}
                height={L.rowH}
                rx={7}
                fill={SURFACE}
                stroke={HAIR}
                strokeWidth={1}
              />
              <text
                x={L.rowX + 10}
                y={L.row1Y + 20.5}
                fontFamily={MONO_FONT}
                fontSize={L.rowFont}
              >
                {ROW1_SEGS.map((s, i) => (
                  <tspan key={s.t} ref={set(`r1s${i}`)} fill={s.f}>
                    {s.t}
                  </tspan>
                ))}
              </text>
            </g>
            <g ref={set("r2g")} opacity={1}>
              <rect
                x={L.rowX}
                y={L.row2Y}
                width={L.rowW}
                height={L.rowH}
                rx={7}
                fill={SURFACE}
                stroke={HAIR}
                strokeWidth={1}
              />
              <text
                x={L.rowX + 10}
                y={L.row2Y + 20.5}
                fontFamily={MONO_FONT}
                fontSize={L.rowFont}
              >
                {ROW2_SEGS.map((s, i) => (
                  <tspan key={s.t} ref={set(`r2s${i}`)} fill={s.f}>
                    {s.t}
                  </tspan>
                ))}
              </text>
            </g>
            <rect
              ref={set("r2echo")}
              x={L.rowX}
              y={L.row2Y}
              width={L.rowW}
              height={L.rowH}
              rx={7}
              fill="none"
              stroke={CORAL}
              strokeWidth={1}
              opacity={0}
            />

            {/* committed badge */}
            <g ref={set("badge")} opacity={1}>
              <rect
                x={L.frameR - 144}
                y={L.frameY + L.frameH + 10}
                width={132}
                height={18}
                rx={5}
                fill={SURFACE}
                stroke={GREEN + "55"}
                strokeWidth={1}
              />
              <text
                x={L.frameR - 78}
                y={L.frameY + L.frameH + 22.5}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={8}
                letterSpacing="0.18em"
                fill={GREEN}
              >
                COMMITTED TOGETHER
              </text>
            </g>

            {/* inbox chip */}
            <rect
              ref={set("chipGlow")}
              x={L.chipX}
              y={chipY}
              width={L.chipW}
              height={30}
              rx={6}
              fill="none"
              stroke={GREEN}
              strokeWidth={5}
              filter="url(#obxSoft)"
              opacity={0}
            />
            <rect
              x={L.chipX}
              y={chipY}
              width={L.chipW}
              height={30}
              rx={6}
              fill={SURFACE}
              stroke="rgba(139,160,188,0.35)"
              strokeWidth={1}
            />
            <rect
              ref={set("chipLit")}
              x={L.chipX}
              y={chipY}
              width={L.chipW}
              height={30}
              rx={6}
              fill="none"
              stroke={GREEN}
              strokeWidth={1}
              opacity={0}
            />
            <text
              x={L.chipCX}
              y={L.cy + 3.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.14em"
              fill={INK}
            >
              INBOX
            </text>
            <text
              ref={set("chipLitText")}
              x={L.chipCX}
              y={L.cy + 3.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9.5}
              letterSpacing="0.14em"
              fill={GREEN}
              opacity={0}
            >
              INBOX
            </text>
            <circle
              ref={set("ring")}
              cx={L.chipCX}
              cy={L.cy}
              r={4}
              fill="none"
              stroke={GREEN}
              strokeWidth={1.5}
              opacity={0}
            />

            {/* chip status labels */}
            <text
              ref={set("proc")}
              x={lw - 10}
              y={L.cy + 33}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8}
              letterSpacing="0.14em"
              fill={GREEN}
              opacity={0.95}
            >
              PROCESSED · id 7f3a
            </text>
            <text
              ref={set("dedup")}
              x={lw - 10}
              y={L.cy + 49}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={8}
              letterSpacing="0.14em"
              fill={DIM}
              opacity={0.6}
            >
              DEDUPED BY ID
            </text>

            {/* coral delivery pulse */}
            <g ref={set("p1")} opacity={0}>
              <circle ref={set("p1t1")} r={2} fill={CORAL} opacity={0} />
              <circle ref={set("p1t2")} r={1.7} fill={CORAL} opacity={0} />
              <circle ref={set("p1t3")} r={1.4} fill={CORAL} opacity={0} />
              <circle
                ref={set("p1glow")}
                r={6}
                fill={CORAL}
                filter="url(#obxGlow)"
                opacity={0.22}
              />
              <circle ref={set("p1core")} r={2.5} fill={CORAL} />
              <circle ref={set("p1inner")} r={1.1} fill={CORAL_SOFT} />
            </g>

            {/* amber redelivery pulse */}
            <g ref={set("p2")} opacity={0}>
              <circle ref={set("p2t1")} r={2} fill={AMBER} opacity={0} />
              <circle ref={set("p2t2")} r={1.7} fill={AMBER} opacity={0} />
              <circle ref={set("p2t3")} r={1.4} fill={AMBER} opacity={0} />
              <circle
                ref={set("p2glow")}
                r={6}
                fill={AMBER}
                filter="url(#obxGlow)"
                opacity={0.22}
              />
              <circle ref={set("p2core")} r={2.5} fill={AMBER} />
              <circle ref={set("p2inner")} r={1.1} fill="#fde68a" />
            </g>
          </svg>
        </div>
        <div className="border-cc-card-border text-cc-nav-label mt-4 border-t pt-3 font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          AT-LEAST-ONCE DELIVERY · EXACTLY-ONCE PROCESSING
        </div>
      </div>
    </div>
  );
}
