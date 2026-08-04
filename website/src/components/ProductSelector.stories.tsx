import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { ProductSelector } from "./ProductSelector";

const meta = {
  title: "Components/ProductSelector",
  component: ProductSelector,
  parameters: {
    layout: "padded",
  },
  // The selector is full-width; in the docs sidebar it sits in a ~288px column.
  decorators: [
    (Story) => (
      <div style={{ width: 288 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ProductSelector>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Example: Story = {
  args: {
    activeSlug: "hotchocolate",
  },
};
