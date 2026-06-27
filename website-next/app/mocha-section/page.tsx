import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Mocha Section Takes",
  description:
    "Five all-visible takes of the Mocha messaging section that sits after Different Protocols, above pricing.",
  robots: { index: false, follow: false },
};

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

interface Take {
  readonly v: number;
  readonly name: string;
  readonly summary: string;
}

const TAKES: readonly Take[] = [
  {
    v: 1,
    name: "Sequence",
    summary:
      "One time-axis sequence: the request enters, the response returns early, and the handler, event, and saga work continues after it.",
  },
  {
    v: 2,
    name: "In-process / across services",
    summary:
      "One shared handler shown two ways: the mediator in-process and the bus across services, with sagas and exactly-once below.",
  },
  {
    v: 3,
    name: "One message, end to end",
    summary:
      "Follow a single message along the reliability path: publish, outbox in the same transaction, transport, inbox dedupe, handler.",
  },
  {
    v: 4,
    name: "Capabilities",
    summary:
      "The five capabilities laid out together as cards, each with a one-line description and a small diagram. The most scannable take.",
  },
  {
    v: 5,
    name: "At a glance",
    summary:
      "One system panel with every part visible at once: mediator, bus, saga, transports, and the outbox-to-inbox exactly-once path.",
  },
  {
    v: 6,
    name: "Every app runs on events",
    summary:
      "Substrate: the visible request on top, the events that carry the real work underneath. Messaging as the layer the app runs on.",
  },
  {
    v: 7,
    name: "Your app is mostly side effects",
    summary:
      "One thing happens and the rest follows: an order is placed, then stock, payment, confirmation, and shipping react on their own.",
  },
  {
    v: 8,
    name: "Simple, and it scales",
    summary:
      "A resolver is just a query handler that batches with a DataLoader: simple to write, and it scales across traffic, codebase, and team.",
  },
];

function Eyebrow({ children }: { readonly children: ReactNode }) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

export default function MochaSectionHubPage() {
  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-16 px-5 py-16 sm:px-12 sm:py-24">
      <header>
        <Eyebrow>Internal · messaging section</Eyebrow>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-semibold tracking-tight">
          Mocha Section{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Takes
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Five all-visible takes of the messaging section that sits after
          Different Protocols and above pricing. Same plain frame and beats,
          five different structures. Previewed full width with the real chrome
          and the pricing section below.
        </p>
      </header>

      <section className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
        {TAKES.map((take) => (
          <Link
            key={take.v}
            href={`/mocha-section/v${take.v}`}
            className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-5 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
          >
            <div className="flex items-center gap-3">
              <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
                v{take.v}
              </span>
              <Eyebrow>Take {take.v}</Eyebrow>
            </div>
            <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
              {take.name}
            </p>
            <p className="text-cc-ink text-[0.95rem] leading-relaxed">
              {take.summary}
            </p>
            <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
              Open take →
            </span>
          </Link>
        ))}
      </section>
    </div>
  );
}
