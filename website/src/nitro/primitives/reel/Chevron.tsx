import { StrokeIcon } from "./StrokeIcon";

interface ChevronProps {
  readonly up?: boolean;
}

const CHEVRON_UP_PATH = "M6 15l6-6 6 6";
const CHEVRON_DOWN_PATH = "M6 9l6 6 6-6";

export function Chevron({ up }: ChevronProps) {
  return (
    <StrokeIcon
      d={up ? CHEVRON_UP_PATH : CHEVRON_DOWN_PATH}
      size={14}
      strokeWidth={1.8}
    />
  );
}
