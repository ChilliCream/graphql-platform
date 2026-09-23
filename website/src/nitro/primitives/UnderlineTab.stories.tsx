import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { UnderlineTab } from "./UnderlineTab";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const meta = {
  title: "Nitro/Primitives/UnderlineTab",
  component: UnderlineTab,
  parameters: { layout: "centered" },
  argTypes: {
    style: { control: false },
  },
  args: {
    label: "Reference",
    active: true,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof UnderlineTab>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Inactive: Story = {
  args: { active: false },
};

export const TabStrip: Story = {
  render: () => (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 18,
        height: 36,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <UnderlineTab label="Reference" active={false} />
      <UnderlineTab label="SDL" active={false} />
      <UnderlineTab label="Insights" active />
    </div>
  ),
};

export const ColumnHeaderTitle: Story = {
  render: () => (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        height: 36,
        padding: "0 10px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <UnderlineTab
        label="Request"
        active
        fontSize={14}
        fontWeight={600}
        height="100%"
      />
    </div>
  ),
};

export const SectionLabel: Story = {
  render: () => (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 16,
        height: 30,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
        fontSize: 12,
      }}
    >
      <UnderlineTab
        label="GraphQL Variables"
        active
        fontSize={12}
        underlineOffset={-8}
      />
      <span style={{ color: token.textSecondary }}>HTTP Headers</span>
    </div>
  ),
};

export const FlyoutIndicator: Story = {
  render: () => (
    <div
      style={{
        display: "flex",
        alignItems: "stretch",
        height: 36,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <UnderlineTab
        label="Details"
        active
        color={token.active}
        underlineInset={6}
        style={{ padding: "0 12px" }}
      />
      <UnderlineTab
        label="History"
        active={false}
        color={token.active}
        underlineInset={6}
        style={{ padding: "0 12px" }}
      />
    </div>
  ),
};
