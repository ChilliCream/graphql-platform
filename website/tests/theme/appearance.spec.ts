import {
  expect,
  test as base,
  type Browser,
  type Locator,
  type Page,
} from "@playwright/test";
import sharp from "sharp";

type Scheme = "light" | "dark";

interface OpenOptions {
  readonly system?: Scheme;
  readonly stored?: string;
  readonly storage?: "access-throws" | "methods-throw";
  readonly javaScript?: boolean;
  readonly width?: number;
  readonly height?: number;
}

interface PaintSnapshot {
  theme?: string;
  colorScheme?: string;
  themeColor?: string;
}

declare global {
  interface Window {
    __paint?: PaintSnapshot;
    __soft?: number;
  }
}

const LIGHT_BG = "rgb(245, 241, 234)";
const DARK_BG = "rgb(11, 15, 26)";
const THEME_COLOR = { light: "#f5f1ea", dark: "#0b0f1a" } as const;
const BACKGROUND = { light: LIGHT_BG, dark: DARK_BG } as const;
const NAMES = { light: "Light", dark: "Dark", auto: "Auto" } as const;

// A light 1x1 PNG stands in for remote images, so poster slots do not render
// as the black of a failed request.
const PLACEHOLDER_PIXEL = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
  "base64",
);

function opposite(scheme: Scheme): Scheme {
  return scheme === "light" ? "dark" : "light";
}

async function openPage(
  browser: Browser,
  problems: string[],
  options: OpenOptions,
) {
  const context = await browser.newContext({
    colorScheme: options.system ?? "light",
    javaScriptEnabled: options.javaScript ?? true,
    viewport: { width: options.width ?? 1440, height: options.height ?? 900 },
  });
  const page = await context.newPage();

  await page.route(
    (url) => url.hostname !== "127.0.0.1",
    (route) =>
      route.request().resourceType() === "image"
        ? route.fulfill({
            status: 200,
            contentType: "image/png",
            body: PLACEHOLDER_PIXEL,
          })
        : route.abort(),
  );
  page.on("pageerror", (error) => problems.push(`pageerror: ${error.message}`));
  page.on("console", (message) => {
    const text = message.text();
    const failedRequest = text.startsWith("Failed to load resource");
    if (
      (message.type() === "error" && !failedRequest) ||
      (message.type() === "warning" && /hydrat/i.test(text))
    ) {
      problems.push(`${message.type()}: ${text}`);
    }
  });

  if (options.javaScript !== false) {
    await page.addInitScript(
      ({ stored, storage }) => {
        if (storage === "access-throws") {
          Object.defineProperty(window, "localStorage", {
            get() {
              throw new DOMException("denied", "SecurityError");
            },
          });
        } else if (storage === "methods-throw") {
          const fail = () => {
            throw new DOMException("denied", "QuotaExceededError");
          };
          Storage.prototype.getItem = fail;
          Storage.prototype.setItem = fail;
        } else if (
          stored !== undefined &&
          sessionStorage.getItem("seeded") === null
        ) {
          sessionStorage.setItem("seeded", "1");
          localStorage.setItem("cc-theme", stored);
        }

        const paint: PaintSnapshot = {};
        window.__paint = paint;
        new MutationObserver((_records, observer) => {
          if (document.body) {
            const root = document.documentElement;
            paint.theme = root.dataset.theme;
            paint.colorScheme = root.style.colorScheme;
            paint.themeColor = Array.from(
              document.querySelectorAll<HTMLMetaElement>(
                'meta[name="theme-color"]',
              ),
            ).find(
              (meta) => !meta.media || window.matchMedia(meta.media).matches,
            )?.content;
            observer.disconnect();
          }
        }).observe(document, { childList: true, subtree: true });
      },
      { stored: options.stored, storage: options.storage },
    );
  }

  return { context, page };
}

interface Fixtures {
  readonly problems: string[];
  readonly open: (path: string, options?: OpenOptions) => Promise<Page>;
}

