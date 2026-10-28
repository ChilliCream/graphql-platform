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
  base: "#fcfbf8",
  tileTone: "#8b5cf6",
  tileGain: 1.6,
  backing: (alpha) => hexToRgba("#f1ecff", alpha),
  fastenerShadow: hexToRgba("#8b5cf6", 0.35),
  fastenerHighlight: whiteToRgba,
  instrumentLight: "#06b6d4",
  coral: "#f43f5e",
  coralSoft: "#fb7185",
  streakCore: (alpha) => hexToRgba("#e11d48", alpha),
  filamentCore: (alpha) => hexToRgba("#d946ef", alpha),
  plasmaGain: 1.4,
};
