"use client";

import { useEffect, useRef, useState } from "react";
import { CORAL, CORAL_SOFT, CYAN, GREEN, MONO_FONT, NAVY } from "../palette";

// ASP.NET-style middleware pipeline. One ~6.5s loop: a coral request pulse
// enters at "SendAsync" and travels the upper lane left-to-right through
// TELEMETRY, TRANSACTION and VALIDATION (each panel brightens as the pulse
// passes and stays warm), reaches the HANDLER (arrival flash, panel and its
// boxed handler row glow), then a green result pulse departs on the lower
// lane right-to-left,
// re-brightening each panel on the way back (the logic-after-next() moment),
// and exits left where "PlaceOrderResult" flashes green. Calm beat, repeat.
const PERIOD = 6.5;
const F_START = 0.45; // request pulse leaves the SendAsync pad
const F_END = 2.5; // request reaches the HANDLER turnaround
const FLASH_DUR = 0.7;
const R_START = 3.3; // result pulse departs the HANDLER
const R_END = 5.2; // result exits at the left edge
const RET_FLASH_DUR = 0.7;
const TAG_FADE_START = 5.95; // PlaceOrderResult tag fades for the next loop

// Below MIN_W the four panels get too cramped, so the diagram is laid out at
// MIN_W and scaled down to fit via the SVG viewBox.
const MIN_W = 640;
const DIAG_H = 252;
const LANE_X = 14; // left edge of both copper lanes
const PANELS_X = 108; // gutter keeps room for the SendAsync / result labels
const MARGIN_R = 12;
const GAP = 22; // gap between panels, hosts the chevron arrowheads
const HANDLER_EXTRA = 126; // HANDLER is wider to fit its boxed handler row
const PANEL_Y = 24;
const PANEL_H = 204;
const REQ_Y = 92; // request lane, upper third of the panels
const RES_Y = 160; // response lane, lower third of the panels
const TAG_Y = PANEL_Y + 15;
const ROW_H = 32;
const ROW_Y = (REQ_Y + RES_Y) / 2 - ROW_H / 2; // handler row between the lanes
const BEND_INSET = 22; // turnaround bend, from the HANDLER's right edge
const BEND_R = 8; // 45-degree bend run

const MW_LABELS = ["TELEMETRY", "TRANSACTION", "VALIDATION"] as const;
const HANDLER_ROW = "PlaceOrderCommandHandler";

const SURFACE = "#0c1322";
const INK = "#a1a3af";
const HAIR = "rgba(139,160,188,0.22)";
const HANDLER_LIT = "#a5e3f6";
const PANEL_FILL = "rgba(139,160,188,0.03)";
const PANEL_STROKE = "rgba(158,176,204,0.44)";
const HANDLER_FILL = "rgba(22,185,228,0.04)";
const LANE_STROKE = "rgba(139,160,188,0.4)";
const VIA_STROKE = "rgba(164,180,208,0.55)";
const CHEV_STROKE = "rgba(139,160,188,0.6)";
const PAD_FILL = "rgba(158,176,204,0.34)";
const SILK = "rgba(154,172,200,0.75)";
const SILK_SOFT = "rgba(154,172,200,0.7)";
const GRID_DOT = "rgba(150,166,194,0.10)";

interface Panel {
  readonly label: string;
  readonly x: number;
  readonly w: number;
}

function layout(w: number) {
  const avail = w - PANELS_X - MARGIN_R - 3 * GAP;
  const mwW = Math.floor((avail - HANDLER_EXTRA) / 4);
  const hw = avail - 3 * mwW;
  const panels: Panel[] = MW_LABELS.map((label, i) => ({
    label,
    x: PANELS_X + i * (mwW + GAP),
    w: mwW,
  }));
  const hx = PANELS_X + 3 * (mwW + GAP);
  // The request lane runs into the HANDLER, U-turns with 45-degree bends and
  // comes back out as the response lane.
  const bx = hx + hw - BEND_INSET;
  // Boxed handler row inside the HANDLER panel, kept clear of the U-turn bend.
  const rowX = hx + 9;
  const rowW = bx - 6 - rowX;
  return {
    rowX,
    rowW,
    rowFont: Math.min(10, (rowW - 16) / (HANDLER_ROW.length * 0.635)),
    panels,
    hx,
    hw,
    hcx: hx + hw / 2,
    bx,
    laneD:
      `M ${LANE_X} ${REQ_Y} H ${bx} L ${bx + BEND_R} ${REQ_Y + BEND_R} ` +
      `V ${RES_Y - BEND_R} L ${bx} ${RES_Y} H ${LANE_X}`,
    // One chevron on the entry segment plus one in each inter-panel gap.
    chevXs: [
      (LANE_X + PANELS_X) / 2,
      ...panels.map((p) => p.x + p.w + GAP / 2),
    ],
  };
}

