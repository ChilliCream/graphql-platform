import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Admonition } from "./Admonition";

const meta = {
  title: "Design System/Admonition",
  component: Admonition,
  argTypes: {
    kind: {
      control: "select",
      options: [
        "note",
        "tip",
        "important",
        "warning",
        "caution",
        "experimental",
      ],
    },
  },
} satisfies Meta<typeof Admonition>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Note: Story = {
  args: {
    kind: "note",
    children: "Useful information that the user should know.",
  },
};

export const Tip: Story = {
  args: {
    kind: "tip",
    children: "A helpful suggestion to make the user's life easier.",
  },
};

export const Important: Story = {
  args: {
    kind: "important",
    children: "Crucial information necessary for users to succeed.",
  },
};

export const Warning: Story = {
  args: {
    kind: "warning",
    children: "Critical content demanding immediate user attention.",
  },
};

export const Caution: Story = {
  args: {
    kind: "caution",
    children: "Negative potential consequences of an action.",
  },
};

export const Experimental: Story = {
  args: {
    kind: "experimental",
    children:
      "An unstable feature whose API may change before it is finalized.",
  },
};

export const AllKinds: Story = {
  args: { kind: "note", children: "" },
  render: () => (
    <div className="flex flex-col gap-2 [&>*]:!my-0">
      <Admonition kind="note">Useful information.</Admonition>
      <Admonition kind="tip">A helpful suggestion.</Admonition>
      <Admonition kind="important">Important information.</Admonition>
      <Admonition kind="warning">Warning content.</Admonition>
      <Admonition kind="caution">Cautionary content.</Admonition>
      <Admonition kind="experimental">Experimental content.</Admonition>
    </div>
  ),
};
