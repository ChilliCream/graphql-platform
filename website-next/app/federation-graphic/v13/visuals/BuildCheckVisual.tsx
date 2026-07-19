"use client";

/**
 * Broken graphs fail the build, not the client: on main catalog and billing
 * both expose Product.price as Float! @shareable. Billing's pull request #482
 * moves its side to Money!, so the branch's fusion compose check fails with
 * the exact type conflict and exit 1 deploys nothing. Once the coordinated
 * catalog change lands in #483 the rerun composes cleanly and publishes
 * composite v42; the branch's +price line settles green as that fix lands.
 * Clients kept querying v41 the whole time. Every line is present in the
 * static frame, so a frozen frame shows the complete before/after transcript.
 */

import { MONO_FONT } from "../palette";
import { easeOutCubic, ramp, useVisual } from "./anim";
import { DASHED, GatewayChip, INK_DIM } from "./stage";

const T = 11000;

const PR = { x: 40, y: 102, w: 300, h: 126 } as const;
const TERM = { x: 390, y: 40, w: 470, h: 250 } as const;

const CMD = "#c9d1d9";
const PROMPT = "#8b949e";
const ERR = "#f27765";
const OK = "#66be77";
const ARTIFACT = "#a5d6ff";

interface Seg {
  readonly t: string;
  readonly fill: string;
}

interface Row {
  readonly segs: readonly Seg[];
  readonly op?: number;
  /** Draw a small arrow glyph before the row's text. */
  readonly arrow?: boolean;
}

const ROWS: readonly Row[] = [
  {
    segs: [
      { t: "$ ", fill: PROMPT },
      { t: "nitro fusion compose", fill: CMD },
      { t: "  # check · branch billing #482", fill: INK_DIM },
    ],
  },
  { segs: [{ t: "✕ OUTPUT_FIELD_TYPES_NOT_MERGEABLE", fill: ERR }] },
  {
    segs: [
      {
        t: "  Product.price: Money! (billing) ≠ Float! (catalog)",
        fill: INK_DIM,
      },
    ],
  },
  { segs: [{ t: "✕ exit 1 · nothing deployed", fill: ERR }], op: 0.72 },
  { segs: [] },
  {
    segs: [
      { t: "$ ", fill: PROMPT },
      { t: "nitro fusion compose", fill: CMD },
      { t: "  # after catalog PR #483 merged", fill: INK_DIM },
    ],
  },
  { segs: [{ t: "✓ composed 5 source schemas · 0 errors", fill: OK }] },
  {
    segs: [{ t: "gateway.far · composite v42", fill: ARTIFACT }],
    arrow: true,
  },
];

/** Pop-in window per terminal row; null rows never replay. */
const POP: readonly (readonly [number, number] | null)[] = [
  [700, 1050],
  [1400, 1750],
  [1800, 2150],
  [2300, 2650],
  null,
  [4800, 5150],
  [5600, 5950],
  [6300, 6650],
];

function rowY(i: number): number {
  return TERM.y + 58 + i * 22;
}

interface DiffLine {
  readonly code: string;
  readonly fill: string;
  readonly bg?: string;
}

const DIFF: readonly DiffLine[] = [
  { code: "type Product {", fill: "#c9d4e8" },
  {
    code: "-   price: Float! @shareable",
    fill: ERR,
    bg: "rgba(242,119,101,0.08)",
  },
  {
    code: "+   price: Money! @shareable",
    fill: OK,
    bg: "rgba(102,190,119,0.09)",
  },
  { code: "}", fill: "#c9d4e8" },
];

function diffY(i: number): number {
  return PR.y + 52 + i * 20;
}

