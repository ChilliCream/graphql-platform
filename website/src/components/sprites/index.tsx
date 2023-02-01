import React, { FC, SVGProps } from "react";

type SpriteProps = Required<Pick<SVGProps<SVGElement>, "id" | "viewBox">> &
  Pick<SVGProps<SVGElement>, "className">;

const Sprite: FC<SpriteProps> = ({ id, viewBox, className }) => (
  <svg viewBox={viewBox} className={className}>
    <use href={`#${id}`} />
  </svg>
);

export const Artwork = Sprite;
export const Brand = Sprite;
export const Company = Sprite;

export const Logo: FC<SpriteProps> = ({ id, viewBox, className }) => (
  <svg id="logo" viewBox={viewBox} className={className}>
    <use href={`#${id}`} />
  </svg>
);
