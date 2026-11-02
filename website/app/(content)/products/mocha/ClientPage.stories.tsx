import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ClientPage } from "./ClientPage";

const meta = {
  title: "Pages/Mocha/ProductPage",
  component: ClientPage,
  parameters: { layout: "fullscreen" },
  // The hero board animates on a canvas rAF loop and every section visual
  // runs its own timed animation, so visual snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ClientPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
