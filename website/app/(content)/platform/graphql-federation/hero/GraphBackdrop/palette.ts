import {
  CYAN,
  LIGHT_CYAN,
  LIGHT_SURFACE,
  LIGHT_VIOLET,
  NAVY,
  SLATE,
  TEAL,
} from "../palette";

/** Colours the constellation backdrop paints with, per appearance. */
export interface BackdropPalette {
  readonly tints: readonly [string, string];
  readonly edge: string;
  readonly edgeTint: string;
  readonly halo: string;
  /** Multiplier on the edge alpha. */
  readonly edgeGain: number;
  readonly hubCore: string;
  /** Surface colour of the scrims and the vignette. */
  readonly surface: string;
  /** Colour the copy-zone darken blend moves toward. */
  readonly darkenTo: string;
}

export const DARK_BACKDROP_PALETTE: BackdropPalette = {
  tints: [CYAN, TEAL],
  edge: SLATE,
  edgeTint: CYAN,
  halo: CYAN,
  edgeGain: 1,
  hubCore: "white",
  surface: NAVY,
  darkenTo: "black",
};

export const LIGHT_BACKDROP_PALETTE: BackdropPalette = {
  tints: [LIGHT_CYAN, LIGHT_VIOLET],
  edge: LIGHT_CYAN,
  edgeTint: LIGHT_VIOLET,
  halo: LIGHT_CYAN,
  edgeGain: 1,
  hubCore: "white",
  surface: LIGHT_SURFACE,
  darkenTo: LIGHT_SURFACE,
};
