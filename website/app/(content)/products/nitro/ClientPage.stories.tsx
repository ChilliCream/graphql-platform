import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ClientPage } from "./ClientPage";

const meta = {
  title: "Pages/Nitro/ProductPage",
  component: ClientPage,
  parameters: { layout: "fullscreen" },
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
