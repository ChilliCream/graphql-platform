"use client";

import React from "react";
import styled from "styled-components";

// PunchCardBackground renders an IBM-mainframe-era punch card pattern as a
// full-page background for the cinematic /products/nitro/agents variant.
// The metaphor: agents read your platform with punch-card precision. Five
// quiet layers, all very low opacity, all pointer-events: none:
//
//   1. an 80-column x 12-row grid of hollow Hollerith slots (1px hairline
//      borders), repeating vertically every PANEL_HEIGHT pixels as the
//      page scrolls, so the pattern feels like one continuous deck of
//      cards stacked end to end;
//   2. a sparse subset of slots is "punched" — filled with the agents
//      amber. The punched slots form a 5x7 dot-matrix pattern that spells
//      AGENT on rows 2-8 and MCP on rows 4-10 of two separate cards
//      (different columns, different cards), so the pattern is legible
//      only to readers who actively look for it;
//   3. column numbers (1-80) along the very top edge of each card, faint
//      monospace 8px;
//   4. row labels (12, 11, 0-9) running down the left edge — that's the
//      canonical Hollerith row layout, with a horizontal dashed line
//      between row 9 and the rest acting as the "9-row separator" of real
//      punch cards;
//   5. an orientation notch chamfered into the top-left corner of each
//      card, lifted from the physical card stock.
//
// The pattern should feel like an artifact of computing history: legible
// as a punch card, illegible as an actual message, and quiet enough that
// the Loop diagram and Terminal mock keep dominating the foreground.

export interface PunchCardBackgroundProps {
  className?: string;
}

const COLS = 80;
const ROWS = 12;
const SLOT_W = 8;
const SLOT_H = 14;
const SLOT_GAP_X = 5;
const SLOT_GAP_Y = 5;
const ROW_LABEL_W = 28;
const TOP_LABEL_H = 14;
const NOTCH = 14;
const PANEL_PAD_X = 16;
const PANEL_PAD_Y = 14;

const GRID_W = COLS * SLOT_W + (COLS - 1) * SLOT_GAP_X;
const GRID_H = ROWS * SLOT_H + (ROWS - 1) * SLOT_GAP_Y;
const PANEL_WIDTH = ROW_LABEL_W + GRID_W + PANEL_PAD_X * 2;
const PANEL_HEIGHT = TOP_LABEL_H + GRID_H + PANEL_PAD_Y * 2;

// Canonical Hollerith row labels, top to bottom (12, 11, 0, 1...9).
const ROW_LABELS = [
  "12",
  "11",
  "0",
  "1",
  "2",
  "3",
  "4",
  "5",
  "6",
  "7",
  "8",
  "9",
];

