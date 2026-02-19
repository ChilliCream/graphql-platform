import React, { FC } from "react";

export const OBSERVE_IMAGE_WIDTH = 670;

export const ObserveImage: FC = () => {
  return (
    <img
      src="/images/startpage/observe.png"
      alt="Observe"
      style={{ maxWidth: OBSERVE_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