const test = base.extend<Fixtures>({
  problems: [
    async ({}, provide) => {
      const problems: string[] = [];
      await provide(problems);
      expect(
        problems,
        "console errors, page errors or hydration warnings",
      ).toEqual([]);
    },
    { auto: true },
  ],
  open: async ({ browser, problems }, provide) => {
    const contexts: { close(): Promise<void> }[] = [];
    await provide(async (path, options = {}) => {
      const { context, page } = await openPage(browser, problems, options);
      contexts.push(context);
      await page.goto(path);
      await page.waitForLoadState("networkidle");
      return page;
    });
    await Promise.all(contexts.map((context) => context.close()));
  },
});

function root(page: Page) {
  return page.evaluate(() => {
    const element = document.documentElement;
    return {
      theme: element.dataset.theme ?? null,
      inlineScheme: element.style.colorScheme,
      scheme: getComputedStyle(element).colorScheme,
      background: getComputedStyle(document.body).backgroundColor,
      themeColor:
        Array.from(
          document.querySelectorAll<HTMLMetaElement>(
            'meta[name="theme-color"]',
          ),
        ).find((meta) => !meta.media || matchMedia(meta.media).matches)
          ?.content ?? null,
      stored: (() => {
        try {
          return localStorage.getItem("cc-theme");
        } catch {
          return "unavailable";
        }
      })(),
    };
  });
}

async function expectResolved(page: Page, scheme: Scheme) {
  await expect.poll(() => root(page).then(({ theme }) => theme)).toBe(scheme);
  expect(await root(page)).toMatchObject({
    theme: scheme,
    inlineScheme: scheme,
    scheme,
    background: BACKGROUND[scheme],
    themeColor: THEME_COLOR[scheme],
  });
}

function radio(page: Page, name: keyof typeof NAMES | string, scope?: Locator) {
  const label = NAMES[name as keyof typeof NAMES] ?? name;
  return (scope ?? page).getByRole("radio", { name: label, exact: true });
}

function headerGroup(page: Page) {
  return page.locator("header").getByRole("radiogroup", { name: "Appearance" });
}

async function expectSelected(
  page: Page,
  preference: "auto" | "light" | "dark",
  scope?: Locator,
) {
  for (const value of ["light", "dark", "auto"] as const) {
    const expectation = expect(radio(page, value, scope));
    if (value === preference) {
      await expectation.toBeChecked();
    } else {
      await expectation.not.toBeChecked();
    }
  }
}

function openMobileMenu(page: Page) {
  return page.getByRole("button", { name: "Open navigation menu" }).click();
}

test.describe("server markup", () => {
  test("Should_OmitDataTheme_When_RenderingServerHtml", async ({ request }) => {
    // act
    const html = await (await request.get("/")).text();

    // assert
    expect(html.match(/<html[^>]*>/)?.[0]).not.toContain("data-theme");
    expect(html).toContain(
      '<meta name="theme-color" content="#f5f1ea" media="(prefers-color-scheme: light)"/>',
    );
    expect(html).toContain(
      '<meta name="theme-color" content="#0b0f1a" media="(prefers-color-scheme: dark)"/>',
    );
    expect(html.indexOf("cc-theme")).toBeLessThan(html.indexOf("<body"));
  });
});

test.describe("without JavaScript", () => {
  for (const system of ["light", "dark"] as const) {
    test(`Should_FollowSystem_When_JavaScriptIsDisabled_And_System${system}`, async ({
      open,
    }) => {
      // arrange
      const page = await open("/", { system, javaScript: false });

      // act
      const state = await root(page);

      // assert
      expect(state).toMatchObject({
        theme: null,
        inlineScheme: "",
        scheme: system,
        background: BACKGROUND[system],
        themeColor: THEME_COLOR[system],
      });
      await expectSelected(page, "auto");
    });
  }
});

