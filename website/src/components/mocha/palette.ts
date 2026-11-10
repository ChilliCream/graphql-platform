import { lightDark } from "@/src/components/lightDark";

export const CYAN = "#16b9e4";
export const TEAL = "#5eead4";
export const CORAL = "#f0786a";
export const CORAL_SOFT = "#f6cabe";
export const VIOLET = "#7c92c6";
export const GREEN = "#34d399";
export const AMBER = "#fbbf24";
export const SLATE = "#8ba0bc";
export const NAVY = "#0b0f1a";

export const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, monospace";

export const SLATE_TEXT = lightDark("var(--color-cc-ink-dim)", SLATE);
export const CYAN_TEXT = lightDark("var(--color-cc-accent)", CYAN);
export const GREEN_TEXT = lightDark("var(--color-cc-success)", GREEN);
export const CORAL_TEXT = lightDark("var(--color-cc-danger)", CORAL);
export const CORAL_SOFT_TEXT = lightDark("var(--color-cc-danger)", CORAL_SOFT);
export const VIOLET_TEXT = lightDark("var(--color-cc-info)", VIOLET);
export const AMBER_TEXT = lightDark("var(--color-cc-warning)", AMBER);
