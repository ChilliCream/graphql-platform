import type { ComponentType } from "react";

import { BrowserIcon } from "@/src/icons/BrowserIcon";
import { HandshakeIcon } from "@/src/icons/HandshakeIcon";
import { PhoneIcon } from "@/src/icons/PhoneIcon";
import { RobotIcon } from "@/src/icons/RobotIcon";

interface IconProps {
  readonly className?: string;
}

interface Protocol {
  readonly Icon: ComponentType<IconProps>;
  readonly title: string;
  readonly subtitle: string;
  readonly tags: readonly string[];
}

const PROTOCOLS: readonly Protocol[] = [
  {
    Icon: BrowserIcon,
    title: "Web",
    subtitle: "Browser SPA",
    tags: ["GraphQL"],
  },
  {
    Icon: PhoneIcon,
    title: "Mobile",
    subtitle: "Android / iOS",
    tags: ["GraphQL"],
  },
  {
    Icon: RobotIcon,
    title: "AI Agents",
    subtitle: "MCP Tools",
    tags: ["GraphQL", "gRPC"],
  },
  {
    Icon: HandshakeIcon,
    title: "Partners",
    subtitle: "Federated API",
    tags: ["OpenAPI", "gRPC"],
  },
];

function ProtocolTag({ label }: { readonly label: string }) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex items-center rounded-full border px-3 py-1 font-mono text-[0.7rem] tracking-[0.12em] uppercase">
      {label}
    </span>
  );
}

/**
 * "Choose your Protocol" grid: four consumer archetypes (Web, Mobile, AI
 * Agents, Partners), each in an icon tile with the protocols it speaks. The
 * Fusion flow above feeds a connector line into the top of each tile.
 */
export function ProtocolCards() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-16 sm:px-12 sm:py-20">
      <h2 className="font-heading text-cc-ink text-h4 sm:text-h3 text-center leading-[1.1] font-semibold text-balance">
        Choose your Protocol.
        <br />
        One Source of Truth.
      </h2>
      <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-center text-base text-pretty sm:text-lg">
        Unify all your APIs into a comprehensive company graph, streamlining
        data accessibility and enhancing integration. Transform the way you
        manage and interact with your data.
      </p>

      <div className="mt-14 grid grid-cols-2 gap-x-6 gap-y-12 sm:gap-x-10 lg:grid-cols-4">
        {PROTOCOLS.map(({ Icon, title, subtitle, tags }) => (
          <div key={title} className="flex flex-col items-center text-center">
            <div className="border-cc-card-border bg-cc-card-bg flex size-20 items-center justify-center rounded-2xl border">
              <Icon className="text-cc-nav-text size-9" />
            </div>
            <h3 className="font-heading text-cc-ink mt-5 text-xl font-semibold">
              {title}
            </h3>
            <p className="text-cc-ink-dim mt-1 text-sm">{subtitle}</p>
            <div className="mt-4 flex flex-wrap justify-center gap-2">
              {tags.map((tag) => (
                <ProtocolTag key={tag} label={tag} />
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