test.describe("first paint", () => {
  for (const system of ["light", "dark"] as const) {
    test(`Should_ResolveSystem_When_NothingIsStored_And_System${system}`, async ({
      open,
    }) => {
      // arrange
      const page = await open("/", { system });

      // act
      const painted = await page.evaluate(() => window.__paint);

      // assert
      expect(painted).toEqual({
        theme: system,
        colorScheme: system,
        themeColor: THEME_COLOR[system],
      });
      await expectResolved(page, system);
      expect((await root(page)).stored).toBeNull();
      await expectSelected(page, "auto");
    });
  }

  for (const stored of ["light", "dark"] as const) {
    test(`Should_ApplyStoredBeforeBody_When_${stored}OpposesSystem`, async ({
      open,
    }) => {
      // arrange
      const page = await open("/", {
        system: opposite(stored),
        stored,
      });

      // act
      const painted = await page.evaluate(() => window.__paint);

      // assert
      expect(painted).toEqual({
        theme: stored,
        colorScheme: stored,
        themeColor: THEME_COLOR[stored],
      });
      await expectResolved(page, stored);
      await expectSelected(page, stored);
    });
  }

  test("Should_ApplyStoredTheme_When_FrameworkScriptsNeverRun", async ({
    browser,
    problems,
  }) => {
    // arrange
    const { context, page } = await openPage(browser, problems, {
      system: "dark",
      stored: "light",
    });
    await page.route("**/_next/static/**/*.js", (route) => route.abort());

    // act
    await page.goto("/");

    // assert
    await expectResolved(page, "light");
    await context.close();
  });
});

test.describe("system changes", () => {
  test("Should_TrackSystem_When_PreferenceIsAuto", async ({ open }) => {
    // arrange
    const page = await open("/", { system: "light" });

    // act
    await page.emulateMedia({ colorScheme: "dark" });

    // assert
    await expectResolved(page, "dark");
    await page.emulateMedia({ colorScheme: "light" });
    await expectResolved(page, "light");
    await expectSelected(page, "auto");
  });

  for (const stored of ["light", "dark"] as const) {
    test(`Should_IgnoreSystem_When_Preference${stored}`, async ({ open }) => {
      // arrange
      const page = await open("/", { system: opposite(stored), stored });

      // act
      await page.emulateMedia({ colorScheme: stored });
      await page.emulateMedia({ colorScheme: opposite(stored) });

      // assert
      await expectResolved(page, stored);
      await expectSelected(page, stored);
    });
  }

  test("Should_PersistAutoAndResumeTracking_When_AutoIsChosen", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "dark", stored: "light" });
    await expectResolved(page, "light");

    // act
    await headerGroup(page)
      .getByRole("radio", { name: "Auto", exact: true })
      .click();

    // assert
    await expectResolved(page, "dark");
    expect((await root(page)).stored).toBe("auto");
    await page.emulateMedia({ colorScheme: "light" });
    await expectResolved(page, "light");
    await page.reload();
    await expectResolved(page, "light");
    expect((await root(page)).stored).toBe("auto");
    await expectSelected(page, "auto");
  });
});

