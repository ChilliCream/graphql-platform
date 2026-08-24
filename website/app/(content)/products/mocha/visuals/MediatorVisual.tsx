"use client";

import { useEffect, useMemo, useRef, useState } from "react";

import {
  type Polyline,
  clamp01,
  easeInOutCubic,
  easeOutCubic,
  measure,
  pointAt,
  ramp,
} from "@/src/components/mocha/geometry";
import { CORAL, CORAL_SOFT, CYAN, GREEN, MONO_FONT, NAVY } from "@/src/components/mocha/palette";
import { type Pin, PinRow } from "@/src/components/mocha/PinRow";
import { useElementRegistry } from "@/src/components/mocha/useElementRegistry";
import { useRafLoop } from "@/src/components/mocha/useRafLoop";

const T = 7200;
const REST_T = 6350;
const REQ_DEP = 400;
const REQ_ARR = 2600;
const RES_DEP = 3600;
const RES_ARR = 5600;

const MIN_W = 640;
const H = 252;
const LANE_X = 14;
const PANELS_X = 108;
const MARGIN_R = 12;
const GAP = 22;
const HANDLER_EXTRA = 126;
const PANEL_Y = 24;
const PANEL_H = 204;
const REQ_Y = 92;
const RES_Y = 160;
const ROW_H = 32;
const ROW_Y = (REQ_Y + RES_Y) / 2 - ROW_H / 2;

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
    req: measure([
      [LANE_X, REQ_Y],
      [hx, REQ_Y],
    ]),
    res: measure([
      [hx, RES_Y],
      [LANE_X, RES_Y],
    ]),
    chevXs: [(LANE_X + PANELS_X) / 2, ...panels.map((p) => p.x + p.w + GAP / 2)],
    crossXs: panels.flatMap((p) => [p.x, p.x + p.w]),
    pins: [
      { x: hx, y: REQ_Y, side: "left" },
      { x: hx, y: RES_Y, side: "left" },
    ],
  };
}

