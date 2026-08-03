import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CopyCommand } from "./CopyCommand";

const meta = {
  title: "Components/CopyCommand",
  component: CopyCommand,
  parameters: { layout: "fullscreen" },
  argTypes: {
    command: { control: "text" },
    className: { control: "text" },
    size: { control: "select", options: ["sm", "md"] },
  },
  args: {
    command: "dnx skillz add chillicream/agent-skills",
    className: "bg-cc-surface max-w-md",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof CopyCommand>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Small: Story = {
  args: {
    size: "sm",
    command:
      "dnx skillz add chillicream/agent-skills --skill graphql-schema-design",
    className: "bg-cc-surface max-w-sm",
  },
};