test.describe("persistence", () => {
  test("Should_KeepPreferenceAndScheme_When_ReloadingAndNavigatingClientSide", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "light" });
    await headerGroup(page)
      .getByRole("radio", { name: "Dark", exact: true })
      .click();
    await expectResolved(page, "dark");

    // act
    await page.reload();
    await expectResolved(page, "dark");
    await page.evaluate(() => {
      window.__soft = 1;
    });
    await page
      .locator("footer")
      .getByRole("link", { name: "Mocha" })
      .first()
      .click();
    await page.waitForURL("**/products/mocha");

    // assert
    expect(await page.evaluate(() => window.__soft)).toBe(1);
    await expectResolved(page, "dark");
    await expectSelected(page, "dark");
    await page.goBack();
    await page.waitForURL((url) => url.pathname === "/");
    await expectResolved(page, "dark");
    await expectSelected(page, "dark");
  });

  test("Should_KeepTrackingSystem_When_AutoSurvivesClientNavigation", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "light" });
    await page.evaluate(() => {
      window.__soft = 1;
    });

    // act
    await page
      .locator("footer")
      .getByRole("link", { name: "Mocha" })
      .first()
      .click();
    await page.waitForURL("**/products/mocha");
    await page.emulateMedia({ colorScheme: "dark" });

    // assert
    expect(await page.evaluate(() => window.__soft)).toBe(1);
    await expectResolved(page, "dark");
    await expectSelected(page, "auto");
  });

  for (const [value, system] of [
    ["banana", "light"],
    ["banana", "dark"],
    ["", "dark"],
    ["LIGHT", "dark"],
    ['{"theme":"dark"}', "light"],
  ] as const) {
    test(`Should_DefaultToAuto_When_StoredValueIs${JSON.stringify(value)}_And_System${system}`, async ({
      open,
    }) => {
      // arrange
      const page = await open("/", { system, stored: value });

      // act
      const painted = await page.evaluate(() => window.__paint?.theme);

      // assert
      expect(painted).toBe(system);
      await expectResolved(page, system);
      await expectSelected(page, "auto");
    });
  }

  for (const storage of ["access-throws", "methods-throw"] as const) {
    test(`Should_RenderAndSwitch_When_Storage_${storage}`, async ({ open }) => {
      // arrange
      const page = await open("/", { system: "light", storage });
      await expectResolved(page, "light");

      // act
      await headerGroup(page)
        .getByRole("radio", { name: "Dark", exact: true })
        .click();

      // assert
      await expectResolved(page, "dark");
      await expectSelected(page, "dark");
      await page.emulateMedia({ colorScheme: "dark" });
      await headerGroup(page)
        .getByRole("radio", { name: "Auto", exact: true })
        .click();
      await page.emulateMedia({ colorScheme: "light" });
      await expectResolved(page, "light");
      await expectSelected(page, "auto");
    });
  }
});

