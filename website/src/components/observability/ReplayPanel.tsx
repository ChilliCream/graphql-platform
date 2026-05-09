"use client";

import React, { FC } from "react";

import {
  REPLAY_PROD_TRACE,
  REPLAY_STAGING_TRACE,
  TraceWaterfall,
} from "./TraceWaterfall";

// Side-by-side prod vs staging replay. The brief calls this the page's single
// most important visual. Same query header sits across both panes; left has
// a faint red overlay and a FAILED status; right has a faint green overlay
// and an OK status. The colors come from the panel-level CSS classes
// `is-fail` and `is-ok` so the tinting stays in one place.

export const ReplayPanel: FC = () => {
  return (
    <div className="cc-replay-mock">
      <div className="cc-replay-header" aria-label="Captured query">
        <span className="method">POST</span>
        <span className="path">/graphql</span>
        <span>·</span>
        <span>op cart-checkout</span>
        <span>·</span>
        <span>session 3a4f</span>
        <span>·</span>
        <span>captured 12:04:18</span>
      </div>
      <div className="cc-replay-grid">
        <div className="cc-replay-pane is-fail">
          <div className="cc-replay-pane-header">
            <span>Production · trace</span>
            <span className="cc-replay-pane-status">FAILED · 412ms</span>
          </div>
          <div className="cc-replay-pane-body">
            <TraceWaterfall
              spans={REPLAY_PROD_TRACE}
              compact
              totalLabel="0ms · 500ms"
              axisMs={[0, 125, 250, 375, 500]}
            />
          </div>
        </div>
        <div className="cc-replay-pane is-ok">
          <div className="cc-replay-pane-header">
            <span>Staging · replay</span>
            <span className="cc-replay-pane-status">OK · 87ms</span>
          </div>
          <div className="cc-replay-pane-body">
            <TraceWaterfall
              spans={REPLAY_STAGING_TRACE}
              compact
              totalLabel="0ms · 500ms"
              axisMs={[0, 125, 250, 375, 500]}
            />
          </div>
        </div>
      </div>
    </div>
  );
};
