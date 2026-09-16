/**
 * The five service colours from the start page's Fusion flow
 * (`src/components/home/FusionFlow.tsx`), in the same left-to-right order,
 * plus the gradient built from them in `src/components/home/ProtocolCards.tsx`.
 * The only spectrum the hero prototypes may use.
 */

export interface SpectrumStop {
  readonly label: string;
  readonly color: string;
}

export const SERVICE_SPECTRUM: readonly SpectrumStop[] = [
  { label: "Catalog", color: "#f27765" },
  { label: "Billing", color: "#eabd21" },
  { label: "Ordering", color: "#66be77" },
  { label: "Shipping", color: "#00bce5" },
  { label: "User", color: "#a983ba" },
];

export const SPECTRUM_GRADIENT =
  "linear-gradient(to right,#f27765 0%,#eabd21 25%,#66be77 50%,#00bce5 75%,#a983ba 100%)";
