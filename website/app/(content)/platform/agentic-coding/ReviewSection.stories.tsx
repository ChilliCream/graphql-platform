import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ReviewSection } from "./ReviewSection";

const meta = {
  title: "Pages/AgenticCoding/ReviewSection",
  component: ReviewSection,
  parameters: { layout: "fullscreen" },
  // The section plays a timed review script (viewed checkboxes, spinning
  // checks, approve, merge), so visual snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark px-10 py-6">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ReviewSection>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