// 5x7 dot-matrix glyphs. Rows are top-to-bottom, each string is 5 cells
// wide where "#" is a punched (filled) slot and "." is a hollow slot.
const FONT: Record<string, readonly string[]> = {
  A: [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
  G: [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###."],
  E: ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
  N: ["#...#", "##..#", "###.#", "#.#.#", "#..##", "#...#", "#...#"],
  T: ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
  M: ["#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#", "#...#"],
  C: [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
  P: ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
};

interface Punch {
  readonly col: number;
  readonly row: number;
}

// Lay a word out into punched slots starting at (startCol, startRow).
// Each glyph is 5 columns wide; glyphs are separated by one blank column.
function layoutWord(word: string, startCol: number, startRow: number): Punch[] {
  const punches: Punch[] = [];
  let col = startCol;
  for (const ch of word) {
    const glyph = FONT[ch];
    if (glyph) {
      for (let r = 0; r < glyph.length; r++) {
        const line = glyph[r];
        for (let c = 0; c < line.length; c++) {
          if (line[c] === "#") {
            punches.push({ col: col + c, row: startRow + r });
          }
        }
      }
    }
    col += 6; // 5 glyph cols + 1 blank
  }
  return punches;
}

// Punched slot pattern. AGENT sits high-left on the first card; MCP sits
// further right one card down. The two patterns alternate across stacked
// panels so the same panel never shows both labels at once, keeping the
// pattern sparse and resisting "wall of text" energy.
const PUNCHES_EVEN: ReadonlySet<string> = (() => {
  const set = new Set<string>();
  // AGENT spans 5 letters x 5 cols + 4 gaps = 29 cols, rows 2-8.
  for (const p of layoutWord("AGENT", 6, 2)) {
    set.add(`${p.col}:${p.row}`);
  }
  return set;
})();

const PUNCHES_ODD: ReadonlySet<string> = (() => {
  const set = new Set<string>();
  // MCP spans 3 letters x 5 cols + 2 gaps = 17 cols, rows 4-10.
  for (const p of layoutWord("MCP", 50, 4)) {
    set.add(`${p.col}:${p.row}`);
  }
  return set;
})();

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;

// The card pattern repeats vertically. We render N panels (enough to cover
// any plausible page height) using absolutely positioned SVG layers so the
// pattern stays pixel-aligned even when the parent wrapper is the size
// of the page.
const PANEL_REPEAT = 14;

const Panel = styled.svg`
  position: absolute;
  left: 0;
  width: ${PANEL_WIDTH}px;
  height: ${PANEL_HEIGHT}px;
  display: block;
  overflow: visible;
  font-family: var(--cc-font-mono, ui-monospace, "SF Mono", Menlo, monospace);
`;

interface PanelProps {
  readonly index: number;
}

const PunchCardPanel: React.FC<PanelProps> = ({ index }) => {
  const punches = index % 2 === 0 ? PUNCHES_EVEN : PUNCHES_ODD;
  const top = index * PANEL_HEIGHT;
  const gridX = ROW_LABEL_W + PANEL_PAD_X;
  const gridY = TOP_LABEL_H + PANEL_PAD_Y;

  // Build the slot rects. Hollow slots use stroke; punched slots use fill.
  const slots: React.ReactNode[] = [];
  for (let r = 0; r < ROWS; r++) {
    for (let c = 0; c < COLS; c++) {
      const x = gridX + c * (SLOT_W + SLOT_GAP_X);
      const y = gridY + r * (SLOT_H + SLOT_GAP_Y);
      const punched = punches.has(`${c}:${r}`);
      slots.push(
        <rect
          key={`${c}:${r}`}
          x={x + 0.5}
          y={y + 0.5}
          width={SLOT_W - 1}
          height={SLOT_H - 1}
          rx={1}
          ry={1}
          fill={punched ? "rgba(247, 186, 100, 0.18)" : "transparent"}
          stroke={
            punched ? "rgba(247, 186, 100, 0.34)" : "rgba(245, 241, 234, 0.06)"
          }
          strokeWidth={1}
        />
      );
    }
  }

  // Column numbers along the top edge, every 5 columns so the strip stays
  // legible without becoming visual noise.
  const colNumbers: React.ReactNode[] = [];
  for (let c = 0; c < COLS; c++) {
    if (c === 0 || (c + 1) % 5 === 0) {
      const x = gridX + c * (SLOT_W + SLOT_GAP_X) + SLOT_W / 2;
      colNumbers.push(
        <text
          key={c}
          x={x}
          y={TOP_LABEL_H - 4}
          fontSize="7"
          fill="rgba(245, 241, 234, 0.10)"
          textAnchor="middle"
          dominantBaseline="alphabetic"
        >
          {c + 1}
        </text>
      );
    }
  }

  // Row labels down the left edge.
  const rowLabels: React.ReactNode[] = [];
  for (let r = 0; r < ROWS; r++) {
    const y = gridY + r * (SLOT_H + SLOT_GAP_Y) + SLOT_H / 2;
    rowLabels.push(
      <text
        key={r}
        x={ROW_LABEL_W - 6}
        y={y}
        fontSize="8"
        fill="rgba(245, 241, 234, 0.10)"
        textAnchor="end"
        dominantBaseline="middle"
      >
        {ROW_LABELS[r]}
      </text>
    );
  }

  // The 9-row separator: a faint dashed horizontal rule between row 8 and
  // row 9 (the bottom row), echoing the printed separator on real cards.
  const sepY = gridY + 11 * (SLOT_H + SLOT_GAP_Y) - SLOT_GAP_Y / 2 - 0.5;

  // Orientation notch: a small chamfer cut into the top-left corner of
  // the card outline. The outer card outline itself is a faint rectangle
  // missing its top-left corner.
  const cardLeft = 0.5;
  const cardTop = 0.5;
  const cardRight = PANEL_WIDTH - 0.5;
  const cardBottom = PANEL_HEIGHT - 0.5;
  const cardPath = [
    `M ${cardLeft + NOTCH} ${cardTop}`,
    `L ${cardRight} ${cardTop}`,
    `L ${cardRight} ${cardBottom}`,
    `L ${cardLeft} ${cardBottom}`,
    `L ${cardLeft} ${cardTop + NOTCH}`,
    `Z`,
  ].join(" ");

  return (
    <Panel
      xmlns="http://www.w3.org/2000/svg"
      style={{ top }}
      viewBox={`0 0 ${PANEL_WIDTH} ${PANEL_HEIGHT}`}
    >
      <path
        d={cardPath}
        fill="none"
        stroke="rgba(245, 241, 234, 0.05)"
        strokeWidth={1}
      />
      {colNumbers}
      {rowLabels}
      {slots}
      <line
        x1={gridX - 6}
        y1={sepY}
        x2={gridX + GRID_W + 6}
        y2={sepY}
        stroke="rgba(245, 241, 234, 0.08)"
        strokeWidth={1}
        strokeDasharray="3 5"
      />
    </Panel>
  );
};

/**
 * Faint mainframe punch-card background pattern for the cinematic
 * /products/nitro/agents variant. Decorative only and hidden from
 * assistive tech.
 */
export const PunchCardBackground: React.FC<PunchCardBackgroundProps> = ({
  className,
}) => {
  const panels: React.ReactNode[] = [];
  for (let i = 0; i < PANEL_REPEAT; i++) {
    panels.push(<PunchCardPanel key={i} index={i} />);
  }
  return (
    <Outer className={className} aria-hidden="true">
      {panels}
    </Outer>
  );
};
