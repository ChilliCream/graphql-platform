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

/** The previous layout: icon stacked above the title. */
export const Stacked: Story = {
  args: { layout: "stacked" },
};

/** What the page renders now: icon to the left of the title and body. */
export const IconLeft: Story = {
  args: { layout: "icon-left" },
};
