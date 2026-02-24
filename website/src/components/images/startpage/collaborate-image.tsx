import React, { FC } from "react";

export const COLLABORATE_IMAGE_WIDTH = 655;

export const CollaborateImage: FC = () => {
  return (
    <img
      src="/images/startpage/collaborate.png"
      alt="Collaborate"
      width={1330}
      height={722}
      loading="lazy"
      decoding="async"
      style={{
        maxWidth: COLLABORATE_IMAGE_WIDTH + "px",
        width: "100%",
        height: "auto",
      }}
    />
  );
};
