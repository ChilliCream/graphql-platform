import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { OutlineButton, SolidButton } from "./Button";

const meta = {
  title: "Design System/Button",
  component: SolidButton,
  decorators: [
    (Story) => (
      <div className="cc-content-dark flex min-h-40 flex-wrap items-center justify-center gap-4 p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof SolidButton>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { children: "Book a Demo" },
  render: () => (
    <>
      <SolidButton href="/services/support/contact">Book a Demo</SolidButton>
      <OutlineButton href="https://nitro.chillicream.com">Launch</OutlineButton>
    </>
  ),
};
