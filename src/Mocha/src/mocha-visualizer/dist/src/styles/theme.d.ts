export declare const theme: {
  readonly colors: {
    readonly canvas: {
      readonly default: "#0d1117";
      readonly subtle: "#161b22";
      readonly inset: "#010409";
    };
    readonly border: {
      readonly default: "#30363d";
      readonly muted: "#21262d";
    };
    readonly fg: {
      readonly default: "#c9d1d9";
      readonly muted: "#8b949e";
      readonly subtle: "#6e7681";
    };
    readonly accent: {
      readonly fg: "#58a6ff";
      readonly emphasis: "#1f6feb";
    };
    readonly success: {
      readonly fg: "#3fb950";
    };
    readonly attention: {
      readonly fg: "#d29922";
    };
    readonly danger: {
      readonly fg: "#f85149";
    };
    readonly done: {
      readonly fg: "#a371f7";
    };
    readonly sponsors: {
      readonly fg: "#db61a2";
    };
    readonly scale: {
      readonly purple4: "#a371f7";
      readonly purple5: "#8957e5";
    };
  };
  readonly fonts: {
    readonly sans: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif";
    readonly mono: "ui-monospace, SFMono-Regular, SF Mono, Menlo, Consolas, monospace";
  };
};
export type Theme = typeof theme;
