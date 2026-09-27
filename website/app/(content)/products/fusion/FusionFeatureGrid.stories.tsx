import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { FusionFeatureGrid } from "./FusionFeatureGrid";

const meta = {
  title: "Pages/Products/Fusion/FusionFeatureGrid",
  component: FusionFeatureGrid,
  parameters: { layout: "fullscreen" },
  argTypes: {
    layout: {
      control: "select",
      options: ["stacked", "icon-left"],
    },
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark px-5 py-8 sm:px-12">
        <div className="mx-auto max-w-7xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof FusionFeatureGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The layout shipped on the page today: icon stacked above the title. */
export const Stacked: Story = {
  args: { layout: "stacked" },
};

/**
 * Comparison variant under review: icon to the left of the title and body,
 * top-aligned. Not wired into the page yet.
 */
export const IconLeft: Story = {
  args: { layout: "icon-left" },
};
