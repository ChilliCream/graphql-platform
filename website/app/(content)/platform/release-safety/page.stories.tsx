import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import ReleaseSafetyPage from "./page";

const meta = {
  title: "Pages/ReleaseSafety/Page",
  component: ReleaseSafetyPage,
  parameters: { layout: "fullscreen" },
  // The page includes continuously running CSS animations (spinners, the
  // staging gate transition, the validating ellipsis), so visual snapshots
  // would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ReleaseSafetyPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
