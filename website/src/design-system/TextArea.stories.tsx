import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { TextArea } from "./TextArea";

const meta = {
  title: "Design System/TextArea",
  component: TextArea,
  argTypes: {
    label: { control: "text" },
    error: { control: "text" },
    placeholder: { control: "text" },
    required: { control: "boolean" },
    disabled: { control: "boolean" },
    rows: { control: { type: "number", min: 1, max: 20 } },
  },
  args: {
    label: "Message",
    placeholder: "Tell us about your project…",
    rows: 5,
  },
  decorators: [
    (Story) => (
      <div className="max-w-sm">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof TextArea>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Required: Story = {
  args: {
    required: true,
  },
};

export const WithError: Story = {
  args: {
    required: true,
    defaultValue: "Too short",
    error: "Please provide a few more details",
  },
};

export const Disabled: Story = {
  args: {
    defaultValue: "Sending is in progress…",
    disabled: true,
  },
};
