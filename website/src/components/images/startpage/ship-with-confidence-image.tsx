import React, { FC } from "react";

export const SHIP_WITH_CONFIDENCE_IMAGE_WIDTH = 650;

export const ShipWithConfidenceImage: FC = () => {
  return (
    <img
      src="/images/startpage/ship-with-confidence.png"
      alt="Ship With Confidence"
      style={{ maxWidth: SHIP_WITH_CONFIDENCE_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
