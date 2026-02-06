import React, { FC } from "react";

export const NITRO_BANNER_IMAGE_WIDTH = 1200;

export const NitroBannerImage: FC = () => {
  return (
    <img
      src="/images/nitro/nitro-banner.png"
      alt="Nitro"
      style={{ maxWidth: NITRO_BANNER_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
