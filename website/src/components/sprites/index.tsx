import React, { FC, SVGProps } from "react";

const Sprite: FC<SVGProps<SVGElement>> = ({ viewBox, id, className }) => (
  <svg viewBox={viewBox} className={className}>
    <use xlinkHref={`#${id}`} />
  </svg>
);

export const Artwork = Sprite;
export const Brand = Sprite;
export const Company = Sprite;

export const Logo: FC<SVGProps<SVGElement>> = ({ viewBox, id, className }) => (
  <svg id="logo" viewBox={viewBox} className={className}>
    <use xlinkHref={`#${id}`} />
  </svg>
);
