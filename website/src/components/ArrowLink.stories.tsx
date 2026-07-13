import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ArrowLink } from "./ArrowLink";

const meta = {
  title: "Components/ArrowLink",
  component: ArrowLink,
  argTypes: {
    href: { control: "text" },
    children: { control: "text" },
    className: { control: "text" },
  },
  args: {
    href: "/products/nitro",
    children: "Learn more",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ArrowLink>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children: "Learn more",
  },
};

export const External: Story = {
  args: {
    href: "https://example.com",
    children: "Visit site",
  },
};
