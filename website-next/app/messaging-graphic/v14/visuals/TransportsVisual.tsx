"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { CORAL, CYAN, MONO_FONT } from "../palette";

const PHASE = 2.8;
const LOOP = PHASE * 3;
const STAGE_H = 264;
const ROW_YS = [52, 132, 212];
const RAIL_Y = 132;
const CHIP_W = 112;
const CHIP_H = 26;
const HANDLER_W = 96;
// Minimum width the stage is laid out at; below this the whole stage
// (SVG lanes + HTML chips) is scaled down uniformly to fit.
const MIN_W = 460;
const PULSE_SPEED = 420;
const LABELS = ["RABBITMQ", "POSTGRES", "IN-PROCESS"];

const SLATE_RGB: readonly [number, number, number] = [139, 160, 188];
const CORAL_RGB: readonly [number, number, number] = [240, 120, 106];

interface RowGeometry {
  readonly y: number;
  readonly pts: ReadonlyArray<readonly [number, number]>;
  readonly cum: readonly number[];
  readonly len: number;
}

interface Layout {
  readonly x0: number;
  readonly sx1: number;
  readonly sx2: number;
  readonly convergeX: number;
  readonly jx: number;
  readonly railEnd: number;
  readonly bridgeLen: number;
  readonly rows: readonly RowGeometry[];
}

// `w` is the layout width and is always >= MIN_W, which guarantees
// railEnd clears the junction (jx + 28) and the handler chip.
function buildLayout(w: number): Layout {
  const x0 = CHIP_W + 6;
  const sx1 = x0 + 26;
  const sx2 = sx1 + 22;
  const convergeX = sx2 + 16;
  const jx = convergeX + 80;
  const railEnd = w - HANDLER_W - 6;
  const rows = ROW_YS.map((y) => {
    const pts: Array<readonly [number, number]> =
      y === RAIL_Y
        ? [
            [x0, y],
            [sx1, y],
            [sx2, y],
            [jx, RAIL_Y],
            [railEnd, RAIL_Y],
          ]
        : [
            [x0, y],
            [sx1, y],
            [sx2, y],
            [convergeX, y],
            [jx, RAIL_Y],
            [railEnd, RAIL_Y],
          ];
    const cum = [0];
    for (let i = 1; i < pts.length; i++) {
      const dx = pts[i][0] - pts[i - 1][0];
      const dy = pts[i][1] - pts[i - 1][1];
      cum.push(cum[i - 1] + Math.hypot(dx, dy));
    }
    return { y, pts, cum, len: cum[cum.length - 1] };
  });
  return {
    x0,
    sx1,
    sx2,
    convergeX,
    jx,
    railEnd,
    bridgeLen: sx2 - sx1 - 7,
    rows,
  };
}

function pointAt(row: RowGeometry, d: number): readonly [number, number] {
  const { pts, cum } = row;
  for (let i = 1; i < pts.length; i++) {
    if (d <= cum[i] || i === pts.length - 1) {
      const segLen = cum[i] - cum[i - 1] || 1;
      const f = Math.min(Math.max((d - cum[i - 1]) / segLen, 0), 1);
      return [
        pts[i - 1][0] + (pts[i][0] - pts[i - 1][0]) * f,
        pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * f,
      ];
    }
  }
  return pts[pts.length - 1];
}

function mixRgba(
  a: readonly [number, number, number],
  b: readonly [number, number, number],
  t: number,
  alpha: number,
): string {
  const r = Math.round(a[0] + (b[0] - a[0]) * t);
  const g = Math.round(a[1] + (b[1] - a[1]) * t);
  const bl = Math.round(a[2] + (b[2] - a[2]) * t);
  return `rgba(${r},${g},${bl},${alpha.toFixed(3)})`;
}

