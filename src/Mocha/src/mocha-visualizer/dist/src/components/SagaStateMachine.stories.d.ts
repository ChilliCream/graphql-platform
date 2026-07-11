import { Meta, StoryObj } from "@storybook/react";
import { SagaStateMachine } from "./SagaStateMachine";
declare const meta: Meta<typeof SagaStateMachine>;
export default meta;
type Story = StoryObj<typeof SagaStateMachine>;
export declare const CompactMode: Story;
export declare const FocusMode: Story;
export declare const SingleState: Story;
export declare const NoTransitions: Story;
