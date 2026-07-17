"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  VIOLET,
} from "../palette";

const T = 9000;
const H = 240;
// Below this width the two service panels get too cramped to read, so we lay
// out at MIN_W and scale the whole stage down via the SVG viewBox.
const MIN_W = 600;

const INK = "#a1a3af";
const DIM = "#62748e";
const ACCENT = "#5eead4";
const SURFACE = "#0c1322";
const HAIR = "rgba(139,160,188,0.22)";
const PANEL_STROKE = "rgba(139,160,188,0.25)";
const FRAME_STROKE = "rgba(139,160,188,0.45)";
const LANE_STROKE = "rgba(139,160,188,0.4)";

const PANEL_Y = 16;
const PANEL_H = 156;
const FRAME_H = 108;
const ROW_H = 34;
// Left frame row 2 (outbox) and right frame row 1 (inbox) share this center
// line, so one straight copper lane connects OUTBOX -> RABBITMQ -> INBOX.
const LANE_Y = 129;
const LEFT_FY = 50;
const RIGHT_FY = 90;
const CHIP_W = 78;
const CHIP_Y = LANE_Y - 13;

interface Seg {
  readonly t: string;
  readonly f: string;
}

const L_ROW1: readonly Seg[] = [
  { t: "INSERT", f: ACCENT },
  { t: " orders …", f: INK },
];

const L_ROW2: readonly Seg[] = [
  { t: "OUTBOX", f: CORAL },
  { t: " · ", f: INK },
  { t: "OrderPlaced", f: CORAL_SOFT },
  { t: " · ", f: INK },
  { t: "7f3a", f: CORAL },
];

const R_ROW1: readonly Seg[] = [
  { t: "INBOX", f: CYAN },
  { t: " · ", f: INK },
  { t: "7f3a", f: CYAN },
  { t: " · ", f: INK },
];

const R_ROW2: readonly Seg[] = [
  { t: "Handle", f: ACCENT },
  { t: "(OrderPlaced)", f: INK },
];

const L1_LEN = L_ROW1.reduce((n, s) => n + s.t.length, 0);
const L2_LEN = L_ROW2.reduce((n, s) => n + s.t.length, 0);
// Widest row text: "OUTBOX · OrderPlaced · 7f3a".
const MAX_CHARS = 27;

const STATUS = [
  { t: "new", f: DIM },
  { t: "recorded", f: CYAN },
  { t: "seen — skip", f: AMBER },
] as const;

interface Panel {
  readonly px: number;
  readonly py: number;
  readonly fx: number;
  readonly fw: number;
  readonly fy: number;
  readonly frameD: string;
  readonly tagX: number;
  readonly rowX: number;
  readonly rowW: number;
  readonly row1Y: number;
  readonly row2Y: number;
}

interface Layout {
  readonly pw: number;
  readonly left: Panel;
  readonly right: Panel;
  readonly x1: number;
  readonly x2: number;
  readonly laneTotal: number;
  readonly inboxRun: number;
  readonly chipX: number;
  readonly midX: number;
  readonly rowFont: number;
}

// Rounded dashed rect with a gap in the top edge for the ONE TRANSACTION tag.
function framePath(
  x: number,
  y: number,
  w: number,
  h: number,
  gx1: number,
  gx2: number,
): string {
  const r = 9;
  const right = x + w;
  const bottom = y + h;
  return [
    `M${gx2} ${y}`,
    `H${right - r}`,
    `Q${right} ${y} ${right} ${y + r}`,
    `V${bottom - r}`,
    `Q${right} ${bottom} ${right - r} ${bottom}`,
    `H${x + r}`,
    `Q${x} ${bottom} ${x} ${bottom - r}`,
    `V${y + r}`,
    `Q${x} ${y} ${x + r} ${y}`,
    `H${gx1}`,
  ].join(" ");
}

function buildPanel(px: number, py: number, pw: number, fy: number): Panel {
  const fx = px + 10;
  const fw = pw - 20;
  const gx1 = fx + 12;
  const gx2 = gx1 + 112;
  return {
    px,
    py,
    fx,
    fw,
    fy,
    frameD: framePath(fx, fy, fw, FRAME_H, gx1, gx2),
    tagX: gx1 + 4,
    rowX: fx + 9,
    rowW: fw - 18,
    row1Y: fy + 22,
    row2Y: fy + 62,
  };
}

