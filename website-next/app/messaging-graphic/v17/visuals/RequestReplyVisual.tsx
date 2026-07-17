"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import { CORAL, CORAL_SOFT, CYAN, GREEN, MONO_FONT } from "../palette";

// One 9s loop: ORDERS SVC fires a coral request pulse across the upper lane
// (the correlation tag brightens as it passes), the pulse docks at the
// CATALOG SERVICE panel border and dips down to the GetProductHandler row,
// which flashes through a work beat; then a green reply pulse returns on the
// lower lane and the typed "ProductResponse" flashes next to ORDERS SVC.
const T = 9000;
const REQ_START = 500;
const REQ_END = 2100;
const DIP_END = 2700;
const REP_START = 4700;
const REP_END = 6300;

// Below this width the chip, panel and lanes get too cramped to read, so we
// lay out at MIN_W and scale the whole stage down via the SVG viewBox.
const MIN_W = 460;
const DIAG_H = 150;

const DIM = "#62748e";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(139,160,188,0.28)";
const LANE_STROKE = "rgba(139,160,188,0.45)";
const CHEVRON = "rgba(139,160,188,0.6)";
const CATALOG_LIT = "#a5e3f6";
const CORR_LIT = "#c7d2f0";
const REPLY_SOFT = "#c9f7e4";

const REQ_Y = 44;
const REP_Y = 100;
const CY = 72;
const CHIP_W = 96;
const CHIP_H = 72;
const CHIP_TOP = CY - CHIP_H / 2;
const PANEL_W = 170;
const PANEL_TOP = 14;
const PANEL_H = 118;
const ROW_TOP = 64;
const ROW_H = 30;
// Radius of the rounded 90-degree elbow on the internal connector.
const ELBOW_R = 10;

interface Layout {
  readonly leftX: number;
  readonly panelX: number;
  readonly x1: number;
  readonly x2: number;
  readonly span: number;
  readonly midX: number;
  readonly rowX: number;
  readonly rowW: number;
  // Internal connector from the request dock down into the handler row: a
  // short horizontal run, a rounded 90-degree elbow, then a vertical drop.
  readonly bendX: number;
  readonly d0: number;
  readonly d1: number;
  readonly d2: number;
  readonly dipTotal: number;
}

function buildLayout(lw: number): Layout {
  const leftX = 8;
  const panelX = lw - 8 - PANEL_W;
  const x1 = leftX + CHIP_W;
  const x2 = panelX;
  const rowX = panelX + 12;
  const bendX = rowX + 26;
  const d0 = bendX - ELBOW_R - x2;
  const d1 = (Math.PI / 2) * ELBOW_R;
  const d2 = ROW_TOP - (REQ_Y + ELBOW_R);
  return {
    leftX,
    panelX,
    x1,
    x2,
    span: x2 - x1,
    midX: Math.round((x1 + x2) / 2),
    rowX,
    rowW: PANEL_W - 24,
    bendX,
    d0,
    d1,
    d2,
    dipTotal: d0 + d1 + d2,
  };
}

