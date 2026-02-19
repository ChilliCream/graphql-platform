import React, { FC } from "react";

export const CONTINUOUS_INTEGRATION_DEPLOY_IMAGE_WIDTH = 992;

export const ContinuousIntegrationDeployImage: FC = () => {
  return (
    <img
      src="/images/continuous-integration/deploy.png"
      alt="Connect Your Ecosystem"
      style={{ maxWidth: CONTINUOUS_INTEGRATION_DEPLOY_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
