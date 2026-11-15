import { useId, type ReactNode } from "react";
import { BenchmarkHintButton } from "@/src/components/benchmarks/BenchmarkHintButton";
import { BenchmarkHintTooltip } from "@/src/components/benchmarks/BenchmarkHintTooltip";

interface BenchmarkColumnHintProps {
  readonly label: string;
  readonly children: ReactNode;
}

/** A question mark button after a column label that shows the column's explanation on hover and keyboard focus. */
export function BenchmarkColumnHint({
  label,
  children,
}: BenchmarkColumnHintProps) {
  const id = useId();

  return (
    <>
      <BenchmarkHintButton label={label} tooltipId={id} hintKey={undefined} />
      <BenchmarkHintTooltip id={id} hintKey={undefined}>
        {children}
      </BenchmarkHintTooltip>
    </>
  );
}
