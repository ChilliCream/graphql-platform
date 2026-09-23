import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import {
  controlBaseClasses,
  controlBorderClasses,
  FormField,
} from "./FormField";

const exampleInput = (hasError: boolean) => (
  <input
    id="demo-email"
    type="email"
    placeholder="ada@example.com"
    defaultValue={hasError ? "not-an-email" : undefined}
    className={`${controlBaseClasses} ${controlBorderClasses(hasError)}`}
  />
);

const meta = {
  title: "Design System/FormField",
  component: FormField,
  argTypes: {
    htmlFor: { control: "text" },
    label: { control: "text" },
    required: { control: "boolean" },
    error: { control: "text" },
    children: { control: false },
  },
  args: {
    htmlFor: "demo-email",
    label: "Email",
    children: exampleInput(false),
  },
  decorators: [
    (Story) => (
      <div className="max-w-sm">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof FormField>;

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
    error: "Please enter a valid email address",
    children: exampleInput(true),
  },
};

export const Unlabeled: Story = {
  args: {
    label: undefined,
  },
};
