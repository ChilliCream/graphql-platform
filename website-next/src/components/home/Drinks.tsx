import type { CSSProperties } from "react";

import { Image } from "@/src/design-system/Image";

/**
 * One component per ChilliCream product drink. Each renders its own standalone
 * SVG from `public/images/products/`, split out of the startpage artwork so the
 * files can be edited and reused independently.
 */
interface DrinkProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

export function GreenDonut({ className, style }: DrinkProps) {
  return <Image src="/images/products/green-donut.svg" alt="Green Donut" className={className} style={style} />;
}

export function StrawberryShake({ className, style }: DrinkProps) {
  return <Image src="/images/products/strawberry-shake.svg" alt="Strawberry Shake" className={className} style={style} />;
}

export function CookieCrumble({ className, style }: DrinkProps) {
  return <Image src="/images/products/cookie-crumble.svg" alt="Cookie Crumble" className={className} style={style} />;
}

export function Fusion({ className, style }: DrinkProps) {
  return <Image src="/images/products/fusion.svg" alt="Fusion" className={className} style={style} />;
}

export function Mocha({ className, style }: DrinkProps) {
  return <Image src="/images/products/mocha.svg" alt="Mocha" className={className} style={style} />;
}

export function HotChocolate({ className, style }: DrinkProps) {
  return <Image src="/images/products/hot-chocolate.svg" alt="Hot Chocolate" className={className} style={style} />;
}

export function Nitro({ className, style }: DrinkProps) {
  return <Image src="/images/products/nitro.svg" alt="Nitro" className={className} style={style} />;
}

export function Espresso({ className, style }: DrinkProps) {
  return <Image src="/images/products/espresso.svg" alt="Espresso" className={className} style={style} />;
}
