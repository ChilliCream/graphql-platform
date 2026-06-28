import type { Metadata } from "next";

import { ContentSection } from "@/src/components/ContentSection";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";

export const metadata: Metadata = {
  title: "Ecosystem",
  description:
    "Explore the ChilliCream ecosystem: GraphQL tooling with authentication flows, document sync, subscriptions, and a fast IDE built for your API journey.",
};

interface FeatureCard {
  title: string;
  description: string;
}

const FEATURES: FeatureCard[] = [
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
      "Use your favorite Browser to install Nitro as a PWA on your Device without requiring administrative privileges.",
  },
  {
    title: "Beautiful Themes",
    description:
      "Choose your single preferred theme or let the system automatically switch between dark and light theme.",
  },
  {
    title: "GraphQL File Upload",
    description:
      "Implements the latest version of the GraphQL multipart request spec.",
  },
  {
    title: "Subscriptions over SSE",
    description: "Supports GraphQL subscriptions over Server-Sent Events.",
  },
  {
    title: "Performant GraphQL IDE",
    description:
      "Lagging apps can be frustrating. We do not accept that and keep always an eye on performance so that you can get your task done fast.",
  },
  {
    title: "Subscriptions over WS",
    description:
      "Supports GraphQL subscriptions over WebSocket as well as the Apollo subscription protocol.",
  },
];

export default function EcosystemPage() {
  return (
    <>
      <PageHero
        title="An Ecosystem You Love"
        teaser="A harmonious blend of tools and community, dedicated to enhancing your API journey. Experience simplicity, efficiency, and collaborative innovation."
      />
      <NextStepsSection
        title="Lead by Intuition"
        text="A framework built by developers for developers. Combining ease of use with high-speed performance, it's designed to elevate your projects effortlessly."
        primaryLink="/docs/hotchocolate"
        primaryLinkText="Get Started"
        secondaryLink="https://nitro.chillicream.com"
        secondaryLinkText="Launch"
      />
      <Section title="Batteries Included">
        <p className="text-cc-ink-dim -mt-4 mb-8 text-center text-base sm:text-lg">
          Everything you need to build great APIs &mdash; and more
        </p>
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
        title="Continuous Evolution"
        text="Embracing the latest GraphQL specification drafts and future updates, this platform ensures users are always at the cutting edge. Experience an evolving GraphQL journey, where innovation and up-to-date features converge seamlessly."
        imageSrc="/images/ecosystem/continuous-evolution.png"
        imageAlt="Continuous Evolution"
        imageMaxWidth={1200}
      />
    </>
  );
}
