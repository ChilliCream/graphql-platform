import React, { FC } from "react";

export const EVOLVE_IMAGE_WIDTH = 524;

export const EvolveImage: FC = () => {
  return (
    <img
      src="/images/startpage/evolve.png"
      alt="Observe"
      style={{ maxWidth: EVOLVE_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