function buildLayout(w: number): Layout {
  const m = 6;
  const gap = Math.max(104, Math.min(140, Math.round(w * 0.2)));
  const pw = Math.floor((w - 2 * m - gap) / 2);
  const leftX = m;
  const rightX = w - m - pw;
  const left = buildPanel(leftX, PANEL_Y, pw, LEFT_FY);
  const right = buildPanel(rightX, PANEL_Y + 40, pw, RIGHT_FY);
  const x1 = leftX + pw;
  const x2 = rightX;
  return {
    pw,
    left,
    right,
    x1,
    x2,
    laneTotal: x2 - x1,
    inboxRun: right.rowX + 16 - x2,
    chipX: Math.round(w / 2 - CHIP_W / 2),
    midX: Math.round((x1 + x2) / 2),
    rowFont: Math.min(10, (left.rowW - 20) / (MAX_CHARS * 0.635)),
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
    let statusCache = -1;

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
      const x = L.x1 + Math.max(0, d);
      setDot(p + "core", x, LANE_Y, coreR);
      setDot(p + "inner", x, LANE_Y, coreR * 0.45);
      setDot(p + "glow", x, LANE_Y, Math.max(0.6, coreR * 2.4));
      for (let k = 1; k <= 3; k++) {
        const dk = d - k * 7;
        const el = E.get(p + "t" + k);
        if (el) {
          el.setAttribute("cx", (L.x1 + Math.max(0, dk)).toFixed(1));
          el.setAttribute("cy", LANE_Y.toFixed(1));
          el.setAttribute(
            "opacity",
            dk > 2 ? (0.5 - k * 0.13).toFixed(2) : "0",
          );
        }
      }
    };

    const apply = (t: number) => {
      const L = layoutRef.current;
      const master = 1 - ramp(t, 8730, 8960);

      // Phase 1: business row and outbox row written in one transaction.
      setO("r1g", ramp(t, 150, 450) * master);
      setO("r2g", ramp(t, 930, 1230) * master);
      const n1 = Math.round(L1_LEN * ramp(t, 150, 840));
      if (n1 !== n1Cache) {
        n1Cache = n1;
        writeTyped("r1s", L_ROW1, n1);
      }
      const n2 = Math.round(L2_LEN * ramp(t, 930, 1860));
      if (n2 !== n2Cache) {
        n2Cache = n2;
        writeTyped("r2s", L_ROW2, n2);
      }

      // Left TX frame commits: green border flash + COMMIT tag pops.
      const f1 =
        t < 2250
          ? easeOutCubic(ramp(t, 2025, 2250))
          : 1 - easeInOutCubic(ramp(t, 2250, 3075));
      setO("fxL", f1 * 0.9);
      setO("fxLg", f1 * 0.25);
      const b1 = easeOutCubic(ramp(t, 2145, 2550));
      setPop("cmL", b1 * master, b1);

      // Phase 2: coral delivery pulse crosses through the broker.
      const e1 = t < 3075 ? ramp(t, 2925, 3075) : 1 - ramp(t, 3075, 3675);
      setO("obEcho", Math.max(0, e1) * 0.6);
      if (t >= 3075 && t < 4125) {
        const u = easeInOutCubic(ramp(t, 3075, 4125));
        placePulse("p1", u * L.laneTotal, 1, 2.5);
      } else {
        setO("p1", 0);
      }
      const lit = Math.max(
        easeOutCubic(ramp(t, 3405, 3555)) *
          (1 - easeInOutCubic(ramp(t, 3705, 4050))),
        easeOutCubic(ramp(t, 6840, 6990)) *
          (1 - easeInOutCubic(ramp(t, 7140, 7485))),
      );
      setO("chipLit", lit * 0.9);
      setO("chipLitTx", lit);
      setO("chipGlow", lit * 0.2);

      // Arrival at the inbox: ring flash, cyan row flash, status recorded.
      const ru = ramp(t, 4125, 5100);
      const ring = E.get("ring");
      if (ring) {
        ring.setAttribute(
          "opacity",
          (ru > 0 && ru < 1 ? (1 - ru) * 0.5 : 0).toFixed(3),
        );
        ring.setAttribute("r", (4 + 15 * easeOutCubic(ru)).toFixed(2));
      }
      const e2 = t < 4275 ? ramp(t, 4125, 4275) : 1 - ramp(t, 4275, 5175);
      setO("inEchoC", Math.max(0, e2) * 0.7);
      const e3 = t < 4875 ? ramp(t, 4725, 4875) : 1 - ramp(t, 4875, 5700);
      setO("hEcho", Math.max(0, e3) * 0.6);

      const si = t >= 7530 ? 2 : t >= 4230 ? 1 : 0;
      if (si !== statusCache) {
        statusCache = si;
        const el = E.get("status");
        if (el) {
          el.textContent = STATUS[si].t;
          el.setAttribute("fill", STATUS[si].f);
        }
      }

      // Right TX frame commits.
      const f2 =
        t < 5625
          ? easeOutCubic(ramp(t, 5400, 5625))
          : 1 - easeInOutCubic(ramp(t, 5625, 6450));
      setO("fxR", f2 * 0.9);
      setO("fxRg", f2 * 0.25);
      const b2 = easeOutCubic(ramp(t, 5520, 5925));
      setPop("cmR", b2 * master, b2);

      // Phase 3: amber redelivery of the same id dissolves inside the inbox.
      if (t >= 6525 && t < 7500) {
        const u = easeInOutCubic(ramp(t, 6525, 7500));
        placePulse("p2", u * L.laneTotal, Math.min(1, (t - 6525) / 180), 2.5);
      } else if (t >= 7500 && t < 7995) {
        const v = easeInOutCubic(ramp(t, 7500, 7995));
        const r = 2.5 * (1 - easeInOutCubic(ramp(t, 7680, 7995)));
        placePulse(
          "p2",
          L.laneTotal + v * L.inboxRun,
          1 - ramp(t, 7770, 7995),
          r,
        );
      } else {
        setO("p2", 0);
      }
      const rt = ramp(t, 6645, 6975) * (1 - ramp(t, 7620, 8070));
      setO("redeliv", rt * 0.9);
      const e4 = t < 7650 ? ramp(t, 7500, 7650) : 1 - ramp(t, 7650, 8475);
      setO("inEchoA", Math.max(0, e4) * 0.75);
      const de = t - 7725;
      let dd = 0;
      if (de >= 0) {
        dd = de < 1170 ? (Math.floor(de / 195) % 2 === 0 ? 0.9 : 0.25) : 0.6;
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
  const PL = L.left;
  const PR = L.right;
  const textPad = 13;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[440px]"
    >
      <div ref={wrapRef} className="flex min-h-0 flex-1 items-center">
        <svg
          viewBox={`0 0 ${lw} ${H}`}
          width="100%"
          height={(H * w) / lw}
          className="block"
        >
          <defs>
            <filter id="obxGlow" x="-300%" y="-300%" width="700%" height="700%">
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

          {/* ── left service panel ─────────────────────────────────── */}
          <rect
            x={PL.px}
            y={PL.py}
            width={L.pw}
            height={PANEL_H}
            rx={10}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={PL.px + 12}
            y={PL.py + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={DIM}
          >
            ORDERS SERVICE
          </text>

          {/* left TX frame */}
          <rect
            x={PL.fx}
            y={PL.fy}
            width={PL.fw}
            height={FRAME_H}
            rx={9}
            fill="rgba(139,160,188,0.04)"
          />
          <path
            d={PL.frameD}
            fill="none"
            stroke={FRAME_STROKE}
            strokeWidth={1}
            strokeDasharray="5 5"
          />
          <path
            ref={set("fxLg")}
            d={PL.frameD}
            fill="none"
            stroke={GREEN}
            strokeWidth={3}
            strokeDasharray="5 5"
            filter="url(#obxFrameGlow)"
            opacity={0}
          />
          <path
            ref={set("fxL")}
            d={PL.frameD}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.2}
            strokeDasharray="5 5"
            opacity={0}
          />
          <text
            x={PL.tagX}
            y={PL.fy + 2.5}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.12em"
            fill={DIM}
          >
            ONE TRANSACTION
          </text>

          {/* left ledger rows */}
          <g ref={set("r1g")} opacity={1}>
            <rect
              x={PL.rowX}
              y={PL.row1Y}
              width={PL.rowW}
              height={ROW_H}
              rx={6}
              fill={SURFACE}
              stroke={HAIR}
              strokeWidth={1}
            />
            <text
              x={PL.rowX + 9}
              y={PL.row1Y + 21}
              fontFamily={MONO_FONT}
              fontSize={L.rowFont}
            >
              {L_ROW1.map((s, i) => (
                <tspan key={i} ref={set(`r1s${i}`)} fill={s.f}>
                  {s.t}
                </tspan>
              ))}
            </text>
          </g>
          <g ref={set("r2g")} opacity={1}>
            <rect
              x={PL.rowX}
              y={PL.row2Y}
              width={PL.rowW}
              height={ROW_H}
              rx={6}
              fill={SURFACE}
              stroke={HAIR}
              strokeWidth={1}
            />
            <rect
              x={PL.rowX}
              y={PL.row2Y}
              width={PL.rowW}
              height={ROW_H}
              rx={6}
              fill={CORAL}
              opacity={0.07}
            />
            <rect
              x={PL.rowX}
              y={PL.row2Y + 5}
              width={3}
              height={ROW_H - 10}
              rx={1.5}
              fill={CORAL}
            />
            <text
              x={PL.rowX + textPad}
              y={PL.row2Y + 21}
              fontFamily={MONO_FONT}
              fontSize={L.rowFont}
            >
              {L_ROW2.map((s, i) => (
                <tspan key={i} ref={set(`r2s${i}`)} fill={s.f}>
                  {s.t}
                </tspan>
              ))}
            </text>
          </g>
          <rect
            ref={set("obEcho")}
            x={PL.rowX}
            y={PL.row2Y}
            width={PL.rowW}
            height={ROW_H}
            rx={6}
            fill="none"
            stroke={CORAL}
            strokeWidth={1.2}
            opacity={0}
          />

          {/* left COMMIT tag on the frame border */}
          <g ref={set("cmL")} opacity={0.92}>
            <rect
              x={PL.fx + PL.fw - 62}
              y={PL.fy + FRAME_H - 8}
              width={56}
              height={16}
              rx={4}
              fill={SURFACE}
              stroke={GREEN + "55"}
              strokeWidth={1}
            />
            <text
              x={PL.fx + PL.fw - 34}
              y={PL.fy + FRAME_H + 3.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.14em"
              fill={GREEN}
            >
              COMMIT
            </text>
          </g>

          {/* ── right service panel ────────────────────────────────── */}
          <rect
            x={PR.px}
            y={PR.py}
            width={L.pw}
            height={PANEL_H}
            rx={10}
            fill="rgba(139,160,188,0.03)"
            stroke={PANEL_STROKE}
            strokeWidth={1}
          />
          <text
            x={PR.px + 12}
            y={PR.py + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={DIM}
          >
            BILLING SERVICE
          </text>

          {/* right TX frame */}
          <rect
            x={PR.fx}
            y={PR.fy}
            width={PR.fw}
            height={FRAME_H}
            rx={9}
            fill="rgba(139,160,188,0.04)"
          />
          <path
            d={PR.frameD}
            fill="none"
            stroke={FRAME_STROKE}
            strokeWidth={1}
            strokeDasharray="5 5"
          />
          <path
            ref={set("fxRg")}
            d={PR.frameD}
            fill="none"
            stroke={GREEN}
            strokeWidth={3}
            strokeDasharray="5 5"
            filter="url(#obxFrameGlow)"
            opacity={0}
          />
          <path
            ref={set("fxR")}
            d={PR.frameD}
            fill="none"
            stroke={GREEN}
            strokeWidth={1.2}
            strokeDasharray="5 5"
            opacity={0}
          />
          <text
            x={PR.tagX}
            y={PR.fy + 2.5}
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.12em"
            fill={DIM}
          >
            ONE TRANSACTION
          </text>

          {/* inbox row */}
          <rect
            x={PR.rowX}
            y={PR.row1Y}
            width={PR.rowW}
            height={ROW_H}
            rx={6}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <rect
            x={PR.rowX}
            y={PR.row1Y}
            width={PR.rowW}
            height={ROW_H}
            rx={6}
            fill={CYAN}
            opacity={0.06}
          />
          <rect
            x={PR.rowX}
            y={PR.row1Y + 5}
            width={3}
            height={ROW_H - 10}
            rx={1.5}
            fill={CYAN}
          />
          <text
            x={PR.rowX + textPad}
            y={PR.row1Y + 21}
            fontFamily={MONO_FONT}
            fontSize={L.rowFont}
          >
            {R_ROW1.map((s, i) => (
              <tspan key={i} fill={s.f}>
                {s.t}
              </tspan>
            ))}
            <tspan ref={set("status")} fill={STATUS[1].f}>
              {STATUS[1].t}
            </tspan>
          </text>
          <rect
            ref={set("inEchoC")}
            x={PR.rowX}
            y={PR.row1Y}
            width={PR.rowW}
            height={ROW_H}
            rx={6}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />
          <rect
            ref={set("inEchoA")}
            x={PR.rowX}
            y={PR.row1Y}
            width={PR.rowW}
            height={ROW_H}
            rx={6}
            fill="none"
            stroke={AMBER}
            strokeWidth={1.2}
            opacity={0}
          />

          {/* handler row */}
          <rect
            x={PR.rowX}
            y={PR.row2Y}
            width={PR.rowW}
            height={ROW_H}
            rx={6}
            fill={SURFACE}
            stroke={HAIR}
            strokeWidth={1}
          />
          <text
            x={PR.rowX + 9}
            y={PR.row2Y + 21}
            fontFamily={MONO_FONT}
            fontSize={L.rowFont}
          >
            {R_ROW2.map((s, i) => (
              <tspan key={i} fill={s.f}>
                {s.t}
              </tspan>
            ))}
          </text>
          <rect
            ref={set("hEcho")}
            x={PR.rowX}
            y={PR.row2Y}
            width={PR.rowW}
            height={ROW_H}
            rx={6}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.2}
            opacity={0}
          />

          {/* right COMMIT tag on the frame border */}
          <g ref={set("cmR")} opacity={0.92}>
            <rect
              x={PR.fx + PR.fw - 62}
              y={PR.fy + FRAME_H - 8}
              width={56}
              height={16}
              rx={4}
              fill={SURFACE}
              stroke={GREEN + "55"}
              strokeWidth={1}
            />
            <text
              x={PR.fx + PR.fw - 34}
              y={PR.fy + FRAME_H + 3.5}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.14em"
              fill={GREEN}
            >
              COMMIT
            </text>
          </g>

          {/* dedupe note under the right panel */}
          <text
            ref={set("dedup")}
            x={PR.px + L.pw / 2}
            y={PR.py + PANEL_H + 14}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.14em"
            fill={DIM}
            opacity={0.5}
          >
            DEDUPED BY ID
          </text>

          {/* ── copper lane through the broker ─────────────────────── */}
          <path
            d={`M${L.x1} ${LANE_Y} H${L.x2}`}
            fill="none"
            stroke={LANE_STROKE}
            strokeWidth={1.5}
          />
          {[L.x1, L.x2].map((vx) => (
            <circle
              key={vx}
              cx={vx}
              cy={LANE_Y}
              r={2.5}
              fill={SURFACE}
              stroke="rgba(139,160,188,0.5)"
              strokeWidth={1}
            />
          ))}
          <text
            ref={set("redeliv")}
            x={L.midX}
            y={LANE_Y + 28}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={9}
            letterSpacing="0.04em"
            fill={AMBER}
            opacity={0}
          >
            redelivery · 7f3a
          </text>
          <circle
            ref={set("ring")}
            cx={L.x2}
            cy={LANE_Y}
            r={4}
            fill="none"
            stroke={CYAN}
            strokeWidth={1.5}
            opacity={0}
          />

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

          {/* broker chip drawn last so pulses dip underneath it */}
          <rect
            ref={set("chipGlow")}
            x={L.chipX}
            y={CHIP_Y}
            width={CHIP_W}
            height={26}
            rx={6}
            fill="none"
            stroke={VIOLET}
            strokeWidth={5}
            filter="url(#obxSoft)"
            opacity={0}
          />
          <rect
            x={L.chipX}
            y={CHIP_Y}
            width={CHIP_W}
            height={26}
            rx={6}
            fill={SURFACE}
            stroke={VIOLET + "59"}
            strokeWidth={1}
          />
          <rect
            ref={set("chipLit")}
            x={L.chipX}
            y={CHIP_Y}
            width={CHIP_W}
            height={26}
            rx={6}
            fill="none"
            stroke={VIOLET}
            strokeWidth={1}
            opacity={0}
          />
          <text
            x={L.chipX + CHIP_W / 2}
            y={LANE_Y + 3}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.1em"
            fill={VIOLET}
            opacity={0.85}
          >
            RABBITMQ
          </text>
          <text
            ref={set("chipLitTx")}
            x={L.chipX + CHIP_W / 2}
            y={LANE_Y + 3}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.1em"
            fill="#c7d2f0"
            opacity={0}
          >
            RABBITMQ
          </text>
        </svg>
      </div>
    </div>
  );
}
