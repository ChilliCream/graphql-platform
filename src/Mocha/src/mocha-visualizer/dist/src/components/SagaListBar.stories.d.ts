import { Meta, StoryObj } from "@storybook/react";
import { SagaListBar } from "./SagaListBar";
declare const meta: Meta<typeof SagaListBar>;
export default meta;
type Story = StoryObj<typeof SagaListBar>;
export declare const SingleSaga: Story;
export declare const MultipleSagas: Story;
export declare const EmptySagas: Story;
export declare const WithBottomOffset: Story;
