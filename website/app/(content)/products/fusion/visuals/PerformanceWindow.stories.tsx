import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PerformanceWindow } from "./PerformanceWindow";

/**
 * Widths match the Fusion FeatureRow panel's measured content box at the
 * page's gate viewports (375, 768, 1024, 1440, 1920), so the container
 * query the window relies on is exercised at the sizes it actually renders
 * at in production.
 */
const PANEL_WIDTHS = {
  375: 335,
  768: 672,
  1024: 514.65625,
  1440: 720,
  1920: 720,
} as const;

const meta = {
  title: "Products/Fusion/Visuals/PerformanceWindow",
  component: PerformanceWindow,
  parameters: { layout: "padded" },
} satisfies Meta<typeof PerformanceWindow>;

export default meta;
type Story = StoryObj<typeof meta>;

function panelStory(viewport: keyof typeof PANEL_WIDTHS): Story {
  return {
    decorators: [
      (Story) => (
        <div style={{ width: PANEL_WIDTHS[viewport], maxWidth: "100%" }}>
          <Story />
        </div>
      ),
    ],
  };
}

export const Panel375: Story = panelStory(375);
export const Panel768: Story = panelStory(768);
export const Panel1024: Story = panelStory(1024);
export const Panel1440: Story = panelStory(1440);
export const Panel1920: Story = panelStory(1920);
