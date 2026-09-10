import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { ButtonRow } from "./ButtonRow";

const meta = {
  title: "Components/ButtonRow",
  component: ButtonRow,
  parameters: { layout: "fullscreen" },
  argTypes: {
    align: {
      control: "select",
      options: ["center", "start", "stacked"],
    },
    className: { control: "text" },
    children: { control: false },
  },
  args: {
    align: "center",
    children: (
      <>
        <SolidButton href="#">Get started</SolidButton>
        <OutlineButton href="#">Talk to us</OutlineButton>
      </>
    ),
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ButtonRow>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Center: Story = {
  args: { align: "center" },
};

export const Start: Story = {
  args: { align: "start" },
};

export const Stacked: Story = {
  args: {
    align: "stacked",
    children: (
      <>
        <SolidButton href="#" className="w-full">
          Get started
        </SolidButton>
        <OutlineButton href="#" className="w-full">
          Talk to us
        </OutlineButton>
      </>
    ),
  },
};
