"use client";

import { createContext, useContext } from "react";

interface VisualSettings {
  /** The pause control is on: animation loops hold their current frame. */
  readonly paused: boolean;
  /**
   * Labels render at 11px or more, in opaque colors with at least 4.5:1
   * contrast. Article content opts in; the product page keeps its denser look.
   */
  readonly readable: boolean;
}

/**
 * Settings for the Mocha diagrams below. `MochaVisual` provides them; without
 * a provider the diagrams play and keep their product-page style.
 */
export const VisualSettingsContext = createContext<VisualSettings>({
  paused: false,
  readable: false,
});

export function useVisualSettings(): VisualSettings {
  return useContext(VisualSettingsContext);
}

/**
 * Card width that keeps a readable diagram at its full label size: the
 * diagram's minimum layout width times its readable scale, plus the card's
 * padding and border. Below it the card scrolls sideways in `MochaVisual`.
 */
export function readableMinWidth(minW: number, scale: number, chrome: number) {
  return Math.ceil(minW * scale + chrome);
}
