"use client";

import { useEffect, useRef, useState } from "react";
import { CORAL, CORAL_SOFT, CYAN, MONO_FONT, NAVY, VIOLET } from "../palette";

const PERIOD = 3.2;
const DIAG_H = 150;
// layout(w) needs w >= ~383 or the chip gaps go negative; below this the
// diagram is laid out at MIN_LAYOUT_W and scaled down via the SVG viewBox.
const MIN_LAYOUT_W = 440;
const MAIN_Y = 75;
const REPLY_Y = 87;
const CHIP_H = 22;
const SEND_X = 26;

const F_START = 0.1;
const F_END = 1.5;
const FLASH_DUR = 0.6;
const R_START = 1.7;
const R_END = 2.8;
const R_FLASH_DUR = 0.5;

interface Chip {
  readonly label: string;
  readonly x: number;
  readonly w: number;
  readonly accent: string;
}

function chipWidth(label: string): number {
  return label.length * 7 + 24;
}

function layout(w: number) {
  const handlerW = chipWidth("HANDLER");
  const hRight = w - 26;
  const hLeft = hRight - handlerW;
  const hcx = hLeft + handlerW / 2;
  const mids = ["VALIDATE", "CACHE", "TELEMETRY"];
  const zoneL = SEND_X + 16;
  const zoneR = hLeft - 16;
  const totalMid = mids.reduce((a, s) => a + chipWidth(s), 0);
  const gap = (zoneR - zoneL - totalMid) / (mids.length + 1);
  const chips: Chip[] = [];
  let x = zoneL + gap;
  for (const label of mids) {
    const cw = chipWidth(label);
    chips.push({ label, x, w: cw, accent: CORAL });
    x += cw + gap;
  }
  chips.push({ label: "HANDLER", x: hLeft, w: handlerW, accent: CYAN });
  const vias = [
    (SEND_X + chips[0].x) / 2,
    (chips[0].x + chips[0].w + chips[1].x) / 2,
    (chips[1].x + chips[1].w + chips[2].x) / 2,
    (chips[2].x + chips[2].w + hLeft) / 2,
  ];
  // Reply trace: exits right of HANDLER, 45-degree drop to the lower lane,
  // then runs left to the REPLY via under the SEND pad.
  const hookX = hRight + 12;
  const replyPts: Array<[number, number]> = [
    [hcx, MAIN_Y],
    [hookX, MAIN_Y],
    [hookX - 12, REPLY_Y],
    [SEND_X, REPLY_Y],
  ];
  const segLens: number[] = [];
  let replyLen = 0;
  for (let i = 0; i < replyPts.length - 1; i++) {
    const dx = replyPts[i + 1][0] - replyPts[i][0];
    const dy = replyPts[i + 1][1] - replyPts[i][1];
    const len = Math.hypot(dx, dy);
    segLens.push(len);
    replyLen += len;
  }
  return { chips, vias, hookX, hcx, hLeft, replyPts, segLens, replyLen };
}

type Layout = ReturnType<typeof layout>;

function replyPointAt(l: Layout, d: number): [number, number] {
  let rest = Math.max(0, Math.min(d, l.replyLen));
  for (let i = 0; i < l.segLens.length; i++) {
    if (rest <= l.segLens[i] || i === l.segLens.length - 1) {
      const t = l.segLens[i] === 0 ? 0 : rest / l.segLens[i];
      const [x0, y0] = l.replyPts[i];
      const [x1, y1] = l.replyPts[i + 1];
      return [x0 + (x1 - x0) * t, y0 + (y1 - y0) * t];
    }
    rest -= l.segLens[i];
  }
  return l.replyPts[l.replyPts.length - 1];
}

const KW = { color: VIOLET };
const TY = { color: CYAN };
const MT = { color: CORAL };

