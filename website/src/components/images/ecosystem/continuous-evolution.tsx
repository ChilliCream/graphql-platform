import React, { FC } from "react";

export const ECOSYSTEM_CONTINUOUS_EVOLUTION_IMAGE_WIDTH = 1198;

export const EcosystemContinuousEvolutionImage: FC = () => {
  return (
    <img
      src="/images/ecosystem/continuous-evolution.png"
      alt="An Ecosystem You Love"
      style={{ maxWidth: ECOSYSTEM_CONTINUOUS_EVOLUTION_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
