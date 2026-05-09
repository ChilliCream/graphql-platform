"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  OtelLogoStrip,
  OtelTimeline,
} from "@/components/observability/OtelLogoStrip";

// OTEL section: split layout, text on the left, the OtelTimeline + 6-monogram
// logo strip on the right. Used as the "Bring your backend" beat after the
// MCP inverted band, before the final CTA.

const Outer = styled.div`
  display: grid;
  grid-template-columns: minmax(0, 0.85fr) minmax(0, 1.25fr);
  gap: 0;
  align-items: stretch;
  border-top: 1px solid var(--cc-grid-hairline);
  border-bottom: 1px solid var(--cc-grid-hairline);

  @media (max-width: 980px) {
    grid-template-columns: 1fr;
  }
`;

const Copy = styled.div`
  padding: clamp(32px, 4vw, 56px);
  display: flex;
  flex-direction: column;
  justify-content: center;
`;

const Visual = styled.div`
  padding: clamp(28px, 3vw, 44px);
  display: flex;
  flex-direction: column;
  justify-content: center;
  border-left: 1px solid var(--cc-grid-hairline);

  @media (max-width: 980px) {
    border-left: 0;
    border-top: 1px solid var(--cc-grid-hairline);
  }
`;

export const ObservabilityGridOtelSection: FC = () => {
  return (
    <Outer>
      <Copy>
        <span className="cc-grid-eyebrow">OTEL &amp; integrations</span>
        <h2 className="cc-grid-display cc-grid-h2">Bring your backend.</h2>
        <p className="cc-grid-body">
          Built on OpenTelemetry. Federation traces appear in the tools you
          already use, no glue.
        </p>
      </Copy>
      <Visual>
        <OtelTimeline />
        <OtelLogoStrip />
      </Visual>
    </Outer>
  );
};
