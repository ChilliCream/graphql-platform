import Link from "next/link";

// Final ask. Distinct from the Enterprise band above: this one points the
// self-serve reader at the docs and the install line, not at sales. Lives on a
// glow band so the page ends on a punctuation mark rather than a bordered card.
export function PricingFooterCta() {
  return (
    <div className="cc-footer-cta">
      <div className="cc-footer-cta-inner">
        <div className="eyebrow">Ready when you are</div>
        <h2 className="display">Start free. Scale when you need to.</h2>
        <p>
          Every Nitro tier ships with hard limits, budget alerts, and the same
          OSS engine underneath. No lock-in, no surprise invoices.
        </p>
        <div
          className="cc-footer-install"
          aria-label="Install Hot Chocolate from NuGet"
        >
          <span className="prompt">$</span>
          <span className="cmd">dotnet add package </span>
          <span className="pkg">HotChocolate</span>
        </div>
        <div className="cc-cta-row">
          <Link
            href="https://nitro.chillicream.com"
            target="_blank"
            rel="noopener noreferrer"
            className="cc-btn cc-btn-primary"
          >
            Start free →
          </Link>
          <Link href="/docs" className="cc-footer-text-link">
            Read the docs →
          </Link>
        </div>
      </div>
    </div>
  );
}