function dipPoint(L: Layout, u: number): readonly [number, number] {
  const dist = u * L.dipTotal;
  if (dist <= L.d0) {
    return [L.x2 + dist, REQ_Y];
  }
  if (dist <= L.d0 + L.d1) {
    const phi = ((dist - L.d0) / L.d1) * (Math.PI / 2);
    return [
      L.bendX - ELBOW_R + ELBOW_R * Math.sin(phi),
      REQ_Y + ELBOW_R - ELBOW_R * Math.cos(phi),
    ];
  }
  return [L.bendX, REQ_Y + ELBOW_R + (dist - L.d0 - L.d1)];
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

export function RequestReplyVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const [w, setW] = useState(520);
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
      const cw = entries[entries.length - 1]?.contentRect.width;
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
      // The initial render is the meaningful static frame: both lanes drawn
      // with a frozen pulse each, the chip and handler row lit softly, the
      // corr tag visible. Keep it.
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

    const setDot = (k: string, x: number, y: number) => {
      const el = E.get(k);
      if (el) {
        el.setAttribute("cx", x.toFixed(1));
        el.setAttribute("cy", y.toFixed(1));
      }
    };

    const setRing = (k: string, s: number) => {
      const el = E.get(k);
      if (!el) {
        return;
      }
      if (s < 0 || s >= 1) {
        el.setAttribute("opacity", "0");
        return;
      }
      el.setAttribute("r", (3 + 12 * easeOutCubic(s)).toFixed(2));
      el.setAttribute("opacity", (0.5 * (1 - s)).toFixed(3));
    };

    const placePulse = (
      p: string,
      x: number,
      y: number,
      op: number,
      dir: 1 | -1,
    ) => {
      if (op <= 0.01) {
        setO(p, 0);
        return;
      }
      const L = layoutRef.current;
      setO(p, op);
      setDot(p + "core", x, y);
      setDot(p + "in", x, y);
      setDot(p + "glow", x, y);
      for (let k = 1; k <= 3; k++) {
        const tx = x - dir * 7 * k;
        const el = E.get(p + "t" + k);
        if (el) {
          const on = dir === 1 ? tx > L.x1 + 2 : tx < L.x2 - 2;
          el.setAttribute("cx", tx.toFixed(1));
          el.setAttribute("cy", y.toFixed(1));
          el.setAttribute("opacity", on ? (0.45 - 0.12 * k).toFixed(2) : "0");
        }
      }
    };

    // Inside the panel the pulse follows the bent trace, so the trail dots
    // (which assume a straight lane) are hidden.
    const placeDip = (p: string, x: number, y: number, op: number) => {
      if (op <= 0.01) {
        setO(p, 0);
        return;
      }
      setO(p, op);
      setDot(p + "core", x, y);
      setDot(p + "in", x, y);
      setDot(p + "glow", x, y);
      for (let k = 1; k <= 3; k++) {
        setO(p + "t" + k, 0);
      }
    };

    const apply = (t: number) => {
      const L = layoutRef.current;

      // ORDERS chip warms up as it emits the request.
      const emit =
        easeOutCubic(ramp(t, 200, 520)) *
        (1 - easeInOutCubic(ramp(t, 950, 1700)));
      setO("cLitL", emit * 0.9);
      setO("cGlowL", emit * 0.25);

      // Coral request pulse, left to right on the upper lane, then dipping
      // inside the panel to the handler row. The correlation tag brightens by
      // proximity as either pulse passes it.
      let corr = 0;
      if (t >= REQ_START && t < REQ_END) {
        const u = easeInOutCubic(ramp(t, REQ_START, REQ_END));
        const x = L.x1 + u * L.span;
        placePulse("p1", x, REQ_Y, Math.min((t - REQ_START) / 200, 1), 1);
        corr = Math.max(corr, clamp01(1 - Math.abs(x - L.midX) / 70));
      } else if (t >= REQ_END && t < DIP_END) {
        const u = easeInOutCubic(ramp(t, REQ_END, DIP_END));
        const [x, y] = dipPoint(L, u);
        placeDip("p1", x, y, 1 - ramp(t, DIP_END - 140, DIP_END));
      } else {
        setO("p1", 0);
      }

      // Green reply pulse, right to left on the lower lane.
      if (t >= REP_START && t < REP_END) {
        const u = easeInOutCubic(ramp(t, REP_START, REP_END));
        const x = L.x2 - u * L.span;
        placePulse("p2", x, REP_Y, Math.min((t - REP_START) / 200, 1), -1);
        corr = Math.max(corr, clamp01(1 - Math.abs(x - L.midX) / 70));
      } else {
        setO("p2", 0);
      }
      setO("corrLit", corr * 0.95);

      // Request docks at the panel border: coral ring, then the handler row
      // and panel edge stay warm through the work beat until the reply leaves.
      setRing("ringR", (t - REQ_END) / 700);
      const he =
        easeOutCubic(ramp(t, DIP_END - 80, DIP_END + 160)) *
        (1 - easeInOutCubic(ramp(t, 4400, 5300)));
      setO("hFx", he * 0.9);
      const work =
        easeOutCubic(ramp(t, DIP_END - 60, DIP_END + 220)) *
        (1 - easeInOutCubic(ramp(t, 4400, 5200)));
      setO("pnlFx", work * 0.4);

      // Reply arrival back at ORDERS: green ring + green chip echo.
      setRing("ringL", (t - REP_END) / 700);
      const done =
        easeOutCubic(ramp(t, REP_END, REP_END + 180)) *
        (1 - easeInOutCubic(ramp(t, 7000, 7800)));
      setO("cEchoL", done * 0.8);

      // awaiting… sits under ORDERS while the request is in flight.
      const aw =
        easeOutCubic(ramp(t, 650, 950)) *
        (1 - ramp(t, REP_END - 100, REP_END + 160));
      setO("await", aw * 0.55);

      // The typed response pops next to the chip, holds, fades for the loop.
      const resp =
        easeOutCubic(ramp(t, REP_END, REP_END + 320)) *
        (1 - ramp(t, 8150, 8700));
      setO("resp", resp * 0.9);
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

    // Paint the phase-0 frame so the static JSX defaults never flash.
    apply(0);

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
  // Static-frame positions: request pulse frozen mid upper lane, reply pulse
  // frozen mid lower lane, each with its trail.
  const reqX = L.x1 + 0.58 * L.span;
  const repX = L.x2 - 0.62 * L.span;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[320px]"
    >
      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${DIAG_H}`}
          width="100%"
          height={(DIAG_H * w) / lw}
          className="block"
        >
          <defs>
            <filter
              id="reqrep-soft"
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <filter
              id="reqrep-glow"
              x="-300%"
              y="-300%"
              width="700%"
              height="700%"
            >
              <feGaussianBlur stdDeviation="2.6" />
            </filter>
          </defs>

          {/* ── CATALOG service panel ──────────────────────────────── */}
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
          <rect
            ref={set("pnlFx")}
            x={L.panelX}
            y={PANEL_TOP}
            width={PANEL_W}
            height={PANEL_H}
            rx={10}
            fill="none"
            stroke={CYAN}
            strokeWidth={1}
            opacity={0}
          />
          <text
            x={L.panelX + 12}
            y={PANEL_TOP + 18}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={DIM}
          >
            CATALOG SERVICE
          </text>

          {/* internal connector: request dock turns down into the handler
              row through a rounded 90-degree elbow */}
          <path
            d={`M${L.x2} ${REQ_Y} H${L.bendX - ELBOW_R} A${ELBOW_R} ${ELBOW_R} 0 0 1 ${L.bendX} ${REQ_Y + ELBOW_R} V${ROW_TOP}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.75}
          />

          {/* handler row inside the panel */}
          <rect
            x={L.rowX}
            y={ROW_TOP}
            width={L.rowW}
            height={ROW_H}
            rx={5}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={L.rowX}
            y={ROW_TOP + 4}
            width={3}
            height={ROW_H - 8}
            rx={1.5}
            fill={CYAN}
          />
          <text
            x={L.rowX + 13}
            y={ROW_TOP + 19}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.04em"
            fill={DIM}
          >
            GetProductHandler
          </text>
          <g ref={set("hFx")} opacity={0.35}>
            <rect
              x={L.rowX}
              y={ROW_TOP}
              width={L.rowW}
              height={ROW_H}
              rx={5}
              fill="none"
              stroke={CYAN}
              strokeWidth={1.2}
            />
            <text
              x={L.rowX + 13}
              y={ROW_TOP + 19}
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.04em"
              fill={CATALOG_LIT}
            >
              GetProductHandler
            </text>
          </g>

          {/* ── lanes: request upper, reply lower, docked flush into the
              chip and panel borders ─────────────────────────────────── */}
          <path
            d={`M${L.x1} ${REQ_Y} H${L.x2}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.75}
          />
          <path
            d={`M${L.x1} ${REP_Y} H${L.x2}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.75}
          />
          {/* direction chevrons mid-lane: request right, reply left */}
          <path
            d={`M${L.midX - 3} ${REQ_Y - 3.5} L${L.midX + 3} ${REQ_Y} L${L.midX - 3} ${REQ_Y + 3.5}`}
            fill="none"
            stroke={CHEVRON}
            strokeWidth={1.25}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path
            d={`M${L.midX + 3} ${REP_Y - 3.5} L${L.midX - 3} ${REP_Y} L${L.midX + 3} ${REP_Y + 3.5}`}
            fill="none"
            stroke={CHEVRON}
            strokeWidth={1.25}
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* correlation tag between the lanes, dim at rest */}
          <text
            x={L.midX}
            y={CY + 2.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={DIM}
            opacity={0.55}
          >
            corr · 7f3a
          </text>
          <text
            ref={set("corrLit")}
            x={L.midX}
            y={CY + 2.5}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.06em"
            fill={CORR_LIT}
            opacity={0}
          >
            corr · 7f3a
          </text>

          {/* awaiting state under ORDERS while the request is in flight */}
          <text
            ref={set("await")}
            x={L.leftX + CHIP_W / 2}
            y={CHIP_TOP + CHIP_H + 14}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.08em"
            fill={DIM}
            opacity={0}
          >
            awaiting…
          </text>

          {/* the typed response flashes next to the chip on reply arrival,
              on its own line below the corr tag so the two never collide */}
          <text
            ref={set("resp")}
            x={L.x1 + 10}
            y={CY + 16}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.02em"
            fill={GREEN}
            opacity={0}
          >
            ProductResponse
          </text>

          {/* arrival rings at each lane end */}
          <circle
            ref={set("ringR")}
            cx={L.x2}
            cy={REQ_Y}
            r={3}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.5}
            opacity={0}
          />
          <circle
            ref={set("ringL")}
            cx={L.x1}
            cy={REP_Y}
            r={3}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.5}
            opacity={0}
          />

          {/* coral request pulse */}
          <g ref={set("p1")} opacity={1}>
            {[1, 2, 3].map((k) => (
              <circle
                key={`p1t${k}`}
                ref={set(`p1t${k}`)}
                cx={reqX - 7 * k}
                cy={REQ_Y}
                r={[2, 1.7, 1.4][k - 1]}
                fill={CORAL}
                opacity={[0.33, 0.21, 0.09][k - 1]}
              />
            ))}
            <circle
              ref={set("p1glow")}
              cx={reqX}
              cy={REQ_Y}
              r={6}
              fill={CORAL}
              filter="url(#reqrep-glow)"
              opacity={0.22}
            />
            <circle
              ref={set("p1core")}
              cx={reqX}
              cy={REQ_Y}
              r={2.5}
              fill={CORAL}
            />
            <circle
              ref={set("p1in")}
              cx={reqX}
              cy={REQ_Y}
              r={1.1}
              fill={CORAL_SOFT}
            />
          </g>

          {/* green reply pulse */}
          <g ref={set("p2")} opacity={1}>
            {[1, 2, 3].map((k) => (
              <circle
                key={`p2t${k}`}
                ref={set(`p2t${k}`)}
                cx={repX + 7 * k}
                cy={REP_Y}
                r={[2, 1.7, 1.4][k - 1]}
                fill={GREEN}
                opacity={[0.33, 0.21, 0.09][k - 1]}
              />
            ))}
            <circle
              ref={set("p2glow")}
              cx={repX}
              cy={REP_Y}
              r={6}
              fill={GREEN}
              filter="url(#reqrep-glow)"
              opacity={0.22}
            />
            <circle
              ref={set("p2core")}
              cx={repX}
              cy={REP_Y}
              r={2.5}
              fill={GREEN}
            />
            <circle
              ref={set("p2in")}
              cx={repX}
              cy={REP_Y}
              r={1.1}
              fill={REPLY_SOFT}
            />
          </g>

          {/* ── ORDERS SVC chip, drawn last so pulses dip underneath ── */}
          <rect
            ref={set("cGlowL")}
            x={L.leftX}
            y={CHIP_TOP}
            width={CHIP_W}
            height={CHIP_H}
            rx={10}
            fill="none"
            stroke={CORAL}
            strokeWidth={5}
            filter="url(#reqrep-soft)"
            opacity={0.15}
          />
          <rect
            x={L.leftX}
            y={CHIP_TOP}
            width={CHIP_W}
            height={CHIP_H}
            rx={10}
            fill={SURFACE}
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={L.leftX + CHIP_W / 2}
            y={CY + 3}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.1em"
            fill={CORAL}
            opacity={0.85}
          >
            ORDERS SVC
          </text>
          <g ref={set("cLitL")} opacity={0.45}>
            <rect
              x={L.leftX}
              y={CHIP_TOP}
              width={CHIP_W}
              height={CHIP_H}
              rx={10}
              fill="none"
              stroke={CORAL}
              strokeWidth={1}
            />
            <text
              x={L.leftX + CHIP_W / 2}
              y={CY + 3}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.1em"
              fill={CORAL_SOFT}
            >
              ORDERS SVC
            </text>
          </g>
          <rect
            ref={set("cEchoL")}
            x={L.leftX}
            y={CHIP_TOP}
            width={CHIP_W}
            height={CHIP_H}
            rx={10}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.2}
            opacity={0}
          />
        </svg>
      </div>
    </div>
  );
}
