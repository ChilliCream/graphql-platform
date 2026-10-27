import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";
import { runInNewContext } from "node:vm";
import type {
  ThemePreference,
  ThemeStore,
} from "../../src/theme/theme-preference";

const {
  createThemeStore,
  themeScript,
}: typeof import("../../src/theme/theme-preference") = await import(
  new URL("../../src/theme/theme-preference.ts", import.meta.url).href
);

function environment(saved: string | null, dark: boolean) {
  const values = new Map(saved === null ? [] : [["cc-theme", saved]]);
  const changes: (() => void)[] = [];
  const media = {
    matches: dark,
    addEventListener(event: string, listener: () => void) {
      assert.equal(event, "change");
      changes.push(listener);
    },
  };
  const meta = {
    content: "#f7f8fc",
    setAttribute(name: string, value: string) {
      assert.equal(name, "content");
      this.content = value;
    },
  };
  const document = {
    documentElement: {
      dataset: {} as { theme?: string },
      style: { colorScheme: "" },
    },
    querySelector(selector: string) {
      assert.equal(selector, 'meta[name="theme-color"]');
      return meta;
    },
  };
  const browser = {
    __ccTheme: undefined as ThemeStore | undefined,
    localStorage: {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => {
        values.set(key, value);
      },
    },
    matchMedia(query: string) {
      assert.equal(query, "(prefers-color-scheme: dark)");
      return media;
    },
  };
  return {
    browser,
    document,
    values,
    setSystemDark(value: boolean) {
      media.matches = value;
      changes.forEach((listener) => listener());
    },
    appearance() {
      return {
        theme: document.documentElement.dataset.theme,
        colorScheme: document.documentElement.style.colorScheme,
        themeColor: meta.content,
      };
    },
  };
}

function start(env: ReturnType<typeof environment>, bootstrap: boolean) {
  if (bootstrap) {
    runInNewContext(themeScript, {
      window: env.browser,
      document: env.document,
    });
    return env.browser.__ccTheme!;
  }
  return createThemeStore(
    env.browser as unknown as Window,
    env.document as unknown as Document,
  );
}

test("root layout leaves the server-rendered theme unresolved", async () => {
  // arrange
  const layout = await readFile(
    new URL("../../app/layout.tsx", import.meta.url),
    "utf8",
  );

  // act
  const root = layout.match(/<html\b[^>]*>/)?.[0] ?? "";

  // assert
  assert.match(root, /^<html\b/);
  assert.doesNotMatch(root, /data-theme|\bstyle=|colorScheme/);
});

test("head bootstrap resolves an attribute-absent root", () => {
  // arrange
  const env = environment("dark", false);

  // act
  const store = start(env, true);

  // assert
  assert.equal(store.getSnapshot().resolvedTheme, "dark");
  assert.deepEqual(env.appearance(), {
    theme: "dark",
    colorScheme: "dark",
    themeColor: "#0b0f1a",
  });
});

for (const bootstrap of [false, true]) {
  const implementation = bootstrap ? "head bootstrap" : "client runtime";

  for (const saved of [null, "", "invalid", "Dark", "auto", "light", "dark"]) {
    for (const dark of [false, true]) {
      test(`${implementation} resolves saved ${JSON.stringify(saved)} with system ${dark ? "dark" : "light"}`, () => {
        // arrange
        const env = environment(saved, dark);
        const preference =
          saved === "light" || saved === "dark" ? saved : "auto";
        const resolvedTheme =
          preference === "auto" ? (dark ? "dark" : "light") : preference;

        // act
        const store = start(env, bootstrap);

        // assert
        assert.equal(store.getSnapshot().preference, preference);
        assert.equal(store.getSnapshot().resolvedTheme, resolvedTheme);
        assert.deepEqual(env.appearance(), {
          theme: resolvedTheme,
          colorScheme: resolvedTheme,
          themeColor: resolvedTheme === "dark" ? "#0b0f1a" : "#f7f8fc",
        });
        assert.equal(env.values.get("cc-theme"), saved ?? undefined);
      });
    }
  }

  test(`${implementation} follows the live system only in Auto and persists Auto itself`, () => {
    // arrange
    const env = environment("dark", false);
    const store = start(env, bootstrap);
    const updates: string[] = [];
    const unsubscribe = store.subscribe(() => {
      const state = store.getSnapshot();
      updates.push(`${state.preference}:${state.resolvedTheme}`);
    });

    // act
    env.setSystemDark(true);
    store.setPreference("light");
    env.setSystemDark(false);
    env.setSystemDark(true);
    store.setPreference("auto");
    env.setSystemDark(false);
    const snapshot = store.getSnapshot();
    env.setSystemDark(false);
    unsubscribe();
    env.setSystemDark(true);

    // assert
    assert.deepEqual(updates, ["light:light", "auto:dark", "auto:light"]);
    assert.equal(env.values.get("cc-theme"), "auto");
    assert.equal(snapshot.resolvedTheme, "light");
    assert.equal(store.getSnapshot().resolvedTheme, "dark");
    assert.deepEqual(env.appearance(), {
      theme: "dark",
      colorScheme: "dark",
      themeColor: "#0b0f1a",
    });
  });

  test(`${implementation} keeps a stable snapshot when the appearance and preference do not change`, () => {
    // arrange
    const env = environment("auto", false);
    const store = start(env, bootstrap);
    const snapshot = store.getSnapshot();

    // act
    env.setSystemDark(false);
    store.setPreference("auto");

    // assert
    assert.equal(store.getSnapshot(), snapshot);
  });

  for (const failure of ["getter", "read", "write"]) {
    test(`${implementation} tolerates a storage ${failure} SecurityError without losing in-memory choices`, () => {
      // arrange
      const env = environment("light", false);
      const unavailable = () => {
        throw new DOMException("Storage unavailable", "SecurityError");
      };
      if (failure === "getter") {
        Object.defineProperty(env.browser, "localStorage", {
          get: unavailable,
        });
      } else if (failure === "read") {
        env.browser.localStorage.getItem = unavailable;
      } else {
        env.browser.localStorage.setItem = unavailable;
      }

      // act
      const store = start(env, bootstrap);
      const initialPreference = store.getSnapshot().preference;
      store.setPreference("dark");
      env.setSystemDark(true);
      env.setSystemDark(false);
      const explicitTheme = store.getSnapshot().resolvedTheme;
      store.setPreference("auto");
      env.setSystemDark(true);

      // assert
      assert.equal(initialPreference, failure === "write" ? "light" : "auto");
      assert.equal(explicitTheme, "dark");
      assert.equal(store.getSnapshot().preference, "auto");
      assert.equal(store.getSnapshot().resolvedTheme, "dark");
      assert.deepEqual(env.appearance(), {
        theme: "dark",
        colorScheme: "dark",
        themeColor: "#0b0f1a",
      });
    });
  }

  test(`${implementation} normalizes invalid setter input instead of writing an invalid theme`, () => {
    // arrange
    const env = environment("light", true);
    const store = start(env, bootstrap);

    // act
    store.setPreference("invalid" as ThemePreference);

    // assert
    assert.equal(store.getSnapshot().preference, "auto");
    assert.equal(store.getSnapshot().resolvedTheme, "dark");
    assert.equal(env.values.get("cc-theme"), "auto");
  });
}
