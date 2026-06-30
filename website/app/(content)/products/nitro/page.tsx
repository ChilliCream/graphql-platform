import type { Metadata } from "next";

import { ContentSection } from "@/src/components/ContentSection";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { OutlineButton } from "@/src/design-system/Button";
import { Picture } from "@/src/design-system/Picture";

import { NitroDownload } from "./NitroDownload";

export const metadata: Metadata = {
  title: "Nitro",
  description:
    "Nitro is ChilliCream's GraphQL IDE: explore any GraphQL API with authentication flows, document sync, and subscriptions over SSE and WebSockets.",
};

const FEATURES: { title: string; description: string }[] = [
  {
    title: "Authentication Flows",
    description:
      "Choose between various authentication flows like basic, bearer or OAuth 2.",
  },
  {
    title: "Organization Workspaces",
    description:
      "Organize your GraphQL APIs and collaborate with colleagues across your organization with ease.",
  },
  {
    title: "Document Synchronization",
    description:
      "Keep your documents safe across all your devices and your teams.",
  },
  {
    title: "PWA Support",
    description:
      "Install Nitro as a PWA on your device without administrative privileges.",
  },
  {
    title: "Beautiful Themes",
    description:
      "Choose your preferred theme or auto-switch between dark and light.",
  },
  {
    title: "GraphQL File Upload",
    description:
      "Implements the latest GraphQL multipart request spec for uploads.",
  },
  {
    title: "Subscriptions over SSE",
    description: "Supports GraphQL subscriptions over Server-Sent Events.",
  },
  {
    title: "Performant GraphQL IDE",
    description:
      "Fast, snappy IDE — we keep an eye on performance so you get your work done fast.",
  },
  {
    title: "Subscriptions over WS",
    description:
      "Supports GraphQL subscriptions over WebSockets and the Apollo subscription protocol.",
  },
];

export default function NitroPage() {
  return (
    <>
      <PageHero
        eyebrow="GraphQL IDE / API Cockpit"
        title="Nitro"
        teaser="Next-Level GraphQL IDE for developers. Works with any GraphQL API. Beautiful, fast, and feature-rich."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <NitroDownload />
        <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
      </div>

      <div className="mt-20 flex justify-center">
        <Picture
          src="/images/nitro/nitro-app.png"
          alt="Nitro App"
          width={1451}
          height={852}
          sizes="(max-width: 1280px) 100vw, 1200px"
          priority
          className="h-auto w-full max-w-[1200px]"
        />
      </div>

      <Section title="Batteries Included">
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((feature) => (
            <div
              key={feature.title}
              className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6 backdrop-blur-sm"
            >
              <h3 className="text-cc-ink text-lg font-semibold">
                {feature.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm">
                {feature.description}
              </p>
            </div>
          ))}
        </div>
      </Section>

      <ContentSection
        title="Built for Modern API Workflows"
        text="Nitro plays nicely with the rest of the ChilliCream platform. Schema & client registry, Fusion management, OpenTelemetry analytics, document sharing, and SSO are all built in — no plugins required."
      />
    </>
  );
}
