import React, { FC, SVGProps } from "react";

export const Artwork: FC<SVGProps<SVGElement>> = ({
  viewBox,
  id,
  className,
}) => (
  <svg viewBox={viewBox} className={className}>
    <use xlinkHref={`#${id}`} />
  </svg>
);
