import React, { FC } from "react";

export const MOVE_FASTER_IMAGE_WIDTH = 658;

export const MoveFasterImage: FC = () => {
  return (
    <img
      src="/images/startpage/move-faster.png"
      alt="Move Faster"
      width={1316}
      height={1124}
      loading="lazy"
      decoding="async"
      style={{
        maxWidth: MOVE_FASTER_IMAGE_WIDTH + "px",
        width: "100%",
        height: "auto",
      }}
    />
  );
};
