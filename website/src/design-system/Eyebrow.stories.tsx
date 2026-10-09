import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Eyebrow } from "./Eyebrow";

const meta = {
  title: "Design System/Eyebrow",
  component: Eyebrow,
  argTypes: {
    size: {
      control: "select",
      options: ["2xs", "xs"],
    },
    color: {
      control: "select",
      options: ["nav-label", "ink-dim", "accent"],
    },
  },
} satisfies Meta<typeof Eyebrow>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    children: "Section label",
  },
};

export const Sizes: Story = {
  args: { children: "" },
  render: () => (
    <div className="flex flex-col items-start gap-3">
      <Eyebrow size="xs">Extra small</Eyebrow>
      <Eyebrow size="2xs">Smaller still</Eyebrow>
    </div>
  ),
};

export const Colors: Story = {
  args: { children: "" },
  render: () => (
    <div className="flex flex-col items-start gap-3">
      <Eyebrow color="nav-label">Nav label</Eyebrow>
      <Eyebrow color="ink-dim">Ink dim</Eyebrow>
      <Eyebrow color="accent">Accent</Eyebrow>
    </div>
  ),
};

export const SemiboldWeight: Story = {
  args: {
    children: "Get in touch",
    color: "accent",
    className: "font-semibold",
  },
};

export const AsTableHeader: Story = {
  args: { children: "" },
  render: () => (
    <table>
      <thead>
        <tr>
          <Eyebrow as="th" size="2xs">
            Capability
          </Eyebrow>
        </tr>
      </thead>
    </table>
  ),
};