test.describe("appearance selector", () => {
  test("Should_OrderSunMoonAndSunMoon_When_Rendered", async ({ open }) => {
    // arrange
    const page = await open("/");

    // act
    const glyphs = await headerGroup(page)
      .locator("label")
      .evaluateAll((labels) =>
        labels.map((label) => ({
          name: label.textContent,
          circles: label.querySelectorAll("circle").length,
          paths: label.querySelectorAll("path").length,
        })),
      );

    // assert
    expect(glyphs).toEqual([
      { name: "Light", circles: 1, paths: 1 },
      { name: "Dark", circles: 0, paths: 1 },
      { name: "Auto", circles: 1, paths: 2 },
    ]);
  });

  test("Should_SelectByKeyboardOnDesktop_When_UsingArrowKeys", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "dark" });
    await page.getByRole("link", { name: "Launch Nitro" }).first().focus();

    // act
    await page.keyboard.press("Tab");

    // assert
    await expect(radio(page, "auto", headerGroup(page))).toBeFocused();
    for (const [key, name, expected] of [
      ["ArrowRight", "light", "light"],
      ["ArrowRight", "dark", "dark"],
      ["ArrowLeft", "light", "light"],
      ["ArrowRight", "dark", "dark"],
      ["ArrowRight", "auto", "dark"],
    ] as const) {
      await page.keyboard.press(key);
      await expect(radio(page, name, headerGroup(page))).toBeChecked();
      await expect(radio(page, name, headerGroup(page))).toBeFocused();
      await expectResolved(page, expected);
      expect((await root(page)).stored).toBe(name);
    }
  });

  test("Should_SelectByPointerOnDesktop_When_ClickingEachOption", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "dark" });
    const group = headerGroup(page);

    // act
    await group.getByRole("radio", { name: "Light", exact: true }).click();

    // assert
    await expectResolved(page, "light");
    await expect(group.locator("label").first()).toHaveAttribute(
      "title",
      "Light",
    );
    await group.getByRole("radio", { name: "Dark", exact: true }).click();
    await expectResolved(page, "dark");
    await group.getByRole("radio", { name: "Auto", exact: true }).click();
    await expectResolved(page, "dark");
    await expectSelected(page, "auto");
  });

  test("Should_SelectByKeyboardAndPointerOnMobile_When_UsingTheDrawer", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "light", width: 375, height: 800 });
    await expect(headerGroup(page)).toBeHidden();
    await openMobileMenu(page);
    const group = page.getByRole("radiogroup", { name: "Appearance" });
    await expect(group).toBeVisible();

    // act
    const sizes = await group
      .locator("label")
      .evaluateAll((labels) =>
        labels.map((label) => label.getBoundingClientRect().width),
      );
    await radio(page, "auto", group).focus();
    await page.keyboard.press("ArrowRight");
    await page.keyboard.press("ArrowRight");

    // assert
    expect(sizes).toEqual([44, 44, 44]);
    await expect(radio(page, "dark", group)).toBeChecked();
    await expectResolved(page, "dark");
    await group.getByRole("radio", { name: "Light", exact: true }).click();
    await expectResolved(page, "light");
    await expectSelected(page, "light", group);
    expect((await root(page)).stored).toBe("light");
  });

  for (const [width, box, bordered] of [
    [1440, 32, true],
    [1280, 32, true],
    [1279, 28, false],
    [1060, 28, false],
  ] as const) {
    test(`Should_FitHeaderWithoutOverflow_When_Width${width}`, async ({
      open,
    }) => {
      // arrange
      const page = await open("/", { width, height: 800 });
      const group = headerGroup(page);

      // act
      const layout = await page.evaluate(() => {
        const actions = document.querySelector(
          "header [role=radiogroup]",
        )!.parentElement!;
        const items = [...actions.children].map((element) => {
          const { left, right } = element.getBoundingClientRect();
          return { left, right };
        });
        return {
          items,
          overflow: document.documentElement.scrollWidth - window.innerWidth,
          viewport: window.innerWidth,
        };
      });
      const boxes = await group
        .locator("label")
        .evaluateAll((labels) =>
          labels.map((label) => label.getBoundingClientRect().width),
        );
      const borderWidth = await group.evaluate((element) =>
        parseFloat(getComputedStyle(element).borderTopWidth),
      );

      // assert
      await expect(group).toBeVisible();
      expect(layout.overflow).toBeLessThanOrEqual(0);
      expect(layout.items.length).toBeGreaterThanOrEqual(4);
      for (const [index, item] of layout.items.entries()) {
        expect(item.right).toBeLessThanOrEqual(layout.viewport);
        if (index > 0) {
          expect(item.left).toBeGreaterThanOrEqual(
            layout.items[index - 1].right,
          );
        }
      }
      expect(boxes).toEqual([box, box, box]);
      expect(borderWidth > 0).toBe(bordered);
    });
  }

  test("Should_MoveSelectorIntoDrawer_When_BelowDesktopHeader", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { width: 1059, height: 800 });

    // act
    const menu = page.getByRole("button", { name: "Open navigation menu" });

    // assert
    await expect(headerGroup(page)).toBeHidden();
    await expect(menu).toBeVisible();
  });
});

const ROUTES = [
  "/",
  "/products/fusion",
  "/products/nitro",
  "/platform/graphql-federation",
  "/pricing",
  "/docs/fusion/adding-a-subgraph",
  "/blog/2025-02-19-dataloader-explained",
  "/videos",
] as const;

