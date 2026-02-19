import React, { FC } from "react";

export const ANALYTICS_INSIGHTS_IMAGE_WIDTH = 1200;

export const AnalyticsInsightsImage: FC = () => {
  return (
    <img
      src="/images/analytics/insights.png"
      alt="Trace, Detail, Insight."
      style={{ maxWidth: ANALYTICS_INSIGHTS_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