export function BuildCheckVisual() {
  const { rootRef, set } = useVisual(T, (t, h) => {
    // The transcript replays: rows dim out fast, then type back one by one.
    const dim = ramp(t, 250, 550);
    ROWS.forEach((row, i) => {
      const win = POP[i];
      if (!win || row.segs.length === 0) {
        return;
      }
      // Rows only recede while waiting to retype; the transcript stays
      // readable in every frame.
      const pop = easeOutCubic(ramp(t, win[0], win[1]));
      const v = 1 - 0.6 * dim * (1 - pop);
      h.setPop(`ln${i}`, v, v);
    });

    // The PR's green line is the fix: one soft pulse between fail and pass.
    h.setO("fixping", 0.3 * Math.sin(Math.PI * ramp(t, 3400, 4200)));

    // A brief green ring around the freshly published composite version.
    h.setO("v42glow", 0.55 * Math.sin(Math.PI * ramp(t, 6900, 7600)));

    // The cursor blinks throughout.
    h.setO("cursor", t % 900 < 470 ? 0.9 : 0.08);
  });

  return (
    <div ref={rootRef} aria-hidden="true" className="w-full">
      {/* Phones: the same story as a compact stacked column, code kept legible. */}
      <div className="space-y-3 sm:hidden">
        <div className="rounded-xl border border-[rgba(245,241,234,0.13)] bg-[rgba(12,19,34,0.5)] p-4">
          <div
            className="font-mono text-[10px] tracking-[0.16em] uppercase"
            style={{ color: INK_DIM }}
          >
            Pull request · billing #482
          </div>
          <div className="mt-2 space-y-0.5 overflow-x-auto border-t border-[rgba(245,241,234,0.1)] pt-2 font-mono text-[12px] leading-6">
            {DIFF.map((l, i) => (
              <div
                key={i}
                className="rounded-[3px] px-1 whitespace-pre"
                style={{ color: l.fill, background: l.bg }}
              >
                {l.code}
              </div>
            ))}
          </div>
        </div>

        <div className="overflow-hidden rounded-xl border border-[rgba(245,241,234,0.13)] bg-[rgba(12,19,34,0.5)]">
          <div className="flex items-center gap-2 border-b border-[rgba(245,241,234,0.1)] px-4 py-2.5">
            <span aria-hidden="true" className="flex items-center gap-1.5">
              <span className="h-2 w-2 rounded-full bg-[#f27765]" />
              <span className="h-2 w-2 rounded-full bg-[#eabd21]" />
              <span className="h-2 w-2 rounded-full bg-[#66be77]" />
            </span>
            <span
              className="font-mono text-[10px] tracking-[0.16em] uppercase"
              style={{ color: INK_DIM }}
            >
              ci · fusion compose
            </span>
          </div>
          <div className="overflow-x-auto px-4 py-3 font-mono text-[11.5px] leading-6">
            {ROWS.map((row, i) =>
              row.segs.length === 0 ? (
                <div key={i} className="h-3" />
              ) : (
                <div
                  key={i}
                  className="whitespace-pre"
                  style={{ opacity: row.op ?? 1 }}
                >
                  {row.arrow && (
                    <svg
                      viewBox="0 0 9 8"
                      width={9}
                      height={8}
                      aria-hidden="true"
                      className="mr-1.5 inline-block"
                      style={{ color: row.segs[0].fill }}
                    >
                      <path
                        d="M0 4 h8 M5.5 1.5 L8 4 L5.5 6.5"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth={1.5}
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      />
                    </svg>
                  )}
                  {row.segs.map((s, j) => (
                    <span key={j} style={{ color: s.fill }}>
                      {s.t}
                    </span>
                  ))}
                </div>
              ),
            )}
          </div>
        </div>

        <div
          className="text-center font-mono text-[10px] tracking-[0.2em] uppercase"
          style={{ color: INK_DIM }}
        >
          Clients kept querying v41 the whole time
        </div>
      </div>

      {/* Larger screens: the PR + CI terminal canvas. */}
      <div className="hidden overflow-x-auto sm:block">
        <svg
          viewBox="0 0 900 412"
          width="100%"
          className="block min-w-[640px] sm:min-w-0"
        >
          {/* The pull request: one schema change, diff style. */}
          <rect
            x={PR.x}
            y={PR.y}
            width={PR.w}
            height={PR.h}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <text
            x={PR.x + 14}
            y={PR.y + 21}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            PULL REQUEST · billing #482
          </text>
          <line
            x1={PR.x}
            x2={PR.x + PR.w}
            y1={PR.y + 32}
            y2={PR.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {DIFF.map((l, i) => (
            <g key={i}>
              {l.bg && (
                <rect
                  x={PR.x + 8}
                  y={diffY(i) - 13}
                  width={PR.w - 16}
                  height={18}
                  rx={3}
                  fill={l.bg}
                />
              )}
              <text
                x={PR.x + 16}
                y={diffY(i)}
                xmlSpace="preserve"
                fontFamily={MONO_FONT}
                fontSize={12}
                fill={l.fill}
              >
                {l.code}
              </text>
            </g>
          ))}
          <rect
            ref={set("fixping")}
            x={PR.x + 8}
            y={diffY(2) - 13}
            width={PR.w - 16}
            height={18}
            rx={3}
            fill={OK}
            opacity={0}
          />
          {/* The PR feeds the CI check. */}
          <line
            x1={PR.x + PR.w + 6}
            x2={TERM.x - 6}
            y1={PR.y + PR.h / 2}
            y2={PR.y + PR.h / 2}
            stroke="rgba(245,241,234,0.5)"
            strokeDasharray="4 5"
          />

          {/* The CI terminal. */}
          <rect
            x={TERM.x}
            y={TERM.y}
            width={TERM.w}
            height={TERM.h}
            rx={12}
            fill="rgba(12,19,34,0.5)"
            stroke="rgba(245,241,234,0.13)"
          />
          <circle cx={TERM.x + 18} cy={TERM.y + 16} r={3.5} fill="#f27765" />
          <circle cx={TERM.x + 30} cy={TERM.y + 16} r={3.5} fill="#eabd21" />
          <circle cx={TERM.x + 42} cy={TERM.y + 16} r={3.5} fill="#66be77" />
          <text
            x={TERM.x + 56}
            y={TERM.y + 20}
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            ci · fusion compose
          </text>
          <line
            x1={TERM.x}
            x2={TERM.x + TERM.w}
            y1={TERM.y + 32}
            y2={TERM.y + 32}
            stroke="rgba(245,241,234,0.1)"
          />
          {ROWS.map((row, i) =>
            row.segs.length === 0 ? null : (
              <g key={i} ref={set(`ln${i}`)} opacity={1}>
                {row.arrow && (
                  <path
                    d={`M${TERM.x + 16} ${rowY(i) - 4} h8 M${TERM.x + 21.5} ${rowY(i) - 6.5} L${TERM.x + 24} ${rowY(i) - 4} L${TERM.x + 21.5} ${rowY(i) - 1.5}`}
                    fill="none"
                    stroke={row.segs[0].fill}
                    strokeWidth={1.5}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    opacity={row.op ?? 1}
                  />
                )}
                <text
                  x={TERM.x + (row.arrow ? 30 : 16)}
                  y={rowY(i)}
                  xmlSpace="preserve"
                  fontFamily={MONO_FONT}
                  fontSize={12}
                  opacity={row.op ?? 1}
                >
                  {row.segs.map((s, j) => (
                    <tspan key={j} fill={s.fill}>
                      {s.t}
                    </tspan>
                  ))}
                </text>
              </g>
            ),
          )}
          <rect
            ref={set("v42glow")}
            x={TERM.x + 10}
            y={rowY(7) - 14}
            width={230}
            height={19}
            rx={4}
            fill="none"
            stroke={OK}
            opacity={0}
          />
          <rect
            ref={set("cursor")}
            x={TERM.x + 16}
            y={rowY(8) - 11}
            width={7.5}
            height={14}
            fill={CMD}
            opacity={0.9}
          />

          {/* What the outage never touched: the live gateway on v41. */}
          <GatewayChip x={105} y={388.5} />
          <line
            x1={150}
            x2={272}
            y1={388.5}
            y2={388.5}
            stroke={DASHED}
            strokeDasharray="4 5"
          />
          <line
            x1={628}
            x2={750}
            y1={388.5}
            y2={388.5}
            stroke={DASHED}
            strokeDasharray="4 5"
          />
          <text
            x={450}
            y={392}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={10}
            letterSpacing="0.2em"
            fill={INK_DIM}
          >
            CLIENTS KEPT QUERYING V41 THE WHOLE TIME
          </text>
        </svg>
      </div>
    </div>
  );
}