function scanForStages(page: Page) {
  return page.evaluate(() => {
    const context = document.createElement("canvas").getContext("2d")!;
    const parse = (color: string) => {
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = "#fff";
      context.fillStyle = color;
      context.fillRect(0, 0, 1, 1);
      return context.getImageData(0, 0, 1, 1).data;
    };
    const luminance = (rgba: Uint8ClampedArray) => {
      const [r, g, b] = [...rgba].slice(0, 3).map((value) => {
        const channel = value / 255;
        return channel <= 0.04045
          ? channel / 12.92
          : ((channel + 0.055) / 1.055) ** 2.4;
      });
      return r * 0.2126 + g * 0.7152 + b * 0.0722;
    };
    const describe = (element: Element) =>
      `${element.tagName.toLowerCase()}.${element.getAttribute("class")?.slice(0, 60) ?? ""}`;

    const stages: string[] = [];
    for (const element of document.querySelectorAll("body *")) {
      const box = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      if (
        box.width * box.height < 30000 ||
        style.visibility === "hidden" ||
        Number(style.opacity) === 0 ||
        element.closest(".aspect-video")
      ) {
        continue;
      }
      const colors: string[] = [];
      if (!(element instanceof SVGElement)) {
        colors.push(style.backgroundColor);
        if (!/text/.test(style.backgroundClip)) {
          colors.push(
            ...(style.backgroundImage.match(/rgba?\([^)]*\)/g) ?? []),
          );
        }
      } else if (
        /^(rect|path|circle|ellipse|polygon)$/.test(element.localName)
      ) {
        colors.push(style.fill);
      }
      const dark = colors.some((color) => {
        const rgba = parse(color);
        return rgba[3] >= 128 && luminance(rgba) < 0.08;
      });
      if (dark) {
        stages.push(describe(element));
      }
    }

    const lightTextOnLight: string[] = [];
    const walker = document.createTreeWalker(
      document.body,
      NodeFilter.SHOW_TEXT,
    );
    const seen = new Set<Element>();
    while (walker.nextNode()) {
      const node = walker.currentNode;
      const element = node.parentElement;
      if (
        !node.textContent?.trim() ||
        !element ||
        seen.has(element) ||
        element.closest("svg, script, style, noscript")
      ) {
        continue;
      }
      seen.add(element);
      const style = getComputedStyle(element);
      const box = element.getBoundingClientRect();
      const color = parse(style.color);
      if (
        box.width === 0 ||
        box.height === 0 ||
        style.visibility === "hidden" ||
        Number(style.opacity) === 0 ||
        color[3] < 200 ||
        luminance(color) < 0.6
      ) {
        continue;
      }
      let surface: number | null = null;
      let image = false;
      for (
        let ancestor: Element | null = element;
        ancestor;
        ancestor = ancestor.parentElement
      ) {
        const ancestorStyle = getComputedStyle(ancestor);
        image ||= ancestorStyle.backgroundImage !== "none";
        const background = parse(ancestorStyle.backgroundColor);
        if (background[3] > 76) {
          surface = luminance(background);
          break;
        }
      }
      if (!image && surface !== null && surface > 0.5) {
        lightTextOnLight.push(
          `${describe(element)} "${node.textContent.trim().slice(0, 30)}"`,
        );
      }
    }

    return { stages, lightTextOnLight };
  });
}

async function darkPixelFraction(page: Page) {
  const { data, info } = await sharp(await page.screenshot())
    .removeAlpha()
    .resize({ width: 240 })
    .raw()
    .toBuffer({ resolveWithObject: true });
  let dark = 0;
  for (let index = 0; index < data.length; index += 3) {
    const luma =
      (0.2126 * data[index] +
        0.7152 * data[index + 1] +
        0.0722 * data[index + 2]) /
      255;
    if (luma < 0.25) {
      dark++;
    }
  }
  return dark / (info.width * info.height);
}

async function revealPage(page: Page) {
  await page.evaluate(async () => {
    const height = document.documentElement.scrollHeight;
    for (let y = 0; y < height; y += 600) {
      window.scrollTo(0, y);
      await new Promise((resolve) => setTimeout(resolve, 60));
    }
    window.scrollTo(0, 0);
    await new Promise((resolve) => setTimeout(resolve, 300));
  });
}

