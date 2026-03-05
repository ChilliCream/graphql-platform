import React, { FC, SVGProps } from "react";

type SpriteProps = Required<Pick<SVGProps<SVGElement>, "id" | "viewBox">> &
  Pick<SVGProps<SVGElement>, "className" | "onClick">;

const Sprite: FC<SpriteProps> = ({ id, viewBox, className, onClick }) => (
  <svg viewBox={viewBox} className={className} onClick={onClick}>
    <use href={`#${id}`} />
  </svg>
);

export const Artwork = Sprite;
export const Company = Sprite;
export const Icon = Sprite;

export const Logo: FC<SpriteProps> = ({ id, viewBox, className, onClick }) => (
  <svg id="logo" viewBox={viewBox} className={className} onClick={onClick}>
    <use href={`#${id}`} />
  </svg>
);
