import React, { FC } from "react";

export const ANALYTICS_OVERVIEW_IMAGE_WIDTH = 1000;

export const AnalyticsOverviewImage: FC = () => {
  return (
    <img
      src="/images/analytics/overview.png"
      alt="Overview from Every Angle"
      style={{ maxWidth: ANALYTICS_OVERVIEW_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