export function MediatorVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const boxRef = useRef<HTMLDivElement>(null);
  const fwdRef = useRef<SVGGElement>(null);
  const fwdTrailRef = useRef<Array<SVGCircleElement | null>>([]);
  const replyRef = useRef<SVGGElement>(null);
  const replyTrailRef = useRef<Array<SVGCircleElement | null>>([]);
  const flashRef = useRef<SVGCircleElement>(null);
  const replyFlashRef = useRef<SVGCircleElement>(null);
  const litRef = useRef<Array<SVGGElement | null>>([]);
  const [w, setW] = useState(0);

  useEffect(() => {
    const el = boxRef.current;
    if (!el) {
      return;
    }
    const ro = new ResizeObserver((entries) => {
      const width = Math.round(entries[0]?.contentRect.width ?? 0);
      if (width > 0) {
        setW(width);
      }
    });
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  useEffect(() => {
    const root = rootRef.current;
    if (!root || w === 0) {
      return;
    }
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      // Static final frame: HANDLER stays lit (set in JSX defaults), no loop.
      return;
    }
    const l = layout(Math.max(w, MIN_LAYOUT_W));
    const glows = [0, 0, 0, 0];
    let raf = 0;
    let running = false;
    let inView = false;
    let phase = 0;
    let last = 0;

    const setOp = (el: SVGElement | null, o: number) => {
      if (el) {
        el.style.opacity = String(o);
      }
    };
    const setPos = (el: SVGGElement | null, x: number, y: number) => {
      el?.setAttribute("transform", `translate(${x} ${y})`);
    };

    const step = (now: number) => {
      if (!running) {
        return;
      }
      const dt = Math.min((now - last) / 1000, 0.05);
      last = now;
      phase = (phase + dt) % PERIOD;

      // forward pulse: SEND pad -> HANDLER on the main lane
      let fx = -1;
      if (phase >= F_START && phase <= F_END) {
        const p = (phase - F_START) / (F_END - F_START);
        fx = SEND_X + p * (l.hcx - SEND_X);
        setPos(fwdRef.current, fx, MAIN_Y);
        setOp(fwdRef.current, 1);
        const fade = Math.min(1, (fx - SEND_X) / 40);
        const trailOp = [0.3, 0.16, 0.07];
        fwdTrailRef.current.forEach((c, j) => {
          if (!c) {
            return;
          }
          c.setAttribute("cx", String(Math.max(SEND_X, fx - 9 * (j + 1))));
          c.setAttribute("cy", String(MAIN_Y));
          c.style.opacity = String(trailOp[j] * fade);
        });
      } else {
        setOp(fwdRef.current, 0);
        fwdTrailRef.current.forEach((c) => setOp(c, 0));
      }

      // arrival flash on HANDLER
      let fe = 0;
      if (phase >= F_END && phase <= F_END + FLASH_DUR) {
        const k = (phase - F_END) / FLASH_DUR;
        fe = 1 - k;
        if (flashRef.current) {
          flashRef.current.setAttribute("r", String(8 + k * 24));
          flashRef.current.style.opacity = String(0.4 * fe * fe);
        }
      } else {
        setOp(flashRef.current, 0);
      }

      // chip lighting while the pulse is inside
      for (let i = 0; i < l.chips.length; i++) {
        const c = l.chips[i];
        const inside = fx >= c.x - 2 && fx <= c.x + c.w + 2;
        glows[i] = inside
          ? Math.min(1, glows[i] + dt / 0.07)
          : Math.max(0, glows[i] - dt / 0.35);
        const el = litRef.current[i];
        if (i === 3) {
          setOp(el, 0.35 + 0.65 * Math.max(glows[3], fe));
        } else {
          setOp(el, glows[i] * 0.95);
        }
      }

      // reply pulse: HANDLER -> REPLY via on the lower lane
      if (phase >= R_START && phase <= R_END) {
        const q = (phase - R_START) / (R_END - R_START);
        const d = q * l.replyLen;
        const [px, py] = replyPointAt(l, d);
        setPos(replyRef.current, px, py);
        setOp(replyRef.current, 0.85);
        const trailOp = [0.18, 0.08];
        replyTrailRef.current.forEach((c, j) => {
          if (!c) {
            return;
          }
          const [tx, ty] = replyPointAt(l, Math.max(0, d - 7 * (j + 1)));
          c.setAttribute("cx", String(tx));
          c.setAttribute("cy", String(ty));
          c.style.opacity = String(trailOp[j]);
        });
      } else {
        setOp(replyRef.current, 0);
        replyTrailRef.current.forEach((c) => setOp(c, 0));
      }

      // arrival flash on the REPLY via
      if (phase >= R_END && phase <= R_END + R_FLASH_DUR) {
        const k = (phase - R_END) / R_FLASH_DUR;
        if (replyFlashRef.current) {
          replyFlashRef.current.setAttribute("r", String(3 + k * 11));
          replyFlashRef.current.style.opacity = String(0.3 * (1 - k) * (1 - k));
        }
      } else {
        setOp(replyFlashRef.current, 0);
      }

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
        inView = entries[0]?.isIntersecting ?? false;
        sync();
      },
      { threshold: 0.15 },
    );
    io.observe(root);
    const onVis = () => sync();
    document.addEventListener("visibilitychange", onVis);
    return () => {
      running = false;
      cancelAnimationFrame(raf);
      io.disconnect();
      document.removeEventListener("visibilitychange", onVis);
    };
  }, [w]);

  // Floor the layout width so the geometry never collapses on narrow stages;
  // the viewBox scales the whole diagram down to fit (identity at w >= MIN).
  const layoutW = Math.max(w, MIN_LAYOUT_W);
  const l = w > 0 ? layout(layoutW) : null;

  return (
    <div
      ref={rootRef}
      aria-hidden
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col gap-4 overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[400px]"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div
        className="border-cc-card-border/60 overflow-x-auto rounded-lg border bg-black/30 p-4 font-mono text-[12.5px] leading-[1.7]"
        style={{ scrollbarWidth: "none" }}
      >
        <div className="whitespace-pre">
          <div>
            <span style={KW}>var</span>
            <span className="text-cc-ink"> review </span>
            <span className="text-cc-ink-dim">= </span>
            <span style={KW}>await</span>
            <span className="text-cc-ink"> sender</span>
            <span className="text-cc-ink-dim">.</span>
            <span style={MT}>Send</span>
            <span className="text-cc-ink-dim">(</span>
            <span style={KW}>new</span>
            <span style={TY}> CreateReview</span>
            <span className="text-cc-ink-dim">(</span>
            <span className="text-cc-ink">id</span>
            <span className="text-cc-ink-dim">, </span>
            <span className="text-cc-ink">stars</span>
            <span className="text-cc-ink-dim">));</span>
          </div>
          <div>{" "}</div>
          <div>
            <span className="text-cc-ink-dim">
              {"// resolved at compile time - no reflection on the hot path"}
            </span>
          </div>
          <div>
            <span className="text-cc-ink-dim">[</span>
            <span style={TY}>Handler</span>
            <span className="text-cc-ink-dim">]</span>
          </div>
          <div>
            <span style={KW}>public static async</span>
            <span style={TY}> Task</span>
            <span className="text-cc-ink-dim">&lt;</span>
            <span style={TY}>Review</span>
            <span className="text-cc-ink-dim">&gt; </span>
            <span style={MT}>Handle</span>
            <span className="text-cc-ink-dim">(</span>
          </div>
          <div>
            <span className="text-cc-ink">{"    "}</span>
            <span style={TY}>CreateReview</span>
            <span className="text-cc-ink"> cmd</span>
            <span className="text-cc-ink-dim">, </span>
            <span style={TY}>ReviewService</span>
            <span className="text-cc-ink"> svc</span>
            <span className="text-cc-ink-dim">)</span>
          </div>
        </div>
      </div>

      <div ref={boxRef} className="flex min-h-0 flex-1 flex-col justify-center">
        {l && (
          <svg
            width={w}
            height={(DIAG_H * w) / layoutW}
            viewBox={`0 0 ${layoutW} ${DIAG_H}`}
            className="block"
            role="presentation"
          >
            {/* copper lanes */}
            <line
              x1={SEND_X}
              y1={MAIN_Y}
              x2={l.hcx}
              y2={MAIN_Y}
              stroke="rgba(139,160,188,0.4)"
              strokeWidth={1.5}
            />
            <polyline
              points={l.replyPts.map(([x, y]) => `${x},${y}`).join(" ")}
              fill="none"
              stroke="rgba(139,160,188,0.3)"
              strokeWidth={1.5}
              strokeLinejoin="miter"
            />

            {/* pulses run under the chips, like copper under components */}
            {[0, 1, 2].map((j) => (
              <circle
                key={j}
                ref={(el) => {
                  fwdTrailRef.current[j] = el;
                }}
                r={[1.8, 1.4, 1][j]}
                fill={CORAL}
                style={{ opacity: 0 }}
              />
            ))}
            <g ref={fwdRef} style={{ opacity: 0 }}>
              <circle r={7} fill={CORAL} opacity={0.16} />
              <circle r={2.5} fill={CORAL} />
              <circle r={1} fill="#ffd9d2" />
            </g>
            {[0, 1].map((j) => (
              <circle
                key={j}
                ref={(el) => {
                  replyTrailRef.current[j] = el;
                }}
                r={[1.4, 1][j]}
                fill={CORAL}
                style={{ opacity: 0 }}
              />
            ))}
            <g ref={replyRef} style={{ opacity: 0 }}>
              <circle r={5} fill={CORAL} opacity={0.12} />
              <circle r={2} fill={CORAL} opacity={0.9} />
            </g>

            {/* SEND pad */}
            <rect
              x={SEND_X - 5}
              y={MAIN_Y - 5}
              width={10}
              height={10}
              rx={2}
              fill="#0c1322"
              stroke="rgba(139,160,188,0.55)"
              strokeWidth={1}
            />
            <text
              x={SEND_X}
              y={MAIN_Y - 13}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={7.5}
              letterSpacing="0.2em"
              fill="#62748e"
            >
              SEND
            </text>

            {/* via rings */}
            {l.vias.map((vx) => (
              <circle
                key={vx}
                cx={vx}
                cy={MAIN_Y}
                r={2.5}
                fill={NAVY}
                stroke="rgba(139,160,188,0.5)"
                strokeWidth={1}
              />
            ))}
            <circle
              cx={l.hookX}
              cy={MAIN_Y}
              r={2.5}
              fill={NAVY}
              stroke="rgba(139,160,188,0.5)"
              strokeWidth={1}
            />
            <circle
              cx={SEND_X}
              cy={REPLY_Y}
              r={3}
              fill={NAVY}
              stroke="rgba(139,160,188,0.55)"
              strokeWidth={1}
            />
            <text
              x={SEND_X}
              y={REPLY_Y + 15}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={7.5}
              letterSpacing="0.2em"
              fill="#62748e"
            >
              REPLY
            </text>

            {/* chips */}
            {l.chips.map((c, i) => {
              const litText = c.accent === CYAN ? "#a5e3f6" : CORAL_SOFT;
              return (
                <g key={c.label}>
                  <rect
                    x={c.x}
                    y={MAIN_Y - CHIP_H / 2}
                    width={c.w}
                    height={CHIP_H}
                    rx={6}
                    fill="#0c1322"
                    stroke="rgba(139,160,188,0.28)"
                    strokeWidth={1}
                  />
                  <text
                    x={c.x + c.w / 2}
                    y={MAIN_Y + 3.3}
                    textAnchor="middle"
                    fontFamily={MONO_FONT}
                    fontSize={9.5}
                    letterSpacing="0.14em"
                    fill="#62748e"
                  >
                    {c.label}
                  </text>
                  <g
                    ref={(el) => {
                      litRef.current[i] = el;
                    }}
                    style={{ opacity: i === 3 ? 0.85 : 0 }}
                  >
                    <rect
                      x={c.x}
                      y={MAIN_Y - CHIP_H / 2}
                      width={c.w}
                      height={CHIP_H}
                      rx={6}
                      fill="none"
                      stroke={c.accent + "99"}
                      strokeWidth={1}
                      style={{ filter: `drop-shadow(0 0 7px ${c.accent}40)` }}
                    />
                    <text
                      x={c.x + c.w / 2}
                      y={MAIN_Y + 3.3}
                      textAnchor="middle"
                      fontFamily={MONO_FONT}
                      fontSize={9.5}
                      letterSpacing="0.14em"
                      fill={litText}
                    >
                      {c.label}
                    </text>
                  </g>
                </g>
              );
            })}

            {/* arrival flashes */}
            <circle
              ref={flashRef}
              cx={l.hcx}
              cy={MAIN_Y}
              r={0}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              style={{ opacity: 0 }}
            />
            <circle
              ref={replyFlashRef}
              cx={SEND_X}
              cy={REPLY_Y}
              r={0}
              fill="none"
              stroke={CORAL}
              strokeWidth={1}
              style={{ opacity: 0 }}
            />
          </svg>
        )}
      </div>

      <div className="text-cc-nav-label flex items-center gap-2 font-mono text-[0.6rem] tracking-[0.22em] uppercase">
        <span>Zero Reflection</span>
        <span className="opacity-40">&middot;</span>
        <span>AOT-Friendly</span>
        <span className="opacity-40">&middot;</span>
        <span>Middleware as Pipeline</span>
      </div>
    </div>
  );
}
