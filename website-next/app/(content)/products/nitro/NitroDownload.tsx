"use client";

import { load } from "js-yaml";
import { useEffect, useRef, useState } from "react";

const DOWNLOAD_BASE_URL = "https://cdn.chillicream.com/app/";

const WEB_STABLE_URL = "https://nitro.chillicream.com";

const WEB_INSIDER_URL = "https://insider.chillicream.com";

type OS = "linux" | "mac" | "windows";

interface AppInfoFile {
  readonly filename: string;
  readonly text: string;
}

interface LinuxAppInfo {
  readonly os: "linux";
  readonly appImage: AppInfoFile;
}

interface MacOSAppInfo {
  readonly os: "mac";
  readonly intel: AppInfoFile;
  readonly silicon: AppInfoFile;
  readonly universal: AppInfoFile;
}

interface WindowsAppInfo {
  readonly os: "windows";
  readonly arm64: AppInfoFile;
  readonly x64: AppInfoFile;
  readonly universal: AppInfoFile;
}

interface AppInfoVariant {
  readonly linux: LinuxAppInfo;
  readonly macOS: MacOSAppInfo;
  readonly windows: WindowsAppInfo;
}

interface AppMatrix {
  readonly stable: AppInfoVariant;
  readonly insider: AppInfoVariant;
}

type ActiveAppInfo = LinuxAppInfo | MacOSAppInfo | WindowsAppInfo;

function getOS(): OS | null {
  if (navigator.userAgent.indexOf("Win") >= 0) {
    return "windows";
  }

  if (navigator.userAgent.indexOf("Mac") >= 0) {
    return "mac";
  }

  if (
    navigator.userAgent.indexOf("X11") >= 0 ||
    navigator.userAgent.indexOf("Linux") >= 0
  ) {
    return "linux";
  }

  return null;
}

type Variant = "nitro" | "nitro-insider";

interface LatestAppInfo {
  readonly version: string;
  readonly files: { readonly url: string }[];
}

async function fetchAppInfo(variant: Variant, os: OS): Promise<LatestAppInfo> {
  const filename =
    os === "windows" ? `${variant}.yml` : `${variant}-${os}.yml`;
  const response = await fetch(
    DOWNLOAD_BASE_URL + filename + "?no-cache=" + new Date().getTime()
  );

  if (!response.ok) {
    throw new Error(
      `Failed to fetch ${filename}: ${response.status} ${response.statusText}`
    );
  }

  const text = await response.text();
  const parsed = load(text);

  if (
    typeof parsed !== "object" ||
    parsed === null ||
    !Array.isArray((parsed as { files?: unknown }).files) ||
    !(parsed as LatestAppInfo).files.every(
      (file) => typeof file?.url === "string"
    )
  ) {
    throw new Error(`Invalid app info manifest: ${filename}`);
  }

  return parsed as LatestAppInfo;
}

function findFile(
  files: LatestAppInfo["files"],
  predicate: (url: string) => boolean,
  description: string
): string {
  const match = files.find((file) => predicate(file.url));

  if (!match) {
    throw new Error(`Missing required file: ${description}`);
  }

  return match.url;
}

async function fetchLinuxAppInfo(variant: Variant): Promise<LinuxAppInfo> {
  const { files } = await fetchAppInfo(variant, "linux");

  return {
    os: "linux",
    appImage: {
      filename: findFile(
        files,
        (url) => url.endsWith(".AppImage"),
        "Linux AppImage"
      ),
      text: "Linux",
    },
  };
}

async function fetchMacOSAppInfo(variant: Variant): Promise<MacOSAppInfo> {
  const { files } = await fetchAppInfo(variant, "mac");
  const isDmg = (url: string) => url.endsWith(".dmg");

  return {
    os: "mac",
    intel: {
      filename: findFile(
        files,
        (url) => isDmg(url) && url.includes("x64"),
        "Mac Intel (.dmg)"
      ),
      text: "Mac Intel",
    },
    silicon: {
      filename: findFile(
        files,
        (url) => isDmg(url) && url.includes("arm64"),
        "Mac Silicon (.dmg)"
      ),
      text: "Mac Silicon",
    },
    universal: {
      filename: findFile(
        files,
        (url) => isDmg(url) && url.includes("universal"),
        "Mac Universal (.dmg)"
      ),
      text: "Mac Universal",
    },
  };
}

