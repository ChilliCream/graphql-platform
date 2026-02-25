import React, { FC } from "react";

export const ANALYTICS_OBSERVABILITY_IMAGE_WIDTH = 1200;

export const AnalyticsObservabilityImage: FC = () => {
  return (
    <img
      src="/images/analytics/observability.png"
      alt="Observability in Focus"
      style={{ maxWidth: ANALYTICS_OBSERVABILITY_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
