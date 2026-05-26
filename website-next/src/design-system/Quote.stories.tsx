import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Quote } from "./Quote";

const meta = {
  title: "Design System/Quote",
  component: Quote,
} satisfies Meta<typeof Quote>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children:
      "The best way to predict the future is to invent it. — Alan Kay",
  },
};

export const Multiline: Story = {
  args: {
    children: (
      <>
        <p>
          Premature optimization is the root of all evil (or at least most of
          it) in programming.
        </p>
        <p className="mt-2">— Donald Knuth</p>
      </>
    ),
  },
};
