import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Link } from "./Link";

const meta = {
  title: "Design System/Link",
  component: Link,
} satisfies Meta<typeof Link>;

export default meta;
type Story = StoryObj<typeof meta>;

export const InProse: Story = {
  args: { href: "/docs", children: "" },
  render: () => (
    <p className="text-base text-stone-800">
      Visit our <Link href="/docs">documentation</Link> or check the{" "}
      <Link href="https://github.com/ChilliCream/graphql-platform">
        GitHub repository
      </Link>{" "}
      for more details.
    </p>
  ),
};
