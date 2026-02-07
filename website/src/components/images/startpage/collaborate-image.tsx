import React, { FC } from "react";

export const COLLABORATE_IMAGE_WIDTH = 655;

export const CollaborateImage: FC = () => {
  return (
    <img
      src="/images/startpage/collaborate.png"
      alt="Observe"
      style={{ maxWidth: COLLABORATE_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
