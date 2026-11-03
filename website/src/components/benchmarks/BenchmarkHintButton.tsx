interface BenchmarkHintButtonProps {
  readonly label: string;
  /** The id of the BenchmarkHintTooltip that the button shows. */
  readonly tooltipId: string;
  /** The key of a tooltip shared within the enclosing `data-hints` element, or undefined when the tooltip directly follows the button. */
  readonly hintKey: string | undefined;
}

/** A question mark button that shows a hint tooltip on hover and keyboard focus. BenchmarkHintTooltip styles it. */
export function BenchmarkHintButton({
  label,
  tooltipId,
  hintKey,
}: BenchmarkHintButtonProps) {
  return (
    <button
      type="button"
      data-hint-button
      data-hint-key={hintKey}
      aria-label={`About ${label}`}
      aria-describedby={tooltipId}
    />
  );
}