async function fetchWindowsAppInfo(variant: Variant): Promise<WindowsAppInfo> {
  const { files } = await fetchAppInfo(variant, "windows");
  const isExe = (url: string) => url.endsWith(".exe");

  return {
    os: "windows",
    arm64: {
      filename: findFile(
        files,
        (url) => isExe(url) && url.includes("arm64"),
        "Windows arm64 (.exe)"
      ),
      text: "Windows arm64",
    },
    x64: {
      filename: findFile(
        files,
        (url) => isExe(url) && url.includes("x64"),
        "Windows x64 (.exe)"
      ),
      text: "Windows x64",
    },
    universal: {
      filename: findFile(
        files,
        (url) =>
          isExe(url) && !url.includes("arm64") && !url.includes("x64"),
        "Windows Universal (.exe)"
      ),
      text: "Windows Universal",
    },
  };
}

async function fetchVariantAppInfo(
  variant: Variant,
  os: OS
): Promise<ActiveAppInfo> {
  switch (os) {
    case "linux":
      return fetchLinuxAppInfo(variant);
    case "mac":
      return fetchMacOSAppInfo(variant);
    case "windows":
      return fetchWindowsAppInfo(variant);
  }
}

type ActiveAppInfoStatus =
  | { readonly state: "loading" }
  | { readonly state: "ready"; readonly activeStable?: ActiveAppInfo };

