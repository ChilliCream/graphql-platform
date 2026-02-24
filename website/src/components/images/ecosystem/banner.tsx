import React, { FC } from "react";

export const ECOSYSTEM_BANNER_IMAGE_WIDTH = 600;

export const EcosystemBannerImage: FC = () => {
  return (
    <img
      src="/images/ecosystem/banner.png"
      alt="An Ecosystem You Love"
      style={{ maxWidth: ECOSYSTEM_BANNER_IMAGE_WIDTH + "px", width: "100%", height: "auto" }}
    />
  );
};
