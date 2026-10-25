import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Input } from "./Input";

const meta = {
  title: "Design System/Input",
  component: Input,
  argTypes: {
    label: { control: "text" },
    error: { control: "text" },
    placeholder: { control: "text" },
    required: { control: "boolean" },
    disabled: { control: "boolean" },
    type: {
      control: "select",
      options: ["text", "email", "password", "number", "tel", "url"],
    },
  },
  args: {
    label: "Name",
    type: "text",
    placeholder: "Ada Lovelace",
  },
  decorators: [
    (Story) => (
      <div className="max-w-sm">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Input>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Required: Story = {
  args: {
    label: "Email",
    type: "email",
    placeholder: "ada@example.com",
    required: true,
  },
};

export const WithError: Story = {
  args: {
    label: "Email",
    type: "email",
    required: true,
    defaultValue: "not-an-email",
    error: "Please enter a valid email address",
  },
};

export const Disabled: Story = {
  args: {
    defaultValue: "Ada Lovelace",
    disabled: true,
  },
};

export const Unlabeled: Story = {
  args: {
    label: undefined,
    placeholder: "Search…",
  },
};
