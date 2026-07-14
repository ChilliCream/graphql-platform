import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Admonition } from "../design-system/Admonition";
import { ICON_NAMES, Icon } from "./Icon";

const meta = {
  title: "Design System/Icons",

  component: Icon,
} satisfies Meta<typeof Icon>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Icons: Story = {
  args: { icon: "arrow-right" },
  render: () => (
    <div>
      <Admonition kind="tip">
        Display icons with the{" "}
        <code className="text-cc-white">
          &lt;Icon icon=&quot;your-icon&quot; /&gt;
        </code>{" "}
        component.
      </Admonition>
      {ICON_NAMES.sort().map((name) => (
        <li
          key={name}
          className="hover:text-cc-white flex items-center gap-2 py-1"
        >
          <Icon icon={name} />
          <span className="font-mono">{name}</span>
        </li>
      ))}
    </div>
  ),
};