test.describe("rendered pages", () => {
  for (const width of [375, 1440]) {
    for (const scheme of ["light", "dark"] as const) {
      for (const route of ROUTES) {
        test(`Should_RenderCompleteScheme_When_${scheme}At${width}On${route}`, async ({
          open,
        }) => {
          // arrange
          const page = await open(route, {
            system: opposite(scheme),
            stored: scheme,
            width,
          });
          await revealPage(page);

          // act
          const overflow = await page.evaluate(
            () => document.documentElement.scrollWidth - window.innerWidth,
          );
          const scan = await scanForStages(page);
          const dark = await darkPixelFraction(page);

          // assert
          await expectResolved(page, scheme);
          expect(overflow).toBeLessThanOrEqual(0);
          if (scheme === "light") {
            expect(scan.stages, "dark stages in Light").toEqual([]);
            expect(scan.lightTextOnLight, "Dark-only text in Light").toEqual(
              [],
            );
            // The video player frames are media and stay black.
            if (route !== "/videos") {
              expect(dark).toBeLessThan(0.08);
            }
          } else {
            expect(scan.stages.length).toBeGreaterThan(0);
            expect(dark).toBeGreaterThan(0.5);
          }
        });
      }
    }
  }
});

test.describe("Dark leftover scan", () => {
  test("Should_ReportLeftovers_When_ADarkStageAndWhiteTextAreInjectedInLight", async ({
    open,
  }) => {
    // arrange
    const page = await open("/", { system: "dark", stored: "light" });
    await page.evaluate(() => {
      document.body.insertAdjacentHTML(
        "afterbegin",
        '<div id="stage" style="width:400px;height:200px;background:#0b0f1a"></div>' +
          '<p id="label" style="color:#fff">Leftover</p>',
      );
    });

    // act
    const scan = await scanForStages(page);

    // assert
    expect(scan.stages).toEqual(["div."]);
    expect(scan.lightTextOnLight).toEqual(['p. "Leftover"']);
  });
});

test.describe("start page detail clarity", () => {
  async function hairlines(page: Page) {
    return page.evaluate(() => {
      const context = document.createElement("canvas").getContext("2d")!;
      const parse = (color: string) => {
        context.clearRect(0, 0, 1, 1);
        context.fillStyle = "#fff";
        context.fillStyle = color;
        context.fillRect(0, 0, 1, 1);
        return [...context.getImageData(0, 0, 1, 1).data];
      };
      const channel = (value: number) => {
        const unit = value / 255;
        return unit <= 0.04045 ? unit / 12.92 : ((unit + 0.055) / 1.055) ** 2.4;
      };
      const luminance = (rgb: number[]) =>
        channel(rgb[0]) * 0.2126 +
        channel(rgb[1]) * 0.7152 +
        channel(rgb[2]) * 0.0722;
      const surface = parse(getComputedStyle(document.body).backgroundColor);
      const ratio = (color: string) => {
        const [r, g, b, a] = parse(color);
        const blended = [r, g, b].map(
          (value, index) => value * (a / 255) + surface[index] * (1 - a / 255),
        );
        const [low, high] = [luminance(blended), luminance(surface)].sort(
          (x, y) => x - y,
        );
        return (high + 0.05) / (low + 0.05);
      };
      const styles = getComputedStyle(document.documentElement);
      const token = (name: string) =>
        ratio(styles.getPropertyValue(name).trim());
      const card = document.querySelector(
        "main [class*='border-cc-card-border']",
      );
      return {
        cardBorder: token("--color-cc-card-border"),
        controlBorder: token("--color-cc-control-border"),
        cardHover: token("--color-cc-card-border-hover"),
        header: ratio(
          getComputedStyle(document.querySelector("header")!).borderBottomColor,
        ),
        sampled: card ? ratio(getComputedStyle(card).borderTopColor) : 0,
      };
    });
  }

  test("Should_KeepHairlinesAsClearInLightAsInDark_When_ComparingTheStartPage", async ({
    open,
  }) => {
    // arrange
    const light = await open("/", { system: "dark", stored: "light" });
    const dark = await open("/", { system: "light", stored: "dark" });

    // act
    const lightRatios = await hairlines(light);
    const darkRatios = await hairlines(dark);

    // assert
    for (const key of Object.keys(
      lightRatios,
    ) as (keyof typeof lightRatios)[]) {
      expect(lightRatios[key], key).toBeGreaterThanOrEqual(darkRatios[key]);
    }
    expect(lightRatios.sampled).toBeGreaterThan(1.3);
  });
});