export function MediatorVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { els, set } = useElementRegistry();
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

  useRafLoop(
    rootRef,
    () => {
      const E = els;

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

      const warm = [0, 0, 0];

      const apply = (t: number, dt: number) => {
        const L = layoutRef.current;

        let px = -1e4;
        if (t >= REQ_DEP && t < REQ_ARR) {
          const u = easeInOutCubic(ramp(t, REQ_DEP, REQ_ARR));
          px = pointAt(L.req, u)[0];
          placePulse("rq", L.req, u, Math.min((t - REQ_DEP) / 150, 1) * (1 - ramp(t, REQ_ARR - 160, REQ_ARR)));
        } else {
          placePulse("rq", L.req, 0, 0);
        }
        setRing("rga", ((t - REQ_ARR + T) % T) / 700, 3, 11);

        let gx = 1e4;
        if (t >= RES_DEP && t < RES_ARR) {
          const u = easeInOutCubic(ramp(t, RES_DEP, RES_ARR));
          gx = pointAt(L.res, u)[0];
          placePulse("rs", L.res, u, Math.min((t - RES_DEP) / 150, 1) * (1 - ramp(t, RES_ARR - 160, RES_ARR)));
        } else {
          placePulse("rs", L.res, 0, 0);
        }
        setRing("rgx", ((t - RES_ARR + T) % T) / 700, 3, 11);

        const reachX = t >= REQ_ARR ? L.hx : px;
        const greenX = t >= RES_ARR ? -1e4 : gx;

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
          warm[i] = target > warm[i] ? Math.min(target, warm[i] + dt / 70) : Math.max(target, warm[i] - dt / 450);
          setO(`pw${i}`, warm[i] * 0.07);
          setO(`pe${i}`, warm[i] * 0.55);
          setO(`pl${i}`, warm[i] * 0.9);
          setO(`ph${i}`, warm[i] * 0.5);
        }

        const hact =
          easeOutCubic(ramp(t, REQ_ARR - 40, REQ_ARR + 220)) *
          (1 - easeInOutCubic(ramp(t, RES_DEP + 150, RES_DEP + 800)));
        setO("hcw", hact * 0.07);
        setO("hce", hact * 0.55);
        setO("plc", hact * 0.9);
        setO("phc", hact * 0.5);
        setO("hrow", hact * 0.9);

        setO("rtag", ramp(t, RES_ARR - 100, RES_ARR + 150) * (1 - ramp(t, 6500, 7100)));
      };

      apply(0, 0);

      return { frame: apply, rest: () => apply(REST_T, 0) };
    },
    { period: T },
  );

  const pulseGlyph = (p: string, color: string, inner: string) => (
    <g key={p} ref={set(p)} opacity={0}>
      <circle ref={set(p + "t2")} r={1.6} fill={color} opacity={0} />
      <circle ref={set(p + "t1")} r={2} fill={color} opacity={0} />
      <circle ref={set(p + "glow")} r={6} fill={color} opacity={0.2} filter="url(#mediator-soft)" />
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
        <svg viewBox={`0 0 ${lw} ${H}`} width="100%" height={(H * w) / lw} className="block">
          <defs>
            <filter id="mediator-soft" x="-60%" y="-60%" width="220%" height="220%">
              <feGaussianBlur stdDeviation="2.4" />
            </filter>
            <pattern id="mediator-grid" width={28} height={28} patternUnits="userSpaceOnUse">
              <circle cx={14} cy={14} r={0.8} fill={GRID_DOT} />
            </pattern>
          </defs>

          <rect x={0} y={0} width={lw} height={H} fill="url(#mediator-grid)" />

          <path d={`M${LANE_X} ${REQ_Y} H${L.hx}`} fill="none" stroke={LANE_STROKE} strokeWidth={1.5} />
          <path d={`M${LANE_X} ${RES_Y} H${L.hx}`} fill="none" stroke={LANE_STROKE} strokeWidth={1.5} />

          {[REQ_Y, RES_Y].map((vy) => (
            <circle key={`via-${vy}`} cx={LANE_X} cy={vy} r={2.5} fill={NAVY} stroke={VIA_STROKE} strokeWidth={1.2} />
          ))}

          {L.chevXs.map((cx) => (
            <g
              key={`chev-${cx}`}
              fill="none"
              stroke={CHEV_STROKE}
              strokeWidth={1.3}
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d={`M ${cx - 3} ${REQ_Y - 3.4} L ${cx + 2.8} ${REQ_Y} L ${cx - 3} ${REQ_Y + 3.4}`} />
              <path d={`M ${cx + 3} ${RES_Y - 3.4} L ${cx - 2.8} ${RES_Y} L ${cx + 3} ${RES_Y + 3.4}`} />
            </g>
          ))}

          {L.crossXs.map((ex) =>
            [REQ_Y, RES_Y].map((py) => (
              <rect key={`pad-${ex}-${py}`} x={ex - 1} y={py - 1.75} width={2} height={3.5} fill={PAD_FILL} />
            )),
          )}

          {L.pins.map((pin, i) => (
            <PinRow key={`pin${i}`} pin={pin} />
          ))}

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
              <circle cx={p.x + p.w - 10} cy={PANEL_Y + 10} r={2} fill={SILK} opacity={0.25} />
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
              <circle ref={set(`pl${i}`)} cx={p.x + p.w - 10} cy={PANEL_Y + 10} r={2} fill={CORAL} opacity={0} />
            </g>
          ))}

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
            <rect ref={set("hcw")} x={L.hx} y={PANEL_Y} width={L.hw} height={PANEL_H} rx={3} fill={CYAN} opacity={0} />
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
            <rect x={L.rowX} y={ROW_Y + 5} width={3} height={ROW_H - 10} rx={1.5} fill={CYAN} />
            <text x={L.rowX + 13} y={ROW_Y + 20} fontFamily={MONO_FONT} fontSize={L.rowFont} fill={INK}>
              {HANDLER_ROW}
            </text>
            <g ref={set("hrow")} opacity={0.35}>
              <rect x={L.rowX} y={ROW_Y} width={L.rowW} height={ROW_H} rx={5} fill={CYAN} opacity={0.07} />
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
              <text x={L.rowX + 13} y={ROW_Y + 20} fontFamily={MONO_FONT} fontSize={L.rowFont} fill={HANDLER_LIT}>
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
            <circle cx={L.hx + L.hw - 10} cy={PANEL_Y + 10} r={2} fill={SILK} opacity={0.25} />
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
            <circle ref={set("plc")} cx={L.hx + L.hw - 10} cy={PANEL_Y + 10} r={2} fill={CYAN} opacity={0} />
          </g>

          <text x={10} y={REQ_Y - 11} fontFamily={MONO_FONT} fontSize={9} letterSpacing="0.02em" fill={SILK_SOFT}>
            SendAsync
          </text>
          <text x={10} y={RES_Y + 20} fontFamily={MONO_FONT} fontSize={9} letterSpacing="0.02em" fill={SILK_SOFT}>
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

          {pulseGlyph("rq", CORAL, CORAL_SOFT)}
          {pulseGlyph("rs", GREEN, "#a7f3d0")}
        </svg>
      </div>
    </div>
  );
}
