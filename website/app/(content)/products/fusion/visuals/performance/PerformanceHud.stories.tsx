import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AppMotionConfig } from "@/src/nitro/lib/motion";

import { PerformanceHud } from "./PerformanceHud";

const PANEL_WIDTHS = {
  375: 335,
  1024: 514.65625,
  1440: 720,
} as const;

const meta = {
  title: "Products/Fusion/Visuals/Performance/PerformanceHud",
  component: PerformanceHud,
  parameters: { layout: "padded" },
} satisfies Meta<typeof PerformanceHud>;

export default meta;
type Story = StoryObj<typeof meta>;

function panelStory(
  viewport: keyof typeof PANEL_WIDTHS,
  reduced = false,
): Story {
  return {
    decorators: [
      (Story) => (
        <AppMotionConfig reducedMotion={reduced ? "always" : "user"}>
          <div style={{ width: PANEL_WIDTHS[viewport], maxWidth: "100%" }}>
            <Story />
          </div>
        </AppMotionConfig>
      ),
    ],
  };
}

export const Panel375: Story = panelStory(375);
export const Panel1024: Story = panelStory(1024);
export const Panel1440: Story = panelStory(1440);
export const Panel1440Reduced: Story = panelStory(1440, true);
