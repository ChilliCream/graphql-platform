import { MessageActivity, MessageTrace } from "../types/trace";
interface TraceTimelineProps {
  trace: MessageTrace;
  hoveredActivityIds: ReadonlySet<string>;
  onActivityHover: (activity: MessageActivity | null) => void;
  onActivityClick: (activity: MessageActivity) => void;
}
export declare function TraceTimeline({
  trace,
  hoveredActivityIds,
  onActivityHover,
  onActivityClick,
}: TraceTimelineProps): import("react/jsx-runtime").JSX.Element;
export {};