test.describe("Light and Dark toggle rules in the production build", () => {
  test("Should_ShipToggleRules_When_LoadingTheFusionPage", async ({ open }) => {
    // arrange
    const page = await open("/products/fusion");

    // act
    const rules = await page.evaluate(() => {
      const found = { light: "", dark: "", auto: "" };
      const visit = (list: CSSRuleList, inLight: boolean) => {
        for (const rule of list) {
          if (rule instanceof CSSMediaRule) {
            visit(
              rule.cssRules,
              /prefers-color-scheme:\s*light/.test(rule.conditionText),
            );
          } else if (rule instanceof CSSStyleRule) {
            const value = rule.style.getPropertyValue("--cc-if-light").trim();
            if (rule.selectorText === ':root[data-theme="light"]' && value) {
              found.light = value;
            }
            if (
              inLight &&
              rule.selectorText === ":root:not([data-theme])" &&
              value
            ) {
              found.auto = value;
            }
            if (
              rule.selectorText === ":root" &&
              rule.style.getPropertyValue("--cc-if-dark")
            ) {
              found.dark = rule.style.getPropertyValue("--cc-if-dark").trim();
            }
          }
        }
      };
      for (const sheet of document.styleSheets) {
        visit(sheet.cssRules, false);
      }
      return found;
    });

    // assert
    expect(rules).toEqual({
      light: "initial",
      dark: "initial",
      auto: "initial",
    });
  });

  for (const scheme of ["light", "dark"] as const) {
    test(`Should_SwitchToggledElement_When_${scheme}OnTheFusionPage`, async ({
      open,
    }) => {
      // arrange
      const page = await open("/products/fusion", {
        system: opposite(scheme),
        stored: scheme,
      });
      const element = page.locator('[style*="--cc-if-light"]').first();

      // act
      const background = await element.evaluate(
        (node) => getComputedStyle(node).backgroundImage,
      );
      const probe = await page.evaluate(() => {
        const node = document.createElement("div");
        node.style.color =
          "var(--cc-if-light, rgb(1, 2, 3)) var(--cc-if-dark, rgb(4, 5, 6))";
        document.body.append(node);
        const color = getComputedStyle(node).color;
        node.remove();
        return color;
      });

      // assert
      const base =
        scheme === "light" ? "rgba(245, 241, 234" : "rgba(11, 15, 26";
      expect(background).toContain(base);
      expect(probe).toBe(scheme === "light" ? "rgb(1, 2, 3)" : "rgb(4, 5, 6)");
    });
  }

  test("Should_SwitchToggledElementLive_When_ChoosingAppearanceOnTheFusionPage", async ({
    open,
  }) => {
    // arrange
    const page = await open("/products/fusion", { system: "light" });
    const element = page.locator('[style*="--cc-if-light"]').first();
    const read = () =>
      element.evaluate((node) => getComputedStyle(node).backgroundImage);

    // act
    const light = await read();
    await headerGroup(page)
      .getByRole("radio", { name: "Dark", exact: true })
      .click();
    const dark = await read();

    // assert
    expect(light).toContain("rgba(245, 241, 234");
    expect(dark).toContain("rgba(11, 15, 26");
  });
});
