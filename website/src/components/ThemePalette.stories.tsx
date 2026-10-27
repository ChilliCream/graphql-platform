import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, within } from "storybook/test";
import "@docsearch/css/dist/style.css";
import { Admonition } from "@/src/design-system/Admonition";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Dropdown, DropdownItem } from "@/src/design-system/Dropdown";
import { Input } from "@/src/design-system/Input";
import { Link } from "@/src/design-system/Link";
import { Tab, Tabs } from "@/src/design-system/Tabs";
import { Tag } from "@/src/design-system/Tag";

interface ThemePaletteProps {
  readonly scheme: "light" | "dark" | "auto";
  readonly mobile: boolean;
}

function SearchPreview() {
  return (
    <section
      aria-label="Local search preview"
      className="DocSearch-Container"
      style={{
        position: "relative",
        width: "100%",
        height: "auto",
        zIndex: 0,
        padding: "1rem",
      }}
    >
      <div className="DocSearch-Modal" style={{ margin: 0, width: "100%" }}>
        <header className="DocSearch-SearchBar">
          <form
            className="DocSearch-Form"
            onSubmit={(event) => event.preventDefault()}
          >
            <input
              aria-label="Search documentation"
              className="DocSearch-Input"
              placeholder="Search documentation"
            />
          </form>
        </header>
        <div
          className="DocSearch-Dropdown"
          style={{ height: "auto", minHeight: 0 }}
        >
          <p className="DocSearch-Help text-sm">
            Search results are local fixtures.
          </p>
          <ul
            role="listbox"
            aria-label="Search results"
            className="DocSearch-Hits"
          >
            <li role="option" aria-selected="true" className="DocSearch-Hit">
              <a href="#palette">
                <div className="DocSearch-Hit-Container">
                  <div className="DocSearch-Hit-content-wrapper">
                    <span className="DocSearch-Hit-title">
                      Getting started with <mark>Fusion</mark>
                    </span>
                    <span className="DocSearch-Hit-path">
                      Documentation / Introduction
                    </span>
                  </div>
                </div>
              </a>
            </li>
          </ul>
          <p
            className="p-3 text-sm"
            style={{
              color: "var(--docsearch-error-text-color)",
              background: "var(--docsearch-error-soft-color)",
            }}
          >
            Search is unavailable. Please try again.
          </p>
        </div>
        <div className="DocSearch-Footer">
          <ul className="DocSearch-Commands">
            <li>Enter to select</li>
            <li>Esc to close</li>
          </ul>
        </div>
      </div>
    </section>
  );
}

function ThemePalette({ scheme, mobile }: ThemePaletteProps) {
  return (
    <main
      id="palette"
      className="cc-content-dark text-cc-ink mx-auto p-6 sm:p-8"
      style={{ maxWidth: mobile ? 390 : 1100 }}
    >
      <h1 className="font-heading text-h4 text-cc-heading font-bold">
        {scheme} palette
      </h1>
      <p className="text-cc-prose mt-3" data-contrast="prose">
        A cool, quiet surface for documentation and product interfaces.
      </p>
      <p className="text-cc-ink-dim mt-2 text-sm" data-contrast="muted">
        Muted navigation and supporting text remain readable.
      </p>
      <p className="my-3">
        <Link href="#palette">Explore the documentation</Link>
      </p>
      <div className="my-5 flex flex-wrap gap-3">
        <SolidButton>Book a demo</SolidButton>
        <OutlineButton>Learn more</OutlineButton>
        <SolidButton disabled>Unavailable</SolidButton>
        <button className="bg-cc-cta text-cc-white hover:bg-cc-cta-hover rounded-full px-7 py-3 text-sm">
          Launch Nitro
        </button>
        <Tag href="#palette">GraphQL</Tag>
      </div>
      <Card variant="tile" hoverBorder>
        <h2 className="font-heading text-h6 text-cc-heading">
          Theme-safe controls
        </h2>
        <div className={`mt-4 grid gap-4 ${mobile ? "" : "sm:grid-cols-2"}`}>
          <Input label="Email" placeholder="you@example.com" />
          <Input
            label="Project"
            defaultValue="Invalid project"
            error="Enter a valid project name."
          />
          <Input label="Disabled input" placeholder="Not available" disabled />
          <Dropdown
            label="Environment"
            trigger="Production"
            defaultOpen
            className="mb-28"
          >
            <ul>
              <DropdownItem active description="Live traffic">
                Production
              </DropdownItem>
              <DropdownItem description="Preview changes">Staging</DropdownItem>
            </ul>
          </Dropdown>
        </div>
        <Tabs>
          <Tab label="Overview">
            <p className="text-cc-prose">
              The selected tab uses the accent color.
            </p>
          </Tab>
          <Tab label="Details">Additional details.</Tab>
        </Tabs>
      </Card>
      <div className={`mt-6 grid gap-x-5 ${mobile ? "" : "sm:grid-cols-2"}`}>
        <Admonition kind="note">Useful information.</Admonition>
        <Admonition kind="tip">A helpful suggestion.</Admonition>
        <Admonition kind="warning">Check before continuing.</Admonition>
        <Admonition kind="caution">This action cannot be undone.</Admonition>
        <Admonition kind="experimental">Features may change.</Admonition>
      </div>
      <SearchPreview />
    </main>
  );
}

function rgba(color: string): readonly number[] {
  const context = document.createElement("canvas").getContext("2d")!;
  context.fillStyle = color;
  context.fillRect(0, 0, 1, 1);
  const [r, g, b, a] = context.getImageData(0, 0, 1, 1).data;
  return [r, g, b, a / 255];
}

function composite(
  foreground: readonly number[],
  background: readonly number[],
) {
  return foreground
    .slice(0, 3)
    .map(
      (value, i) => value * foreground[3] + background[i] * (1 - foreground[3]),
    );
}

