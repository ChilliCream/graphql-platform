#!/usr/bin/env node
import { spawn } from "node:child_process";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { familySync, MUSL } from "detect-libc";

function resolveCandidates(): readonly string[] {
  const { platform, arch } = process;

  if (platform === "linux" && arch === "x64") {
    return familySync() === MUSL
      ? ["@chillicream/nitro-linux-musl-x64"]
      : ["@chillicream/nitro-linux-x64"];
  }

  const map: Record<string, readonly string[]> = {
    "darwin-arm64": ["@chillicream/nitro-osx-arm64"],
    "darwin-x64": ["@chillicream/nitro-osx-x64"],
    "linux-arm64": ["@chillicream/nitro-linux-arm64"],
    "win32-ia32": ["@chillicream/nitro-win-x86"],
    "win32-x64": ["@chillicream/nitro-win-x64"],
  };

  return map[`${platform}-${arch}`] ?? [];
}

function resolveBinary(): string | null {
  const binaryName = process.platform === "win32" ? "nitro.exe" : "nitro";

  for (const pkg of resolveCandidates()) {
    try {
      const pkgJson = fileURLToPath(import.meta.resolve(`${pkg}/package.json`));
      return join(dirname(pkgJson), binaryName);
    } catch {
      // try next candidate
    }
  }

  return null;
}

const bin = resolveBinary();

if (bin === null) {
  console.error(
    `Platform "${process.platform} (${process.arch})" is not supported by @chillicream/nitro.`,
  );
  process.exit(1);
}

const child = spawn(bin, process.argv.slice(2), { stdio: "inherit" });

child.on("error", (error) => {
  console.error(
    `Failed to start @chillicream/nitro binary "${bin}": ${error.message}`,
  );
  process.exit(1);
});

child.on("exit", (code, signal) => {
  if (signal !== null) {
    try {
      process.kill(process.pid, signal);
      return;
    } catch {
      process.exit(1);
    }
  }

  process.exit(code ?? 1);
});
