// For more info, see https://github.com/storybookjs/eslint-plugin-storybook#configuration-flat-config-format
import storybook from "eslint-plugin-storybook";

import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "storybook-static/**",
    "next-env.d.ts",
  ]),
  ...storybook.configs["flat/recommended"],
  {
    rules: {
      // Guards a Pages Router constraint: `beforeInteractive` belongs in
      // `pages/_document.js`. This site is App Router only, and the rule
      // already exempts files under `app/`, so it only ever fires on
      // components that live in `src/` by convention.
      "@next/next/no-before-interactive-script-outside-document": "off",
      // A leading underscore marks a binding that exists only to be skipped: a
      // marker component's unread props, or a key destructured to omit it.
      "@typescript-eslint/no-unused-vars": [
        "warn",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
    },
  },
]);

export default eslintConfig;