type Layout = ReturnType<typeof layout>;

function easeInOut(t: number): number {
  return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
}

function clamp01(v: number): number {
  return Math.max(0, Math.min(1, v));
}

export function MediatorVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const boxRef = useRef<HTMLDivElement>(null);
  const [els] = useState(() => new Map<string, SVGElement | null>());
  const [w, setW] = useState(0);

  useEffect(() => {
    const el = boxRef.current;
    if (!el) {
      return;
    }
    const ro = new ResizeObserver((entries) => {
      const width = Math.round(
        entries[entries.length - 1]?.contentRect.width ?? 0,
      );
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
      // Static frame: panels, lanes and chevrons drawn, HANDLER lit (JSX
      // defaults).
      return;
    }

    const l: Layout = layout(Math.max(w, MIN_W));
    const E = els;
    const warm = [0, 0, 0];
    let handlerGlow = 0;
    let raf = 0;
    let running = false;
    let inView = false;
    let phase = 0;
    let last = 0;

    const setO = (k: string, o: number) => {
      const el = E.get(k);
      if (el) {
        el.style.opacity = String(o);
      }
    };
    const setPos = (k: string, x: number, y: number) => {
      E.get(k)?.setAttribute("transform", `translate(${x} ${y})`);
    };

    const paint = (dt: number) => {
      const fwdActive = phase >= F_START && phase < F_END;
      const retActive = phase >= R_START && phase <= R_END;
      let px = -1e4;
      let gx = 1e4;
      if (fwdActive) {
        const q = easeInOut((phase - F_START) / (F_END - F_START));
        px = LANE_X + q * (l.bx - LANE_X);
      }
      if (retActive) {
        const q = easeInOut((phase - R_START) / (R_END - R_START));
        gx = l.bx - q * (l.bx - LANE_X);
      }
      // How far the request has come this loop, and how far the result has
      // gone back out. Panels warm up behind the request, then cool down
      // behind the result on the return pass.
      const reachX = fwdActive ? px : phase >= F_END ? l.bx : -1e4;
      const greenX = retActive ? gx : phase > R_END ? -1e4 : 1e4;

      // coral request pulse on the upper lane
      if (fwdActive) {
        setPos("fwd", px, REQ_Y);
        setO("fwd", Math.min(1, (px - LANE_X) / 25));
        const trailOp = [0.3, 0.16, 0.07];
        for (let j = 0; j < 3; j++) {
          const c = E.get(`fwdT${j}`);
          if (c) {
            c.setAttribute("cx", String(Math.max(LANE_X, px - 9 * (j + 1))));
            c.setAttribute("cy", String(REQ_Y));
            c.style.opacity = String(
              trailOp[j] * Math.min(1, (px - LANE_X) / 25),
            );
          }
        }
      } else {
        setO("fwd", 0);
        for (let j = 0; j < 3; j++) {
          setO(`fwdT${j}`, 0);
        }
      }

      // green result pulse on the lower lane
      if (retActive) {
        setPos("ret", gx, RES_Y);
        setO("ret", 0.95 * Math.min(1, (l.bx - gx) / 16));
        const trailOp = [0.2, 0.09];
        for (let j = 0; j < 2; j++) {
          const c = E.get(`retT${j}`);
          if (c) {
            c.setAttribute("cx", String(Math.min(l.bx, gx + 8 * (j + 1))));
            c.setAttribute("cy", String(RES_Y));
            c.style.opacity = String(trailOp[j]);
          }
        }
      } else {
        setO("ret", 0);
        for (let j = 0; j < 2; j++) {
          setO(`retT${j}`, 0);
        }
      }

      // panels brighten as the request enters, stay warm once it has passed,
      // re-brighten while the result passes back through, then cool behind it
      for (let i = 0; i < 3; i++) {
        const x0 = l.panels[i].x;
        const x1 = x0 + l.panels[i].w;
        let target: number;
        if (greenX <= x0 - 2) {
          target = 0;
        } else if (greenX <= x1 + 2) {
          target = 1;
        } else if (reachX >= x1 + 2) {
          target = 0.55;
        } else if (reachX >= x0 + 2) {
          target = 1;
        } else {
          target = 0;
        }
        warm[i] =
          target > warm[i]
            ? Math.min(target, warm[i] + dt / 0.07)
            : Math.max(target, warm[i] - dt / 0.45);
        setO(`warmB${i}`, warm[i] * 0.85);
        setO(`warmL${i}`, warm[i]);
      }

      // arrival flash at the HANDLER turnaround
      let fe = 0;
      if (phase >= F_END && phase <= F_END + FLASH_DUR) {
        const k = (phase - F_END) / FLASH_DUR;
        fe = 1 - k;
        const flash = E.get("flash");
        if (flash) {
          flash.setAttribute("r", String(6 + k * 22));
          flash.style.opacity = String(0.4 * fe * fe);
        }
      } else {
        setO("flash", 0);
      }

      // HANDLER panel glows from request arrival until the result leaves it
      const hTarget = reachX >= l.hx + 8 && greenX >= l.hx ? 1 : 0;
      handlerGlow = hTarget
        ? Math.min(1, handlerGlow + dt / 0.08)
        : Math.max(0, handlerGlow - dt / 0.5);
      setO("hlit", 0.3 + 0.7 * Math.max(handlerGlow, fe));

      // result exits: green flash on the pad + PlaceOrderResult tag
      if (phase >= R_END && phase <= R_END + RET_FLASH_DUR) {
        const k = (phase - R_END) / RET_FLASH_DUR;
        const flash = E.get("padFlash");
        if (flash) {
          flash.setAttribute("r", String(3 + k * 11));
          flash.style.opacity = String(0.35 * (1 - k) * (1 - k));
        }
      } else {
        setO("padFlash", 0);
      }
      const tagIn = clamp01((phase - (R_END - 0.05)) / 0.2);
      const tagOut =
        1 - clamp01((phase - TAG_FADE_START) / (PERIOD - TAG_FADE_START));
      setO("rtag", Math.min(tagIn, tagOut));
    };

    const step = (now: number) => {
      if (!running) {
        return;
      }
      const dt = Math.min((now - last) / 1000, 0.05);
      last = now;
      phase = (phase + dt) % PERIOD;
      paint(dt);
      raf = requestAnimationFrame(step);
    };

    // paint the phase-0 frame so the JSX static defaults never flash
    paint(0);

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
        inView = entries[entries.length - 1]?.isIntersecting ?? false;
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
  }, [w, els]);

  const set = (k: string) => (node: SVGElement | null) => {
    els.set(k, node);
  };

  // Floor the layout width so the pipeline never collapses on narrow stages;
  // the viewBox scales the whole diagram down to fit (identity at w >= MIN_W).
  const layoutW = Math.max(w, MIN_W);
  const l = w > 0 ? layout(layoutW) : null;

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[320px]"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div ref={boxRef} className="flex min-h-0 flex-1 flex-col justify-center">
        {l && (
          <svg
            width={w}
            height={(DIAG_H * w) / layoutW}
            viewBox={`0 0 ${layoutW} ${DIAG_H}`}
            className="block"
            role="presentation"
          >
            <defs>
              <pattern
                id="mediator-pcb-grid"
                width={28}
                height={28}
                patternUnits="userSpaceOnUse"
              >
                <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
              </pattern>
            </defs>

            {/* substrate: faint pad-dot grid behind everything */}
            <rect
              width={layoutW}
              height={DIAG_H}
              fill="url(#mediator-pcb-grid)"
            />

            {/* copper lanes: request in at the top, U-turn inside the
                HANDLER, response back out at the bottom */}
            <path
              d={l.laneD}
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
                strokeWidth={1}
              />
            ))}

            {/* chevron arrowheads: right on the request lane, left on the
                response lane */}
            {l.chevXs.map((cx) => (
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

            {/* middleware panels spanning both lanes */}
            {l.panels.map((p) => (
              <rect
                key={`panel-${p.label}`}
                x={p.x}
                y={PANEL_Y}
                width={p.w}
                height={PANEL_H}
                rx={3}
                fill={PANEL_FILL}
                stroke={PANEL_STROKE}
                strokeWidth={1}
              />
            ))}

            {/* HANDLER panel, the last element of the chain */}
            <rect
              x={l.hx}
              y={PANEL_Y}
              width={l.hw}
              height={PANEL_H}
              rx={3}
              fill={HANDLER_FILL}
              stroke={PANEL_STROKE}
              strokeWidth={1}
            />

            {/* pin-1 dots inside each package's top-left corner */}
            {[...l.panels.map((p) => p.x), l.hx].map((ex) => (
              <circle
                key={`pin1-${ex}`}
                cx={ex + 5.5}
                cy={PANEL_Y + 5.5}
                r={1.2}
                fill={SILK}
              />
            ))}

            {/* pads where the lanes cross each package edge */}
            {[...l.panels.flatMap((p) => [p.x, p.x + p.w]), l.hx].map((ex) =>
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

            {/* panel micro-tags */}
            {l.panels.map((p, i) => (
              <g key={`tag-${p.label}`}>
                <text
                  x={p.x + p.w / 2}
                  y={TAG_Y}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  letterSpacing="0.04em"
                  fill={SILK}
                >
                  {p.label}
                </text>
                <text
                  ref={set(`warmL${i}`)}
                  x={p.x + p.w / 2}
                  y={TAG_Y}
                  textAnchor="middle"
                  fontFamily={MONO_FONT}
                  fontSize={10}
                  letterSpacing="0.04em"
                  fill={CORAL_SOFT}
                  style={{ opacity: 0 }}
                >
                  {p.label}
                </text>
              </g>
            ))}
            <text
              x={l.hcx}
              y={TAG_Y}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={10}
              letterSpacing="0.2em"
              fill={CYAN}
              opacity={0.75}
            >
              HANDLER
            </text>
            {/* boxed handler row inside the HANDLER panel */}
            <rect
              x={l.rowX}
              y={ROW_Y}
              width={l.rowW}
              height={ROW_H}
              rx={5}
              fill={SURFACE}
              stroke={HAIR}
              strokeWidth={1}
            />
            <rect
              x={l.rowX}
              y={ROW_Y + 5}
              width={3}
              height={ROW_H - 10}
              rx={1.5}
              fill={CYAN}
            />
            <text
              x={l.rowX + 13}
              y={ROW_Y + 20}
              fontFamily={MONO_FONT}
              fontSize={l.rowFont}
              fill={INK}
            >
              {HANDLER_ROW}
            </text>

            {/* warm border overlays: lit while a pulse is inside a panel */}
            {l.panels.map((p, i) => (
              <rect
                key={`warm-${p.label}`}
                ref={set(`warmB${i}`)}
                x={p.x}
                y={PANEL_Y}
                width={p.w}
                height={PANEL_H}
                rx={3}
                fill="none"
                stroke={CORAL}
                strokeWidth={1.2}
                style={{
                  opacity: 0,
                  filter: `drop-shadow(0 0 6px ${CORAL}66)`,
                }}
              />
            ))}

            {/* lane labels at the left edge */}
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
              style={{ opacity: 0.45 }}
            >
              PlaceOrderResult
            </text>

            {/* pulses ride on top of the translucent panels */}
            {[0, 1, 2].map((j) => (
              <circle
                key={`fwd-trail-${j}`}
                ref={set(`fwdT${j}`)}
                r={[1.8, 1.4, 1][j]}
                fill={CORAL}
                style={{ opacity: 0 }}
              />
            ))}
            <g ref={set("fwd")} style={{ opacity: 0 }}>
              <circle r={7} fill={CORAL} opacity={0.16} />
              <circle r={2.5} fill={CORAL} />
              <circle r={1} fill={CORAL_SOFT} />
            </g>
            {[0, 1].map((j) => (
              <circle
                key={`ret-trail-${j}`}
                ref={set(`retT${j}`)}
                r={[1.4, 1][j]}
                fill={GREEN}
                style={{ opacity: 0 }}
              />
            ))}
            <g ref={set("ret")} style={{ opacity: 0 }}>
              <circle r={5} fill={GREEN} opacity={0.14} />
              <circle r={2.2} fill={GREEN} opacity={0.9} />
              <circle r={0.9} fill="#c9f7e4" />
            </g>

            {/* arrival and exit flashes */}
            <circle
              ref={set("flash")}
              cx={l.bx}
              cy={REQ_Y}
              r={0}
              fill="none"
              stroke={CORAL}
              strokeWidth={1.5}
              style={{ opacity: 0 }}
            />
            <circle
              ref={set("padFlash")}
              cx={LANE_X}
              cy={RES_Y}
              r={0}
              fill="none"
              stroke={GREEN}
              strokeWidth={1}
              style={{ opacity: 0 }}
            />

            {/* HANDLER lit overlay, drawn last so its glow sits on top */}
            <g ref={set("hlit")} style={{ opacity: 0.85 }}>
              <rect
                x={l.hx}
                y={PANEL_Y}
                width={l.hw}
                height={PANEL_H}
                rx={3}
                fill="none"
                stroke={`${CYAN}99`}
                strokeWidth={1}
                style={{ filter: `drop-shadow(0 0 7px ${CYAN}40)` }}
              />
              <text
                x={l.hcx}
                y={TAG_Y}
                textAnchor="middle"
                fontFamily={MONO_FONT}
                fontSize={10}
                letterSpacing="0.2em"
                fill={HANDLER_LIT}
              >
                HANDLER
              </text>
              <rect
                x={l.rowX}
                y={ROW_Y}
                width={l.rowW}
                height={ROW_H}
                rx={5}
                fill={CYAN}
                opacity={0.07}
              />
              <rect
                x={l.rowX}
                y={ROW_Y}
                width={l.rowW}
                height={ROW_H}
                rx={5}
                fill="none"
                stroke={CYAN}
                strokeWidth={1.2}
                opacity={0.8}
              />
              <text
                x={l.rowX + 13}
                y={ROW_Y + 20}
                fontFamily={MONO_FONT}
                fontSize={l.rowFont}
                fill={HANDLER_LIT}
              >
                {HANDLER_ROW}
              </text>
            </g>
          </svg>
        )}
      </div>
    </div>
  );
}
