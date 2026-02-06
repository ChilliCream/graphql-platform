import React, { FC } from "react";

export const CONTINUOUS_INTEGRATION_DESIGN_IMAGE_WIDTH = 1043;

export const ContinuousIntegrationDesignImage: FC = () => {
  return (
    <img
      src="/images/continuous-integration/design.png"
      alt="Connect Your Ecosystem"
      style={{ maxWidth: CONTINUOUS_INTEGRATION_DESIGN_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
