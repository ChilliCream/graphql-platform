"use client";

import styled from "styled-components";

import { AgentsRoot } from "../AgentsRoot";

// AgentsGridRoot extends AgentsRoot 1:1 so every existing CSS class on the
// agents page keeps working (.cc-term, .cc-ag-loop-svg, .cc-ag-demo,
// .cc-ag-product-row, etc.) inside the Grid variant. The Grid layer adds
// strict-hairline framing and zero-radius surfaces ON TOP of the shared
// agents tokens, then overrides the page background to the flat dark
// canvas the Grid spec requires.
//
// What the Grid root changes versus the default:
//   - Page background flips from the radial-amber gradient to a flat
//     `#0c1322` so the hairlines read at 1px without any glow contamination.
//   - The legacy `--cc-pad-x` is preserved for the inherited section shell.
//   - All chrome lives at zero border-radius. Cards inside the page never
//     show drop shadows or gradient frames. The hero terminal in particular
//     is overridden so it sits naked inside its GridCard cell.
//   - `--cc-grid-*` tokens are exposed so primitives like GridCard,
//     GridSection, and GridRow resolve to the correct hairline / surface
//     palette without each component having to know it lives on agents.
export const AgentsGridRoot = styled(AgentsRoot)`
  /* Grid palette tokens (read by GridSection / GridCard / GridRow). */
  --cc-grid-page-bg: #0c1322;
  --cc-grid-card-bg: #0f1828;
  --cc-grid-card-bg-inverted: #ffffff;
  --cc-grid-hairline: #1f2a3d;
  --cc-grid-hairline-strong: #2c3a52;
  --cc-grid-card-padding: clamp(28px, 3vw, 40px);

  /* Flat surface, no ambient gradient. The hairlines do all the work. */
  background: var(--cc-grid-page-bg);

  /* Legacy section shell padding lines up with grid gutter. */
  --cc-pad-x: clamp(24px, 5vw, 64px);

  /* Strip rounded corners and shadows from inherited chrome. The hero
     terminal is the most visible target: AgentsRoot wraps it in a 22px
     gradient frame with a soft shadow, neither of which belongs on Grid. */
  .cc-term {
    border-radius: 0;
    padding: 0;
    background: var(--cc-grid-card-bg);
    border: 1px solid var(--cc-grid-hairline);
    box-shadow: none;
  }
  .cc-term-inner {
    border-radius: 0;
    background: transparent;
  }

  /* Buttons sit square inside Grid cards. The .cc-btn shape is inherited
     from AgentsRoot but the radius and hover transform come from a
     pre-Grid system, so flatten them here. */
  .cc-btn {
    border-radius: 0;
    padding: 12px 22px;
  }
  .cc-btn:hover {
    transform: none;
  }
  .cc-btn-primary {
    background: #ffffff;
    color: var(--cc-grid-page-bg);
  }
  .cc-btn-ghost {
    border-color: var(--cc-grid-hairline-strong);
  }

  /* Demo internals also carry rounded chrome from the cinematic / default
     variants. Square them off so they read on the Grid surface. */
  .cc-ag-demo-investigate-side,
  .cc-ag-demo-trace-full,
  .cc-ag-demo-chat,
  .cc-ag-demo-out,
  .cc-ag-demo-out-snippet,
  .cc-ag-fanout-tile,
  .cc-ag-ledger-row,
  .cc-ag-guardrail,
  .cc-ag-guardrail-icon,
  .cc-ag-sees-viz {
    border-radius: 0;
  }
  .cc-ag-client-chip a {
    border-radius: 0;
  }
`;
