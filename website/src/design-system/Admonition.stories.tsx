import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Admonition } from "./Admonition";

const meta = {
  title: "Design System/Admonition",
  component: Admonition,
  argTypes: {
    kind: {
      control: "select",
      options: ["note", "tip", "warning", "caution", "experimental"],
    },
  },
} satisfies Meta<typeof Admonition>;

export default meta;
type Story = StoryObj<typeof meta>;

export const AllKinds: Story = {
  args: { kind: "note", children: "" },
  render: () => (
    <div className="flex flex-col gap-2 *:my-0!">
      <Admonition kind="note">Useful information.</Admonition>
      <Admonition kind="tip">A helpful suggestion.</Admonition>
      <Admonition kind="warning">Warning content.</Admonition>
      <Admonition kind="caution">Cautionary content.</Admonition>
      <Admonition kind="experimental">Experimental content.</Admonition>
    </div>
  ),
};
