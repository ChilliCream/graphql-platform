"use strict";

import { join, dirname } from "path";
import { fileURLToPath } from "url";
import { spawn } from "child_process";
import { family, GLIBC, MUSL } from "detect-libc";

const __dirname = dirname(fileURLToPath(import.meta.url));

async function resolveBinary() {
  const { platform, arch } = process;

  const binaries = {
    darwin: {
      x64: "osx-x64/nitro",
      arm64: "osx-arm64/nitro",
    },
    win32: {
      x64: "win-x64/nitro.exe",
      ia32: "win-x86/nitro.exe",
      arm64: "win-arm64/nitro.exe",
    },
    linux: {
      x64: async () => {
        const libc = await family();

        if (libc === GLIBC) {
          return "linux-x64/nitro";
        } else if (libc === MUSL) {
          return "linux-musl-x64/nitro";
        }

        return null;
      },
      arm64: "linux-arm64/nitro",
    },
  };

  const binary = binaries[platform]?.[arch];
  if (!binary) {
    return null;
  }

  const binaryPath = typeof binary === "function" ? await binary() : binary;
  return binaryPath ? join(__dirname, binaryPath) : null;
}

const bin = await resolveBinary();
const input = process.argv.slice(2);

if (bin !== null) {
  spawn(bin, input, { stdio: "inherit" }).on("exit", process.exit);
} else {
  throw new Error(
    `Platform "${process.platform} (${process.arch})" not supported.`
  );
}
