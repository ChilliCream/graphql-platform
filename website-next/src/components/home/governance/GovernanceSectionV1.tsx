import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

const ID = "governance-v1-";

/** Coral x-in-circle that marks the blocked breaking result. */
function BlockMark() {
  return (
    <svg
      id={`${ID}block-mark`}
      aria-hidden="true"
      viewBox="0 0 12 12"
      width="14"
      height="14"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      className="text-cc-status-firing"
    >
      <circle cx="6" cy="6" r="4.7" />
      <path d="M4.4 4.4 7.6 7.6M7.6 4.4 4.4 7.6" />
    </svg>
  );
}

/** One indented evidence row under the breaking result: dim label, ink value. */
function EvidenceLine({
  label,
  children,
}: {
  readonly label: string;
  readonly children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-0.5 sm:flex-row sm:gap-3">
      <span className="text-cc-nav-label shrink-0 sm:w-28">{label}</span>
      <span className="text-cc-ink">{children}</span>
    </div>
  );
}

/**
 * Governance section, take v1: an all-visible, illustration-forward proof. The
 * heading sits beside a terminal card running `nitro schema publish`. A safe
 * additive change passes in green, then a breaking removal fails in coral with
 * the traffic, client, and operation that justify the block. Nothing is hidden
 * behind tabs or clicks; everything reads at once.
 */
export function GovernanceSectionV1() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll className="grid grid-cols-1 items-center gap-10 lg:grid-cols-2 lg:gap-16">
        {/* Heading block. */}
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Governance
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 leading-[1.1] font-semibold text-balance">
            Break the build, not the client.
          </h2>
          <p className="text-cc-ink mt-5 max-w-3xl text-base text-pretty sm:text-lg">
            Every proposed schema change is checked against the clients you have
            published. A change that would break one fails the check, so it
            stops at your build, not in someone&rsquo;s app.
          </p>
          <a
            href="/platform/release-safety"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </a>
        </div>

        {/* Terminal card: a blocked publish/check. */}
        <div className="w-full lg:justify-self-end">
          <div className="border-cc-card-border bg-cc-surface mx-auto max-w-xl overflow-hidden rounded-2xl border shadow-[0_1px_3px_rgba(2,6,16,0.6)]">
            {/* Faint window header. Neutral dots only; status colors stay data. */}
            <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
              <span className="flex gap-1.5" aria-hidden="true">
                <span className="bg-cc-ink-faint size-2.5 rounded-full" />
                <span className="bg-cc-ink-faint size-2.5 rounded-full" />
                <span className="bg-cc-ink-faint size-2.5 rounded-full" />
              </span>
              <span className="text-cc-nav-label ml-auto font-mono text-[0.6875rem]">
                schema publish
              </span>
            </div>

            {/* Terminal body. */}
            <div className="p-4 font-mono text-xs leading-relaxed sm:p-5 sm:text-[0.8125rem]">
              {/* Prompt. */}
              <div className="flex items-center gap-2">
                <span
                  aria-hidden="true"
                  className="text-cc-nav-label select-none"
                >
                  $
                </span>
                <span className="text-cc-ink">nitro schema publish</span>
              </div>

              {/* Safe additive change passes, kept quiet. */}
              <div className="text-cc-status-healthy mt-3 flex items-center gap-2">
                <span aria-hidden="true" className="select-none">
                  +
                </span>
                <span>Order.note added</span>
                <span className="text-cc-status-healthy/70">safe</span>
              </div>

              {/* Breaking removal blocks the publish: the focal result. */}
              <div className="border-cc-status-firing/25 bg-cc-status-firing/[0.06] mt-2 rounded-xl border p-3 sm:p-3.5">
                <div className="flex items-start gap-2.5">
                  <span className="mt-px shrink-0">
                    <BlockMark />
                  </span>
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-x-2 gap-y-1.5">
                      <span className="border-cc-status-firing/50 bg-cc-status-firing/10 text-cc-status-firing inline-flex items-center rounded border px-1.5 py-0.5 text-[0.625rem] font-semibold tracking-[0.08em] uppercase">
                        Breaking
                      </span>
                      <span className="text-cc-ink">
                        <span className="text-cc-status-firing">
                          Product.rating
                        </span>{" "}
                        was removed
                      </span>
                    </div>

                    <div className="mt-2.5 space-y-1">
                      <EvidenceLine label="still queried">
                        <span className="text-cc-heading">4,213 requests</span>{" "}
                        in the last 7 days
                      </EvidenceLine>
                      <EvidenceLine label="by client">
                        <span className="text-cc-heading">web@2.4.0</span>{" "}
                        <span className="text-cc-ink-dim">
                          (operation{" "}
                          <span className="text-cc-heading">ProductCard</span>)
                        </span>
                      </EvidenceLine>
                    </div>

                    <p className="text-cc-status-firing mt-2.5 font-semibold">
                      publish blocked
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}
