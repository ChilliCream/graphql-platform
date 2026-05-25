import React, { FC } from "react";

export const SHIP_WITH_CONFIDENCE_IMAGE_WIDTH = 650;

export const ShipWithConfidenceImage: FC = () => {
  return (
    <img
      src="/images/startpage/ship-with-confidence.png"
      alt="Ship With Confidence"
      width={1300}
      height={906}
      loading="lazy"
      decoding="async"
      style={{
        maxWidth: SHIP_WITH_CONFIDENCE_IMAGE_WIDTH + "px",
        width: "100%",
        height: "auto",
      }}
    />
  );
};
