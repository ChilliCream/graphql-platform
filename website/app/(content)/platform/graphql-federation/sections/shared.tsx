import Link from "next/link";
import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";

export const SPEC_URL =
  "https://graphql.github.io/composite-schemas-spec/draft/";

export const GRADIENT = "linear-gradient(90deg, #5eead4, #16b9e4)";

export const DEVELOPER_EYEBROW = "For GraphQL developers";

export const LINK_CLASS =
  "text-cc-accent hover:text-cc-accent-hover underline underline-offset-4";

interface SectionProps {
  readonly id: string;
  readonly children: ReactNode;
}

export function Section({ id, children }: SectionProps) {
  return (
    <section id={id} className="border-cc-card-border scroll-mt-24 border-t">
      <PageSection maxWidth="6xl" className="py-16 sm:py-24">
        {children}
      </PageSection>
    </section>
  );
}

interface IntroProps {
  readonly eyebrow?: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

export function Intro({ eyebrow, title, children }: IntroProps) {
  return (
    <div className="max-w-2xl">
      <SectionHeading eyebrow={eyebrow} title={title} />
      {children && (
        <div className="text-cc-ink mt-5 space-y-4 text-base">{children}</div>
      )}
    </div>
  );
}

export function SubHeading({
  id,
  children,
}: {
  readonly id: string;
  readonly children: ReactNode;
}) {
  return (
    <h3
      id={id}
      className="font-heading text-cc-heading text-h5 scroll-mt-24 font-semibold text-balance"
    >
      {children}
    </h3>
  );
}

export function SceneReveal({ children }: { readonly children: ReactNode }) {
  return (
    <RevealOnScroll className="mt-12" hiddenClassName="translate-y-8 opacity-0">
      {children}
    </RevealOnScroll>
  );
}

export function Code({ children }: { readonly children: string }) {
  return (
    <code className="text-cc-heading rounded bg-[rgba(245,241,234,0.06)] px-1 py-0.5 font-mono text-[0.85em] whitespace-nowrap">
      {children}
    </code>
  );
}

export function ExternalLink({
  href,
  children,
}: {
  readonly href: string;
  readonly children: ReactNode;
}) {
  return (
    <a className={LINK_CLASS} href={href} rel="noopener" target="_blank">
      {children}
    </a>
  );
}

export function InPractice({
  href,
  children,
}: {
  readonly href: string;
  readonly children: ReactNode;
}) {
  return (
    <p className="text-cc-ink-dim text-sm">
      In practice:{" "}
      <Link className={LINK_CLASS} href={href}>
        {children}
      </Link>
      .
    </p>
  );
}

interface TableColumn {
  readonly header: string;
  readonly mono?: boolean;
}

interface TableProps {
  readonly caption: string;
  readonly columns: readonly TableColumn[];
  readonly rows: readonly (readonly string[])[];
  readonly minWidth: string;
}

export function Table({ caption, columns, rows, minWidth }: TableProps) {
  return (
    <div className="overflow-x-auto">
      <table className={`w-full border-collapse text-left ${minWidth}`}>
        <caption className="sr-only">{caption}</caption>
        <thead>
          <tr className="text-cc-nav-label font-mono text-xs tracking-[0.16em] uppercase">
            {columns.map((column, i) => (
              <th
                key={column.header}
                scope="col"
                className={`border-cc-card-border border-b py-3 font-semibold ${
                  i < columns.length - 1 ? "pr-6" : ""
                }`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="text-cc-ink text-sm">
          {rows.map((row) => (
            <tr key={row[0]}>
              {row.map((cell, i) => (
                <td
                  key={columns[i].header}
                  className={`border-cc-card-border border-b py-4 align-top ${
                    i < columns.length - 1 ? "pr-6" : ""
                  } ${
                    columns[i].mono
                      ? "text-cc-heading font-mono text-[13px]"
                      : ""
                  }`}
                >
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
