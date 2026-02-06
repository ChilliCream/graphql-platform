import React, { FC } from "react";

export const CONTINUOUS_INTEGRATION_CONNECT_IMAGE_WIDTH = 869;

export const ContinuousIntegrationConnectImage: FC = () => {
  return (
    <img
      src="/images/continuous-integration/connect.png"
      alt="Connect Your Ecosystem"
      style={{ maxWidth: CONTINUOUS_INTEGRATION_CONNECT_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
