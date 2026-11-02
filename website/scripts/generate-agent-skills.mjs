#!/usr/bin/env node
// Standalone Agent Skills discovery step, run as part of the release workflow.
// Downloads the chillicream/agent-skills repository tarball at main, publishes
// each skill under `public/.well-known/agent-skills/` (a bare SKILL.md for
// single-file skills, a deterministic .tar.gz for multi-file skills) and
// writes the discovery index per the Agent Skills Discovery RFC v0.2.0.
//
// Archives are byte-deterministic: entries are sorted, mode/uid/gid/mtime are
// fixed, and files sit at the archive root with SKILL.md at the top level, so
// digests only change when the skill content changes.
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";
import zlib from "node:zlib";
import matter from "gray-matter";

const TARBALL_URL =
  "https://codeload.github.com/chillicream/agent-skills/tar.gz/refs/heads/main";
const SCHEMA_URL = "https://schemas.agentskills.io/discovery/0.2.0/schema.json";
const BASE_PATH = "/.well-known/agent-skills";
const OUTPUT_DIR = path.join(
  process.cwd(),
  "public",
  ".well-known",
  "agent-skills",
);

const BLOCK_SIZE = 512;

async function downloadTarball(url) {
  const res = await fetch(url, { redirect: "follow" });
  if (!res.ok) {
    throw new Error(
      `download failed: ${res.status} ${res.statusText} (${url})`,
    );
  }
  return Buffer.from(await res.arrayBuffer());
}

function readOctal(header, offset, length) {
  const raw = header
    .toString("ascii", offset, offset + length)
    .replace(/\0.*$/s, "")
    .trim();
  return raw === "" ? 0 : parseInt(raw, 8);
}

function readString(header, offset, length) {
  return header.toString("utf8", offset, offset + length).replace(/\0.*$/s, "");
}

// Minimal tar reader for the GitHub-generated tarball: regular files only,
// GNU longname ('L') honored, pax headers ('x'/'g') and directories skipped.
function* readTarEntries(tarBuffer) {
  let offset = 0;
  let pendingLongName = null;
  while (offset + BLOCK_SIZE <= tarBuffer.length) {
    const header = tarBuffer.subarray(offset, offset + BLOCK_SIZE);
    if (header.every((byte) => byte === 0)) {
      break;
    }
    const size = readOctal(header, 124, 12);
    const typeflag = String.fromCharCode(header[156]);
    const prefix = readString(header, 345, 155);
    let name = readString(header, 0, 100);
    if (prefix !== "") {
      name = `${prefix}/${name}`;
    }
    if (pendingLongName !== null) {
      name = pendingLongName;
      pendingLongName = null;
    }
    const data = tarBuffer.subarray(
      offset + BLOCK_SIZE,
      offset + BLOCK_SIZE + size,
    );
    offset += BLOCK_SIZE + Math.ceil(size / BLOCK_SIZE) * BLOCK_SIZE;
    if (typeflag === "L") {
      pendingLongName = data.toString("utf8").replace(/\0.*$/s, "");
      continue;
    }
    if (typeflag === "0" || typeflag === "\0") {
      yield { name, data: Buffer.from(data) };
    }
  }
}

// Maps repo-relative path -> file contents, with the tarball's single
// top-level directory stripped.
function extractRepoFiles(tarballBuffer) {
  const files = new Map();
  for (const entry of readTarEntries(zlib.gunzipSync(tarballBuffer))) {
    const repoRelative = entry.name.split("/").slice(1).join("/");
    if (repoRelative !== "") {
      files.set(repoRelative, entry.data);
    }
  }
  return files;
}

function writeTarOctal(header, offset, length, value) {
  header.write(value.toString(8).padStart(length - 1, "0"), offset, "ascii");
}

function tarFileHeader(name, size) {
  if (Buffer.byteLength(name, "utf8") > 100) {
    throw new Error(`archive entry name exceeds 100 bytes: ${name}`);
  }
  const header = Buffer.alloc(BLOCK_SIZE);
  header.write(name, 0, "utf8");
  writeTarOctal(header, 100, 8, 0o644); // mode
  writeTarOctal(header, 108, 8, 0); // uid
  writeTarOctal(header, 116, 8, 0); // gid
  writeTarOctal(header, 124, 12, size);
  writeTarOctal(header, 136, 12, 0); // mtime
  header.fill(0x20, 148, 156); // checksum placeholder: spaces
  header.write("0", 156, "ascii"); // typeflag: regular file
  header.write("ustar", 257, "ascii");
  header.write("00", 263, "ascii"); // ustar version
  let checksum = 0;
  for (const byte of header) {
    checksum += byte;
  }
  header.write(`${checksum.toString(8).padStart(6, "0")}\0 `, 148, "ascii");
  return header;
}

