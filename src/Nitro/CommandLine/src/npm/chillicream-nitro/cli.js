#!/usr/bin/env node
"use strict";

import { createRequire } from "module";
import { spawn } from "child_process";
import { dirname, join } from "path";

const require = createRequire(import.meta.url);

const candidates = {
  "darwin-arm64": ["@chillicream/nitro-osx-arm64"],
  "darwin-x64": ["@chillicream/nitro-osx-x64"],
  "linux-arm64": ["@chillicream/nitro-linux-arm64"],
  "linux-x64": [
    "@chillicream/nitro-linux-x64",
    "@chillicream/nitro-linux-musl-x64",
  ],
  "win32-ia32": ["@chillicream/nitro-win-x86"],
  "win32-x64": ["@chillicream/nitro-win-x64"],
};

function resolveBinary() {
  const key = `${process.platform}-${process.arch}`;
  const binaryName = process.platform === "win32" ? "nitro.exe" : "nitro";

  for (const pkg of candidates[key] ?? []) {
    try {
      const pkgJson = require.resolve(`${pkg}/package.json`);
      return join(dirname(pkgJson), binaryName);
    } catch {}
  }
  return null;
}

const bin = resolveBinary();

if (bin === null) {
  throw new Error(
    `Platform "${process.platform} (${process.arch})" is not supported by @chillicream/nitro.`
  );
}

const child = spawn(bin, process.argv.slice(2), { stdio: "inherit" });
child.on("exit", (code) => process.exit(code ?? 0));
