import type { ReactElement } from "react";
import type { BundledTheme } from "shiki";
import { CodeBlock } from "./CodeBlock";

// CodeBlock is an async server component (uses shiki's `codeToHtml`).
// Storybook's renderer doesn't render async components on the client, so
// each story pre-resolves its JSX in a `loaders` hook and renders the
// captured node synchronously.
export async function renderBlock(
  language: string,
  code: string,
  meta?: string,
  theme?: BundledTheme,
): Promise<ReactElement> {
  return (await CodeBlock({
    theme,
    children: (
      <code className={`language-${language}`} data-meta={meta}>
        {code}
      </code>
    ),
  })) as ReactElement;
}

// Sets the resolved site theme for one story. "auto" removes the attribute so
// the stylesheet resolves the theme from the system preference.
export function storyTheme(scheme: "light" | "dark" | "auto") {
  return () => {
    const previous = document.documentElement.getAttribute("data-theme");
    if (scheme === "auto") {
      document.documentElement.removeAttribute("data-theme");
    } else {
      document.documentElement.dataset.theme = scheme;
    }
    return () => {
      if (previous === null) {
        document.documentElement.removeAttribute("data-theme");
      } else {
        document.documentElement.setAttribute("data-theme", previous);
      }
    };
  };
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
  const [r, g, b] = color.map((value) => {
    const channel = value / 255;
    return channel <= 0.04045
      ? channel / 12.92
      : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return r * 0.2126 + g * 0.7152 + b * 0.0722;
}

// WCAG contrast of an element's text color against the backgrounds it sits on.
export function textContrast(element: Element): number {
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
    rgba(getComputedStyle(element).color),
    background,
  );
  const [low, high] = [luminance(foreground), luminance(background)].sort(
    (a, b) => a - b,
  );
  return (high + 0.05) / (low + 0.05);
}

// The lowest text contrast among the elements matching `selector`.
export function lowestContrast(root: ParentNode, selector: string): number {
  const elements = [...root.querySelectorAll(selector)].filter(
    (element) => element.textContent?.trim() && element.children.length === 0,
  );
  return Math.min(...elements.map(textContrast));
}