// Fetches only the active OS's stable manifest on mount to power the primary
// CTA. If the OS can't be detected, nothing is fetched (the matrix is loaded
// lazily when the dropdown opens).
function useActiveAppInfo(): ActiveAppInfoStatus {
  const [status, setStatus] = useState<ActiveAppInfoStatus>({
    state: "loading",
  });

  useEffect(() => {
    const os = getOS();

    let cancelled = false;

    // When no OS is detected there's nothing to fetch, so resolve immediately
    // to the "ready" state with no active build.
    const pending = os
      ? fetchVariantAppInfo("nitro", os).then(
          (info): ActiveAppInfoStatus => ({ state: "ready", activeStable: info })
        )
      : Promise.resolve<ActiveAppInfoStatus>({ state: "ready" });

    pending
      .then((next) => {
        if (cancelled) {
          return;
        }

        setStatus(next);
      })
      .catch(() => {
        // CDN unreachable: leave the placeholder linking to the web version.
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return status;
}

type AppMatrixStatus =
  | { readonly state: "idle" }
  | { readonly state: "ready"; readonly matrix: AppMatrix }
  | { readonly state: "error" };

// Lazily fetches the full stable + insider matrix the first time it's enabled
// (i.e. the dropdown is opened), then caches it for subsequent opens.
function useAppMatrix(enabled: boolean): AppMatrixStatus {
  const [status, setStatus] = useState<AppMatrixStatus>({ state: "idle" });
  const requested = useRef(false);

  useEffect(() => {
    // Fetch at most once across the component's lifetime.
    if (!enabled || requested.current) {
      return;
    }

    requested.current = true;
    let cancelled = false;

    Promise.all([
      fetchLinuxAppInfo("nitro"),
      fetchLinuxAppInfo("nitro-insider"),
      fetchMacOSAppInfo("nitro"),
      fetchMacOSAppInfo("nitro-insider"),
      fetchWindowsAppInfo("nitro"),
      fetchWindowsAppInfo("nitro-insider"),
    ])
      .then(
        ([
          linux,
          linuxInsider,
          macOS,
          macOSInsider,
          windows,
          windowsInsider,
        ]) => {
          if (cancelled) {
            return;
          }

          setStatus({
            state: "ready",
            matrix: {
              stable: { linux, macOS, windows },
              insider: {
                linux: linuxInsider,
                macOS: macOSInsider,
                windows: windowsInsider,
              },
            },
          });
        }
      )
      .catch(() => {
        if (cancelled) {
          return;
        }

        // CDN unreachable: keep the tfoot web/insider links available.
        setStatus({ state: "error" });
      });

    return () => {
      cancelled = true;
    };
  }, [enabled]);

  return status;
}

function getActiveDownload(
  active: ActiveAppInfo | undefined
): { url: string; text: string; filename?: string } {
  switch (active?.os) {
    case "linux":
      return {
        url: DOWNLOAD_BASE_URL + active.appImage.filename,
        text: "Download " + active.appImage.text,
        filename: active.appImage.filename,
      };
    case "mac":
    case "windows":
      return {
        url: DOWNLOAD_BASE_URL + active.universal.filename,
        text: "Download " + active.universal.text,
        filename: active.universal.filename,
      };
    default:
      return { url: WEB_STABLE_URL, text: "Open Web Version" };
  }
}

function DownloadAppLink({
  filename,
  onClick,
}: {
  readonly filename: string;
  readonly onClick?: () => void;
}) {
  return (
    <a
      href={DOWNLOAD_BASE_URL + filename}
      download={filename}
      rel="noopener noreferrer nofollow"
      onClick={onClick}
      className="flex items-center justify-center py-1 text-cc-ink hover:text-cc-white"
      aria-label={"Download " + filename}
    >
      <svg
        viewBox="0 0 512 512"
        className="h-4 w-4 fill-current"
        aria-hidden="true"
      >
        <path d="M256 0a256 256 0 1 0 0 512 256 256 0 1 0 0-512zM376.9 294.6L269.8 394.5c-3.8 3.5-8.7 5.5-13.8 5.5s-10.1-2-13.8-5.5L135.1 294.6c-4.5-4.2-7.1-10.1-7.1-16.3c0-12.3 10-22.3 22.3-22.3l57.7 0 0-96c0-17.7 14.3-32 32-32l32 0c17.7 0 32 14.3 32 32l0 96 57.7 0c12.3 0 22.3 10 22.3 22.3c0 6.2-2.6 12.1-7.1 16.3z" />
      </svg>
    </a>
  );
}

const MATRIX_ROWS: {
  readonly os: string;
  readonly type: string;
  readonly pick: (variant: AppInfoVariant) => string;
  readonly groupStart?: boolean;
}[] = [
  {
    os: "macOS 64",
    type: "Universal",
    pick: (v) => v.macOS.universal.filename,
  },
  { os: "", type: "Silicon", pick: (v) => v.macOS.silicon.filename },
  { os: "", type: "Intel", pick: (v) => v.macOS.intel.filename },
  {
    os: "Windows 64",
    type: "Universal",
    pick: (v) => v.windows.universal.filename,
    groupStart: true,
  },
  { os: "", type: "arm64", pick: (v) => v.windows.arm64.filename },
  { os: "", type: "x64", pick: (v) => v.windows.x64.filename },
  {
    os: "Linux x64",
    type: "AppImage",
    pick: (v) => v.linux.appImage.filename,
    groupStart: true,
  },
];

export function NitroDownload() {
  const activeStatus = useActiveAppInfo();
  const [open, setOpen] = useState(false);
  const matrixStatus = useAppMatrix(open);
  const matrix =
    matrixStatus.state === "ready" ? matrixStatus.matrix : undefined;
  const matrixLoading = matrixStatus.state === "idle";
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
      }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  // While the active-OS fetch is pending or if it failed (the status never
  // resolves to "ready"), fall back to the plain web link.
  const activeStable =
    activeStatus.state === "ready" ? activeStatus.activeStable : undefined;
  const os = activeStatus.state === "ready" ? getOS() : null;

  if (activeStatus.state !== "ready" || (os !== null && !activeStable)) {
    return (
      <a
        href={WEB_STABLE_URL}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex cursor-pointer items-center justify-center rounded-full bg-cc-ink px-7 py-3 text-sm font-medium text-cc-surface no-underline transition-colors hover:bg-cc-white"
      >
        Open Web Version
      </a>
    );
  }

  const active = getActiveDownload(activeStable);

  return (
    <div ref={containerRef} className="relative inline-flex">
      <a
        href={active.url}
        download={active.filename}
        rel="noopener noreferrer nofollow"
        className="inline-flex cursor-pointer flex-col items-center justify-center rounded-l-full bg-cc-ink py-2 pl-7 pr-4 text-sm font-medium leading-tight text-cc-surface no-underline transition-colors hover:bg-cc-white"
      >
        {active.text}
        <span className="text-xs opacity-80">Stable Build</span>
      </a>
      <button
        type="button"
        aria-label="Show all downloads"
        aria-expanded={open}
        onClick={() => setOpen((prev) => !prev)}
        className="inline-flex cursor-pointer items-center justify-center rounded-r-full border-l border-cc-surface/30 bg-cc-ink py-2 pl-3 pr-5 text-cc-surface transition-colors hover:bg-cc-white"
      >
        <svg
          viewBox="0 0 448 512"
          className="h-3.5 w-3.5 fill-current"
          aria-hidden="true"
        >
          <path d="M201.4 374.6c12.5 12.5 32.8 12.5 45.3 0l160-160c12.5-12.5 12.5-32.8 0-45.3s-32.8-12.5-45.3 0L224 306.7 86.6 169.4c-12.5-12.5-32.8-12.5-45.3 0s-12.5 32.8 0 45.3l160 160z" />
        </svg>
      </button>
      {open && (
        <div className="absolute left-1/2 top-full z-10 mt-2 -translate-x-1/2 rounded-xl border border-cc-card-border bg-cc-card-bg backdrop-blur-md">
          <table className="w-full border-collapse text-sm text-cc-ink">
            <thead>
              <tr>
                <th className="px-3 py-1.5" />
                <th className="px-3 py-1.5" />
                <th className="px-3 py-1.5 text-center font-semibold">
                  Stable
                </th>
                <th className="px-3 py-1.5 text-center font-semibold">
                  Insider
                </th>
              </tr>
            </thead>
            {matrix ? (
              <tbody>
                {MATRIX_ROWS.map((row) => (
                  <tr
                    key={row.os + row.type}
                    className={
                      row.groupStart || row.os
                        ? "border-t border-cc-card-border"
                        : ""
                    }
                  >
                    <td className="whitespace-nowrap px-3 py-1.5 font-semibold">
                      {row.os}
                    </td>
                    <td className="whitespace-nowrap px-3 py-1.5 text-cc-ink-dim">
                      {row.type}
                    </td>
                    <td className="px-3 py-1.5 text-center">
                      <DownloadAppLink
                        filename={row.pick(matrix.stable)}
                        onClick={() => setOpen(false)}
                      />
                    </td>
                    <td className="px-3 py-1.5 text-center">
                      <DownloadAppLink
                        filename={row.pick(matrix.insider)}
                        onClick={() => setOpen(false)}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            ) : (
              <tbody>
                <tr className="border-t border-cc-card-border">
                  <td
                    colSpan={4}
                    className="px-3 py-3 text-center text-cc-ink-dim"
                  >
                    {matrixLoading
                      ? "Loading downloads…"
                      : "Downloads unavailable"}
                  </td>
                </tr>
              </tbody>
            )}
            <tfoot>
              <tr className="border-t border-cc-card-border">
                <td
                  colSpan={4}
                  className="whitespace-nowrap px-3 py-2 text-center"
                >
                  <a
                    href={WEB_STABLE_URL}
                    target="_blank"
                    rel="noopener noreferrer"
                    onClick={() => setOpen(false)}
                    className="text-cc-ink hover:text-cc-white"
                  >
                    Open Web Version
                  </a>
                  <span className="mx-2 text-cc-ink-dim">|</span>
                  <a
                    href={WEB_INSIDER_URL}
                    target="_blank"
                    rel="noopener noreferrer"
                    onClick={() => setOpen(false)}
                    className="text-cc-ink hover:text-cc-white"
                  >
                    Open Insider Version
                  </a>
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  );
}
