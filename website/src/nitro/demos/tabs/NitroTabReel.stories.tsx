import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { NitroTabReel } from "./NitroTabReel";
import { ThemeProvider } from "../../lib/theme";

// NitroTabReel autoplays a 5-tab reel off a master clock, so a plain story would screenshot a
// non-deterministic frame. Its `staticTab` + `staticProgress` props freeze one tab at a fixed local
// progress with NO clock running (verified in TabReel.tsx: static mode passes a constant
// `frozenLocal` MotionValue to the active screen and never starts `animate`), so each variant below
// renders a stable, deterministic frame. staticProgress ~0.55 lands mid-reveal on each tab.
const meta = {
  title: "Nitro/TabReel",
  component: NitroTabReel,
  parameters: { layout: "fullscreen" },
  args: {
    staticProgress: 0.55,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" className="mx-auto w-full max-w-[1200px] p-6">
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof NitroTabReel>;

export default meta;
type Story = StoryObj<typeof NitroTabReel>;

// Tab 0 — Observe (the monitoring / trace flagship).
export const Observe: Story = {
  args: { staticTab: 0 },
};

// Tab 1 — Diagnose (error spike to root cause).
export const Diagnose: Story = {
  args: { staticTab: 1 },
};

// Tab 2 — Fusion (query plan across subgraphs).
export const Fusion: Story = {
  args: { staticTab: 2 },
};

// Tab 3 — Schema (deprecated-field usage).
export const Schema: Story = {
  args: { staticTab: 3 },
};

// Tab 4 — Author (schema-aware compose).
export const Author: Story = {
  args: { staticTab: 4 },
};
