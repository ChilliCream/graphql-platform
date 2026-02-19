import React, { FC } from "react";

export const ANALYTICS_BANNER_IMAGE_WIDTH = 1200;

export const AnalyticsBannerImage: FC = () => {
  return (
    <img
      src="/images/analytics/banner.png"
      alt="Instant Insights. Enhanced Performance."
      style={{ maxWidth: ANALYTICS_BANNER_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
