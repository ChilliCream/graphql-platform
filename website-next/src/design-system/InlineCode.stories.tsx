import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { InlineCode } from "./InlineCode";

const meta = {
  title: "Design System/InlineCode",
  component: InlineCode,
} satisfies Meta<typeof InlineCode>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { children: "useState()" },
};

export const InProse: Story = {
  args: { children: "" },
  render: () => (
    <p className="text-base text-stone-800">
      Use the <InlineCode>useState</InlineCode> hook to add local state to a
      component, then pass the value down via props or read it from{" "}
      <InlineCode>context</InlineCode>.
    </p>
  ),
};
