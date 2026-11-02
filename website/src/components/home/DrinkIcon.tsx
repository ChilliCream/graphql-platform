import type { ComponentType, CSSProperties } from "react";

type Drink = ComponentType<{
  readonly className?: string;
  readonly style?: CSSProperties;
}>;

/**
 * The drink icons are crops from one sprite: they share a bottom edge but have
 * different viewBox heights (a coffee cup is 84 tall; the Strawberry Shake
 * milkshake is 109; an espresso is 61). Sizing them all to one CSS height shrinks
 * the taller drinks' bodies and floats them off the baseline.
 *
 * DrinkIcon renders every drink at a CONSTANT per-unit scale (so the cup bodies
 * match) inside a fixed-height box, bottom-aligned, so a row reads as one shelf
 * of drinks with consistent weight, a milkshake simply standing a little taller.
 *
 * `base` is the body height in px for a normal 84-unit cup; pass the px value the
 * take used before (h-10 = 40, h-12 = 48, h-14 = 56, h-16 = 64).
 */

const COFFEE_CUP_UNITS = 84;

/** viewBox height per drink, used to keep one constant px-per-unit scale. */
const DRINK_UNITS: Record<string, number> = {
  "hot chocolate": 84,
  fusion: 84,
  nitro: 84,
  mocha: 84,
  "strawberry shake": 109,
  "green donut": 109,
  "cookie crumble": 109,
  espresso: 61,
};

/** Tallest drink we account for, so the box always reserves enough headroom. */
const MAX_RATIO = 109 / COFFEE_CUP_UNITS;

function ratioFor(name: string): number {
  const units = DRINK_UNITS[name.trim().toLowerCase()] ?? COFFEE_CUP_UNITS;
  return units / COFFEE_CUP_UNITS;
}

interface DrinkIconProps {
  readonly Icon: Drink;
  /** Product name, used to look up the drink's true proportions. */
  readonly name: string;
  /** Body height in px for a normal cup. */
  readonly base: number;
  /** Applied to the inner svg (hover transforms, drop-shadow, etc.). */
  readonly className?: string;
  readonly style?: CSSProperties;
}

export function DrinkIcon({
  Icon,
  name,
  base,
  className,
  style,
}: DrinkIconProps) {
  return (
    <span
      aria-hidden="true"
      className="flex items-end justify-center"
      style={{ height: `${base * MAX_RATIO}px` }}
    >
      <Icon
        className={className}
        style={{
          height: `${base * ratioFor(name)}px`,
          width: "auto",
          ...style,
        }}
      />
    </span>
  );
}
