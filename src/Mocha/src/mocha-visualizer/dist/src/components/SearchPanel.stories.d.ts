import { Meta, StoryObj } from "@storybook/react";
import { SearchPanel } from "./SearchPanel";
declare const meta: Meta<typeof SearchPanel>;
export default meta;
type Story = StoryObj<typeof SearchPanel>;
export declare const Empty: Story;
export declare const WithResults: Story;
