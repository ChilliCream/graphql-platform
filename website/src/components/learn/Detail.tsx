interface DetailProps {
  readonly label: string;
  readonly value: string;
  readonly className?: string;
}

/**
 * A single `dt`/`dd` fact pair for the learn detail-page sidebars
 * (`TemplateDetail`, `LearnVideoDetail`). Shared per the hnm.1 review
 * (website-8s5.3 comment 2): the two pages carried byte-identical markup.
 */
export function Detail({ label, value, className = "" }: DetailProps) {
  return (
    <div>
      <dt className="text-cc-ink-dim font-mono text-[0.6875rem] tracking-wider uppercase">{label}</dt>
      <dd className={`text-cc-heading mt-1 ${className}`.trim()}>{value}</dd>
    </div>
  );
}
