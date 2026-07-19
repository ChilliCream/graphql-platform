import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { CYAN, GREEN, TEAL, VIOLET } from "./palette";
import { TransitMap } from "./TransitMap";

interface QueryRowProps {
  readonly code: string;
  readonly indent?: number;
  /** Gutter bar and service tag color; matches the map line that answers. */
  readonly color?: string;
  readonly service?: string;
  readonly dim?: boolean;
}

function QueryRow({ code, indent = 0, color, service, dim }: QueryRowProps) {
  return (
    <div className="flex items-center gap-2.5 font-mono text-[13px] leading-6">
      <span
        aria-hidden="true"
        className="h-4 w-1 shrink-0 rounded-full"
        style={{ background: color ?? "transparent" }}
      />
      <span
        className={dim ? "text-cc-ink-dim" : "text-cc-heading"}
        style={{ paddingLeft: indent * 14 }}
      >
        {code}
      </span>
      {service && (
        <>
          <span className="border-cc-card-border mx-1 flex-1 border-t border-dotted" />
          <span className="text-[11px]" style={{ color }}>
            {service}
          </span>
        </>
      )}
    </div>
  );
}

/** The hero's teaching device: one request, each field tagged with the line
 * (service) that answers it. The map's journey resolves this exact query. */
function QueryCard() {
  return (
    <div className="border-cc-card-border mt-8 hidden max-w-sm rounded-2xl border bg-[rgba(12,19,34,0.72)] p-4 backdrop-blur lg:block">
      <div className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.24em] uppercase">
        POST /graphql
      </div>
      <div className="mt-3">
        <QueryRow code="query GetProduct {" dim />
        <QueryRow
          code={'product(id: "P-401") {'}
          indent={1}
          color={TEAL}
          service="gateway"
        />
        <QueryRow code="name" indent={2} color={CYAN} service="catalog" />
        <QueryRow code="stock" indent={2} color={GREEN} service="inventory" />
        <QueryRow code="rating" indent={2} color={VIOLET} service="reviews" />
        <QueryRow code="}" indent={1} dim />
        <QueryRow code="}" dim />
      </div>
      <div className="border-cc-card-border text-cc-ink-dim mt-3 border-t pt-3 font-mono text-[11px]">
        one request · three services · one response
      </div>
    </div>
  );
}

export function Hero() {
  return (
    <section className="bg-cc-bg relative flex min-h-[92svh] items-center overflow-hidden">
      <TransitMap />
      {/* Bottom fade so the map settles into the page below. */}
      <div
        aria-hidden="true"
        className="absolute inset-x-0 bottom-0 h-32 bg-[linear-gradient(180deg,transparent,#0b0f1a)]"
      />
      <div className="relative z-10 mx-auto w-full max-w-6xl px-5 sm:px-12">
        <div className="max-w-xl py-14 xl:max-w-2xl">
          <div className="flex items-center gap-3">
            <span
              aria-hidden="true"
              className="h-px w-12 rounded-full"
              style={{
                background: `linear-gradient(90deg, ${TEAL}, transparent)`,
              }}
            />
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              Fusion · GraphQL Federation for .NET
            </span>
          </div>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6 text-balance">
            Many teams. Many services.{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage: `linear-gradient(90deg, ${TEAL}, ${CYAN})`,
              }}
            >
              One API.
            </span>
          </h1>
          <p className="text-cc-ink mt-6 max-w-xl text-base sm:text-lg">
            Every team runs its own line: its own schema, its own deploys, its
            own pace. Fusion composes them into one network and plans every
            query across it. Clients ride one API and never see the transfer.
          </p>
          <div className="mt-9 flex flex-wrap gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/fusion">Read the Docs</OutlineButton>
          </div>
          <QueryCard />
          <div className="text-cc-nav-label mt-8 flex items-center gap-3 font-mono text-[0.65rem] tracking-[0.24em] uppercase">
            <span aria-hidden="true" className="bg-cc-card-border h-px w-8" />
            Apollo Federation · Composite Schemas · One Gateway
          </div>
        </div>
      </div>
    </section>
  );
}