// Builds a deterministic .tar.gz: entries sorted by path, fixed metadata.
function buildArchive(entries) {
  const chunks = [];
  const sorted = [...entries].sort((a, b) =>
    a.name < b.name ? -1 : a.name > b.name ? 1 : 0,
  );
  for (const entry of sorted) {
    chunks.push(tarFileHeader(entry.name, entry.data.length));
    chunks.push(entry.data);
    const padding =
      (BLOCK_SIZE - (entry.data.length % BLOCK_SIZE)) % BLOCK_SIZE;
    if (padding > 0) {
      chunks.push(Buffer.alloc(padding));
    }
  }
  chunks.push(Buffer.alloc(BLOCK_SIZE * 2)); // end-of-archive marker
  return zlib.gzipSync(Buffer.concat(chunks), { level: 9 });
}

function skillDescription(skillName, skillMd) {
  const { data } = matter(skillMd.toString("utf8"));
  const description =
    typeof data.description === "string" ? data.description.trim() : "";
  if (description === "") {
    throw new Error(
      `skill "${skillName}" has no description in its SKILL.md frontmatter`,
    );
  }
  return description;
}

function collectSkills(repoFiles) {
  const skills = new Map();
  for (const [file, data] of repoFiles) {
    const match = file.match(/^skills\/([^/]+)\/(.+)$/);
    if (!match) {
      continue;
    }
    const [, name, relative] = match;
    if (!skills.has(name)) {
      skills.set(name, []);
    }
    skills.get(name).push({ name: relative, data });
  }
  // A directory without SKILL.md is not a skill.
  for (const [name, files] of skills) {
    if (!files.some((file) => file.name === "SKILL.md")) {
      skills.delete(name);
    }
  }
  return skills;
}

function sha256Digest(buffer) {
  return `sha256:${crypto.createHash("sha256").update(buffer).digest("hex")}`;
}

async function main() {
  console.log(`[agent-skills] downloading ${TARBALL_URL}`);
  const repoFiles = extractRepoFiles(await downloadTarball(TARBALL_URL));
  const skills = collectSkills(repoFiles);
  if (skills.size === 0) {
    throw new Error("no skills found in the agent-skills repository tarball");
  }

  fs.rmSync(OUTPUT_DIR, { recursive: true, force: true });
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });

  const index = [];
  for (const name of [...skills.keys()].sort()) {
    const files = skills.get(name);
    const skillMd = files.find((file) => file.name === "SKILL.md");
    const description = skillDescription(name, skillMd.data);
    let entry;
    if (files.length === 1) {
      const target = path.join(OUTPUT_DIR, name, "SKILL.md");
      fs.mkdirSync(path.dirname(target), { recursive: true });
      fs.writeFileSync(target, skillMd.data);
      entry = {
        name,
        type: "skill-md",
        description,
        url: `${BASE_PATH}/${name}/SKILL.md`,
        digest: sha256Digest(skillMd.data),
      };
    } else {
      const archive = buildArchive(files);
      fs.writeFileSync(path.join(OUTPUT_DIR, `${name}.tar.gz`), archive);
      entry = {
        name,
        type: "archive",
        description,
        url: `${BASE_PATH}/${name}.tar.gz`,
        digest: sha256Digest(archive),
      };
    }
    index.push(entry);
    console.log(`[agent-skills] ${name} (${entry.type}) ${entry.digest}`);
  }

  const indexJson = `${JSON.stringify({ $schema: SCHEMA_URL, skills: index }, null, 2)}\n`;
  fs.writeFileSync(path.join(OUTPUT_DIR, "index.json"), indexJson);
  console.log(
    `[agent-skills] wrote ${index.length} skills to ` +
      path.relative(process.cwd(), path.join(OUTPUT_DIR, "index.json")),
  );
}

main().catch((err) => {
  console.error(`[agent-skills] ${err?.message ?? err}`);
  process.exit(1);
});
