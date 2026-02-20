import React, { FC } from "react";

export const CONTINUOUS_INTEGRATION_TRACK_IMAGE_WIDTH = 1043;

export const ContinuousIntegrationTrackImage: FC = () => {
  return (
    <img
      src="/images/continuous-integration/track.png"
      alt="Connect Your Ecosystem"
      style={{ maxWidth: CONTINUOUS_INTEGRATION_TRACK_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
