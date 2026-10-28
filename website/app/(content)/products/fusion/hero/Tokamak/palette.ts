import { BRAND } from "../../tokens";
import { blackToRgba, hexToRgba, warmWhiteToRgba, whiteToRgba } from "./colors";

type ToRgba = (alpha: number) => string;

/** Every colour and blend mode the Tokamak scene paints with, per appearance. */
export interface TokamakPalette {
  /** Blend mode of the plasma layers: additive on Dark, plain alpha on Light. */
  readonly composite: GlobalCompositeOperation;
  /** Opaque vessel colour behind the tiles, also the vignette and scrim tone. */
  readonly base: string;
  /** Tone of the tile faces and the ambient wash. */
  readonly tileTone: string;
  /** Multiplier on every tile face alpha. */
  readonly tileGain: number;
  /** Opaque backing of the column and bridge tiles. */
  readonly backing: ToRgba;
  readonly fastenerShadow: string;
  readonly fastenerHighlight: ToRgba;
  readonly instrumentLight: string;
  readonly coral: string;
  readonly coralSoft: string;
  /** Hot centre of a streak, the bloom core and the helix. */
  readonly streakCore: ToRgba;
  readonly filamentCore: ToRgba;
  /** Multiplier on the plasma, tint, bloom and core alphas. */
  readonly plasmaGain: number;
}

export const DARK_TOKAMAK_PALETTE: TokamakPalette = {
  composite: "lighter",
  base: BRAND.navy,
  tileTone: BRAND.slate,
  tileGain: 1,
  backing: blackToRgba,
  fastenerShadow: hexToRgba(BRAND.navy, 0.6),
  fastenerHighlight: whiteToRgba,
  instrumentLight: BRAND.cyan,
  coral: BRAND.coral,
  coralSoft: BRAND.coralSoft,
  streakCore: whiteToRgba,
  filamentCore: warmWhiteToRgba,
  plasmaGain: 1,
};

export const LIGHT_TOKAMAK_PALETTE: TokamakPalette = {
  composite: "source-over",
  base: "#f5f1ea",
  tileTone: "#0b0f1a",
  tileGain: 2.4,
  backing: (alpha) => hexToRgba("#d9d0be", alpha),
  fastenerShadow: hexToRgba("#0b0f1a", 0.3),
  fastenerHighlight: whiteToRgba,
  instrumentLight: "#075e75",
  coral: "#d4402a",
  coralSoft: "#e0705c",
  streakCore: (alpha) => hexToRgba("#7a1a0e", alpha),
  filamentCore: (alpha) => hexToRgba("#8a2012", alpha),
  plasmaGain: 1.4,
};