function luminance(color: readonly number[]) {
  const linear = color.map((value) => {
    const channel = value / 255;
    return channel <= 0.04045
      ? channel / 12.92
      : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
}

function contrast(
  element: Element,
  property: "color" | "borderTopColor" | "borderBottomColor" = "color",
) {
  const ancestors: Element[] = [];
  for (
    let current: Element | null = element;
    current;
    current = current.parentElement
  ) {
    ancestors.unshift(current);
  }
  const background = ancestors.reduce<readonly number[]>(
    (color, ancestor) =>
      composite(rgba(getComputedStyle(ancestor).backgroundColor), color),
    [255, 255, 255],
  );
  const foreground = composite(
    rgba(getComputedStyle(element)[property]),
    background,
  );
  const values = [luminance(foreground), luminance(background)].sort(
    (a, b) => a - b,
  );
  return (values[1] + 0.05) / (values[0] + 0.05);
}

const meta = {
  title: "Components/ThemePalette",
  component: ThemePalette,
  parameters: { layout: "fullscreen" },
  args: { scheme: "light", mobile: false },
  beforeEach: ({ args }) => {
    const previous = document.documentElement.getAttribute("data-theme");
    if (args.scheme === "auto") {
      document.documentElement.removeAttribute("data-theme");
    } else {
      document.documentElement.dataset.theme = args.scheme;
    }
    return () => {
      if (previous === null) {
        document.documentElement.removeAttribute("data-theme");
      } else {
        document.documentElement.setAttribute("data-theme", previous);
      }
    };
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const root = getComputedStyle(document.documentElement);
    const resolved =
      args.scheme === "auto"
        ? matchMedia("(prefers-color-scheme: light)").matches
          ? "light"
          : "dark"
        : args.scheme;
    expect(root.colorScheme).toBe(resolved);
    expect(root.getPropertyValue("--color-cc-white").trim()).toBe("#fff");
    expect(root.getPropertyValue("--color-cc-black").trim()).toBe("#000");
    const solid = canvas.getByRole("button", { name: "Book a demo" });
    expect(contrast(solid)).toBeGreaterThanOrEqual(4.5);
    expect(solid).toHaveClass("hover:bg-cc-solid-hover");
    const labelLuminance = luminance(rgba(getComputedStyle(solid).color));
    const hoverLuminance = luminance(
      rgba(root.getPropertyValue("--color-cc-solid-hover")),
    );
    expect(
      (Math.max(labelLuminance, hoverLuminance) + 0.05) /
        (Math.min(labelLuminance, hoverLuminance) + 0.05),
    ).toBeGreaterThanOrEqual(4.5);
    if (resolved === "dark") {
      for (const [token, value] of Object.entries({
        "--color-cc-heading": "#f5f0ea",
        "--color-cc-bg": "#0b0f1a",
        "--color-cc-surface": "#0c1322",
        "--color-cc-accent": "#16b9e4",
        "--color-cc-code-bg": "#0d1117",
      })) {
        expect(root.getPropertyValue(token).trim()).toBe(value);
      }
      expect(root.getPropertyValue("--cc-dark-surface")).toContain(
        "radial-gradient",
      );
    }
    if (resolved === "light") {
      expect(getComputedStyle(canvas.getByRole("main")).backgroundImage).toBe(
        "none",
      );
      expect(root.getPropertyValue("--color-cc-bg").trim()).toBe("#f5f1ea");
      for (const element of canvasElement.querySelectorAll(
        "[data-contrast], a, label, [role=tab], .text-cc-danger, .DocSearch-Hit-title, .DocSearch-Hit-path, .DocSearch-Commands li, .DocSearch-Help, .DocSearch-Dropdown p, .font-bold, button:not(:disabled)",
      )) {
        expect(
          contrast(element),
          `${element.textContent} contrast`,
        ).toBeGreaterThanOrEqual(4.5);
      }
      const card = canvas.getByRole("heading", {
        name: "Theme-safe controls",
      }).parentElement!;
      expect(contrast(card, "borderTopColor")).toBeGreaterThanOrEqual(1.5);
      const outline = canvas.getByRole("button", { name: "Learn more" });
      expect(contrast(outline, "borderTopColor")).toBeGreaterThanOrEqual(3);
      const search = canvas.getByRole("textbox", {
        name: "Search documentation",
      });
      search.focus();
      expect(search).toHaveFocus();
      expect(
        contrast(search.closest("form")!, "borderBottomColor"),
      ).toBeGreaterThanOrEqual(3);
      const email = canvas.getByRole("textbox", { name: "Email" });
      const placeholderLuminance = luminance(
        composite(
          rgba(getComputedStyle(email, "::placeholder").color),
          rgba(getComputedStyle(email).backgroundColor),
        ),
      );
      const fieldLuminance = luminance(
        rgba(getComputedStyle(email).backgroundColor),
      );
      expect(
        (Math.max(placeholderLuminance, fieldLuminance) + 0.05) /
          (Math.min(placeholderLuminance, fieldLuminance) + 0.05),
      ).toBeGreaterThanOrEqual(4.5);
      expect(contrast(email, "borderTopColor")).toBeGreaterThanOrEqual(3);
      email.focus();
      expect(email).toHaveFocus();
      expect(contrast(email, "borderTopColor")).toBeGreaterThanOrEqual(3);
    }
  },
} satisfies Meta<typeof ThemePalette>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Light: Story = {};
export const Dark: Story = { args: { scheme: "dark" } };
export const LightMobile: Story = { args: { mobile: true } };
export const DarkMobile: Story = { args: { scheme: "dark", mobile: true } };
export const Auto: Story = { args: { scheme: "auto" } };
