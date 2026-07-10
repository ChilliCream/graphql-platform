import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { NitroTabReel } from "./NitroTabReel";
import { ThemeProvider } from "../../lib/theme";

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

export const Observe: Story = {
  args: { staticTab: 0 },
};

export const Diagnose: Story = {
  args: { staticTab: 1 },
};

export const Fusion: Story = {
  args: { staticTab: 2 },
};

export const Schema: Story = {
  args: { staticTab: 3 },
};

export const Author: Story = {
  args: { staticTab: 4 },
};