export function TransportsVisual() {
  const stageRef = useRef<HTMLDivElement>(null);
  const cardRef = useRef<HTMLDivElement>(null);
  const chipRefs = useRef<Array<HTMLDivElement | null>>([]);
  const handlerRef = useRef<HTMLDivElement>(null);
  const preRefs = useRef<Array<SVGLineElement | null>>([]);
  const postRefs = useRef<Array<SVGPathElement | null>>([]);
  const bridgeRefs = useRef<Array<SVGLineElement | null>>([]);
  const bridgeGlowRefs = useRef<Array<SVGLineElement | null>>([]);
  const viaFlashRefs = useRef<Array<SVGCircleElement | null>>([]);
  const pulseGroupRef = useRef<SVGGElement>(null);
  const pulseDotRef = useRef<SVGCircleElement>(null);
  const pulseGlowRef = useRef<SVGCircleElement>(null);
  const trailRefs = useRef<Array<SVGCircleElement | null>>([]);
  const arrivalRef = useRef<SVGCircleElement>(null);

  const [w, setW] = useState(0);
  const layoutW = Math.max(w, MIN_W);
  const scale = w > 0 ? w / layoutW : 1;
  const layout = useMemo(
    () => (w > 0 ? buildLayout(layoutW) : null),
    [w, layoutW],
  );

  useEffect(() => {
    const stage = stageRef.current;
    if (!stage) {
      return;
    }
    const ro = new ResizeObserver((entries) => {
      const width = Math.round(entries[0].contentRect.width);
      setW((current) => (current === width ? current : width));
    });
    ro.observe(stage);
    return () => ro.disconnect();
  }, []);

  useEffect(() => {
    const card = cardRef.current;
    if (!card || !layout) {
      return;
    }

    const styleChip = (el: HTMLDivElement | null, lit: number) => {
      if (!el) {
        return;
      }
      el.style.color = mixRgba(SLATE_RGB, CORAL_RGB, lit, 0.72 + 0.28 * lit);
      el.style.borderColor = mixRgba(
        SLATE_RGB,
        CORAL_RGB,
        lit,
        0.16 + 0.24 * lit,
      );
      el.style.boxShadow =
        lit > 0.02
          ? `0 0 14px rgba(240,120,106,${(0.2 * lit).toFixed(3)})`
          : "none";
    };

    const render = (t: number) => {
      const active = Math.floor(t / PHASE) % 3;
      const prev = (active + 2) % 3;
      const tau = t % PHASE;
      const c = Math.min(tau / 0.35, 1);
      const eased = c * c * (3 - 2 * c);

      for (let i = 0; i < 3; i++) {
        const lit = i === active ? eased : i === prev ? 1 - eased : 0;
        styleChip(chipRefs.current[i], lit);
        const laneStroke = `rgba(139,160,188,${(0.28 + 0.24 * lit).toFixed(3)})`;
        preRefs.current[i]?.setAttribute("stroke", laneStroke);
        postRefs.current[i]?.setAttribute("stroke", laneStroke);
        const bridge = bridgeRefs.current[i];
        if (bridge) {
          bridge.setAttribute(
            "stroke-dashoffset",
            (layout.bridgeLen * (1 - lit)).toFixed(2),
          );
          bridge.setAttribute("opacity", (0.35 + 0.65 * lit).toFixed(3));
        }
        bridgeGlowRefs.current[i]?.setAttribute(
          "opacity",
          (0.22 * lit).toFixed(3),
        );
        let flashOpacity = 0;
        let flashR = 3;
        if (i === active && tau < 0.5) {
          const u = tau / 0.5;
          flashOpacity = 0.8 * (1 - u);
          flashR = 3 + 7 * u;
        }
        for (const j of [i * 2, i * 2 + 1]) {
          const flash = viaFlashRefs.current[j];
          if (flash) {
            flash.setAttribute("opacity", flashOpacity.toFixed(3));
            flash.setAttribute("r", flashR.toFixed(2));
          }
        }
      }

      const row = layout.rows[active];
      const depart = 0.5;
      const duration = row.len / PULSE_SPEED;
      const tp = tau - depart;
      const group = pulseGroupRef.current;
      if (group) {
        if (tp >= 0 && tp <= duration) {
          group.setAttribute("opacity", "1");
          const d = Math.min(tp * PULSE_SPEED, row.len);
          const [px, py] = pointAt(row, d);
          pulseDotRef.current?.setAttribute("cx", px.toFixed(2));
          pulseDotRef.current?.setAttribute("cy", py.toFixed(2));
          pulseGlowRef.current?.setAttribute("cx", px.toFixed(2));
          pulseGlowRef.current?.setAttribute("cy", py.toFixed(2));
          const offsets = [6, 12, 18];
          const alphas = [0.35, 0.2, 0.1];
          for (let j = 0; j < offsets.length; j++) {
            const trail = trailRefs.current[j];
            if (!trail) {
              continue;
            }
            const td = d - offsets[j];
            if (td > 0) {
              const [tx, ty] = pointAt(row, td);
              trail.setAttribute("cx", tx.toFixed(2));
              trail.setAttribute("cy", ty.toFixed(2));
              trail.setAttribute("opacity", String(alphas[j]));
            } else {
              trail.setAttribute("opacity", "0");
            }
          }
        } else {
          group.setAttribute("opacity", "0");
        }
      }

      const ua = (tau - (depart + duration)) / 0.6;
      const arrival = arrivalRef.current;
      if (arrival) {
        if (ua >= 0 && ua < 1) {
          arrival.setAttribute("opacity", (0.85 * (1 - ua)).toFixed(3));
          arrival.setAttribute("r", (3 + 13 * ua).toFixed(2));
        } else {
          arrival.setAttribute("opacity", "0");
        }
      }
      if (handlerRef.current) {
        const bump = ua >= 0 && ua < 1 ? 0.05 * (1 - ua) : 0;
        handlerRef.current.style.boxShadow = `0 0 14px rgba(22,185,228,${(
          0.2 + bump
        ).toFixed(3)})`;
      }
    };

    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    if (reduced) {
      render(2.75);
      return;
    }

    let raf = 0;
    let running = false;
    let inView = false;
    let t = 0;
    let last = 0;

    const tick = (now: number) => {
      if (!running) {
        return;
      }
      const dt = Math.min((now - last) / 1000, 0.05);
      last = now;
      t = (t + dt) % LOOP;
      render(t);
      raf = requestAnimationFrame(tick);
    };
    const start = () => {
      if (running) {
        return;
      }
      running = true;
      last = performance.now();
      raf = requestAnimationFrame(tick);
    };
    const stop = () => {
      running = false;
      cancelAnimationFrame(raf);
    };
    const sync = () => {
      if (inView && !document.hidden) {
        start();
      } else {
        stop();
      }
    };
    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1].isIntersecting;
        sync();
      },
      { threshold: 0.15 },
    );
    io.observe(card);
    document.addEventListener("visibilitychange", sync);
    render(0);

    return () => {
      stop();
      io.disconnect();
      document.removeEventListener("visibilitychange", sync);
    };
  }, [layout]);

  const laneBase = "rgba(139,160,188,0.28)";
  const laneActive = "rgba(139,160,188,0.52)";
  const viaStroke = "rgba(139,160,188,0.55)";

  return (
    <div aria-hidden="true" className="w-full">
      <div
        ref={cardRef}
        className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[380px]"
      >
        <div
          ref={stageRef}
          className="relative w-full"
          style={{ height: STAGE_H * scale }}
        >
          {layout && (
            <div
              className="absolute top-0 left-0"
              style={{
                width: layoutW,
                height: STAGE_H,
                transform: `scale(${scale})`,
                transformOrigin: "top left",
              }}
            >
              <svg
                width={layoutW}
                height={STAGE_H}
                className="absolute inset-0"
                fill="none"
              >
                <defs>
                  <radialGradient id="tv14-pulse-glow">
                    <stop offset="0%" stopColor={CORAL} stopOpacity="0.4" />
                    <stop offset="100%" stopColor={CORAL} stopOpacity="0" />
                  </radialGradient>
                </defs>

                {/* common rail */}
                <line
                  x1={layout.jx}
                  y1={RAIL_Y}
                  x2={layout.railEnd}
                  y2={RAIL_Y}
                  stroke="rgba(139,160,188,0.46)"
                  strokeWidth={1.5}
                />
                <text
                  x={layout.railEnd - 4}
                  y={RAIL_Y - 16}
                  textAnchor="end"
                  fontSize={8}
                  letterSpacing={1.6}
                  fill="#62748e"
                  style={{ fontFamily: MONO_FONT }}
                >
                  SAME CONTRACT
                </text>

                {ROW_YS.map((y, i) => {
                  const outer = y !== RAIL_Y;
                  const postD = outer
                    ? `M ${layout.sx2} ${y} L ${layout.convergeX} ${y} L ${layout.jx} ${RAIL_Y}`
                    : `M ${layout.sx2} ${RAIL_Y} L ${layout.jx} ${RAIL_Y}`;
                  const stroke = i === 0 ? laneActive : laneBase;
                  return (
                    <g key={y}>
                      <line
                        ref={(el) => {
                          preRefs.current[i] = el;
                        }}
                        x1={layout.x0}
                        y1={y}
                        x2={layout.sx1}
                        y2={y}
                        stroke={stroke}
                        strokeWidth={1.5}
                      />
                      <path
                        ref={(el) => {
                          postRefs.current[i] = el;
                        }}
                        d={postD}
                        stroke={stroke}
                        strokeWidth={1.5}
                      />
                      {/* jumper strap */}
                      <line
                        ref={(el) => {
                          bridgeGlowRefs.current[i] = el;
                        }}
                        x1={layout.sx1 + 3.5}
                        y1={y}
                        x2={layout.sx2 - 3.5}
                        y2={y}
                        stroke={CORAL}
                        strokeWidth={5}
                        strokeLinecap="round"
                        opacity={i === 0 ? 0.22 : 0}
                      />
                      <line
                        ref={(el) => {
                          bridgeRefs.current[i] = el;
                        }}
                        x1={layout.sx1 + 3.5}
                        y1={y}
                        x2={layout.sx2 - 3.5}
                        y2={y}
                        stroke={CORAL}
                        strokeWidth={2}
                        strokeLinecap="round"
                        strokeDasharray={layout.bridgeLen}
                        strokeDashoffset={i === 0 ? 0 : layout.bridgeLen}
                        opacity={i === 0 ? 1 : 0.35}
                      />
                      {/* strap vias */}
                      {[layout.sx1, layout.sx2].map((sx, j) => (
                        <g key={sx}>
                          <circle
                            cx={sx}
                            cy={y}
                            r={2.5}
                            stroke={viaStroke}
                            strokeWidth={1.2}
                            fill="#0c1322"
                          />
                          <circle
                            ref={(el) => {
                              viaFlashRefs.current[i * 2 + j] = el;
                            }}
                            cx={sx}
                            cy={y}
                            r={3}
                            stroke={CORAL}
                            strokeWidth={1.2}
                            opacity={0}
                          />
                        </g>
                      ))}
                    </g>
                  );
                })}

                {/* junction via where the lanes meet the rail */}
                <circle
                  cx={layout.jx}
                  cy={RAIL_Y}
                  r={2.5}
                  stroke={viaStroke}
                  strokeWidth={1.2}
                  fill="#0c1322"
                />

                {/* message pulse */}
                <g ref={pulseGroupRef} opacity={0}>
                  {[0, 1, 2].map((j) => (
                    <circle
                      key={j}
                      ref={(el) => {
                        trailRefs.current[j] = el;
                      }}
                      r={2 - j * 0.4}
                      fill={CORAL}
                      opacity={0}
                    />
                  ))}
                  <circle
                    ref={pulseGlowRef}
                    r={8}
                    fill="url(#tv14-pulse-glow)"
                  />
                  <circle ref={pulseDotRef} r={2.5} fill={CORAL} />
                </g>

                {/* arrival flash at the handler */}
                <circle
                  ref={arrivalRef}
                  cx={layout.railEnd + 2}
                  cy={RAIL_Y}
                  r={3}
                  stroke={CORAL}
                  strokeWidth={1.5}
                  opacity={0}
                />
              </svg>

              {LABELS.map((label, i) => (
                <div
                  key={label}
                  ref={(el) => {
                    chipRefs.current[i] = el;
                  }}
                  className="bg-cc-surface absolute flex items-center justify-center rounded-md border px-2.5 font-mono text-[0.65rem] tracking-[0.14em] uppercase"
                  style={{
                    left: 0,
                    top: ROW_YS[i] - CHIP_H / 2,
                    width: CHIP_W,
                    height: CHIP_H,
                    color: i === 0 ? CORAL : "rgba(139,160,188,0.72)",
                    borderColor:
                      i === 0 ? `${CORAL}66` : "rgba(139,160,188,0.16)",
                    boxShadow: i === 0 ? `0 0 14px ${CORAL}33` : "none",
                  }}
                >
                  {label}
                </div>
              ))}

              <div
                ref={handlerRef}
                className="bg-cc-surface absolute flex items-center justify-center rounded-md border px-2.5 font-mono text-[0.65rem] tracking-[0.14em] uppercase"
                style={{
                  left: layoutW - HANDLER_W,
                  top: RAIL_Y - 15,
                  width: HANDLER_W,
                  height: 30,
                  color: CYAN,
                  borderColor: `${CYAN}66`,
                  boxShadow: `0 0 14px ${CYAN}33`,
                }}
              >
                HANDLER
              </div>
            </div>
          )}
        </div>

        <div className="mt-auto flex items-center gap-2.5">
          <span
            className="inline-block h-[3px] w-[14px] rounded-full"
            style={{ background: CORAL, opacity: 0.8 }}
          />
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            SWAP THE STRAP, NOT THE HANDLER
          </span>
        </div>
      </div>
    </div>
  );
}
