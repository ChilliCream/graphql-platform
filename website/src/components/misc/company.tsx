import React, { FC, SVGProps } from "react";

export const Company: FC<SVGProps<SVGElement>> = ({
  viewBox,
  id,
  className,
}) => (
  <svg viewBox={viewBox} className={className}>
    <use xlinkHref={`#${id}`} />
  </svg>
);
