import React, { FC, SVGProps } from "react";

export const Logo: FC<SVGProps<SVGElement>> = ({ viewBox, id, className }) => (
  <svg id="logo" viewBox={viewBox} className={className}>
    <use xlinkHref={`#${id}`} />
  </svg>
);
