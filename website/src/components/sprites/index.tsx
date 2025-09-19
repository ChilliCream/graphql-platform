import React, { FC, SVGProps } from "react";

type SpriteProps = Required<Pick<SVGProps<SVGElement>, "id" | "viewBox">> &
  Pick<SVGProps<SVGElement>, "className" | "onClick">;

const Sprite: FC<SpriteProps> = ({ id, ...rest }) => (
  <svg {...rest}>
    <use href={`#${id}`} />
  </svg>
);

export const Artwork = Sprite;
export const Company = Sprite;
export const Icon = Sprite;

export const Logo: FC<SpriteProps> = ({ id, ...rest }) => (
  <svg id="logo" {...rest}>
    <use href={`#${id}`} />
  </svg>
);
