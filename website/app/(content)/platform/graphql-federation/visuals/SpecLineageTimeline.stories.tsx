import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AppMotionConfig } from "@/src/nitro/lib/motion";

import { SpecLineageTimeline } from "./SpecLineageTimeline";

const PANEL_WIDTHS = {
  375: 335,
  1024: 349.3259,
  1440: 496,
} as const;

const meta = {
  title: "Pages/Platform/GraphQLFederation/Visuals/SpecLineageTimeline",
  component: SpecLineageTimeline,
  parameters: { layout: "padded" },
} satisfies Meta<typeof SpecLineageTimeline>;

export default meta;
type Story = StoryObj<typeof meta>;

function panelStory(viewport: keyof typeof PANEL_WIDTHS): Story {
  return {
    decorators: [
      (Story) => (
        <AppMotionConfig reducedMotion="user">
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
