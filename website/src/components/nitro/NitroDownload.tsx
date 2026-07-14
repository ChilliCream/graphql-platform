"use client";

import { load } from "js-yaml";
import {
  useEffect,
  useId,
  useRef,
  useState,
  useSyncExternalStore,
} from "react";

import { SolidButton } from "@/src/design-system/Button";
import { Icon } from "@/src/icons/Icon";

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
  const filename = os === "windows" ? `${variant}.yml` : `${variant}-${os}.yml`;
  const response = await fetch(
    DOWNLOAD_BASE_URL + filename + "?no-cache=" + new Date().getTime(),
  );

  if (!response.ok) {
    throw new Error(
      `Failed to fetch ${filename}: ${response.status} ${response.statusText}`,
    );
  }

  const text = await response.text();
  const parsed = load(text);

  if (
    typeof parsed !== "object" ||
    parsed === null ||
    !Array.isArray((parsed as { files?: unknown }).files) ||
    !(parsed as LatestAppInfo).files.every(
      (file) => typeof file?.url === "string",
    )
  ) {
    throw new Error(`Invalid app info manifest: ${filename}`);
  }

  return parsed as LatestAppInfo;
}

function findFile(
  files: LatestAppInfo["files"],
  predicate: (url: string) => boolean,
  description: string,
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
        "Linux AppImage",
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
        "Mac Intel (.dmg)",
      ),
      text: "Mac Intel",
    },
    silicon: {
      filename: findFile(
        files,
        (url) => isDmg(url) && url.includes("arm64"),
        "Mac Silicon (.dmg)",
      ),
      text: "Mac Silicon",
    },
    universal: {
      filename: findFile(
        files,
        (url) => isDmg(url) && url.includes("universal"),
        "Mac Universal (.dmg)",
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
        "Windows arm64 (.exe)",
      ),
      text: "Windows arm64",
    },
    x64: {
      filename: findFile(
        files,
        (url) => isExe(url) && url.includes("x64"),
        "Windows x64 (.exe)",
      ),
      text: "Windows x64",
    },
    universal: {
      filename: findFile(
        files,
        (url) => isExe(url) && !url.includes("arm64") && !url.includes("x64"),
        "Windows Universal (.exe)",
      ),
      text: "Windows Universal",
    },
  };
}

async function fetchVariantAppInfo(
  variant: Variant,
  os: OS,
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
  | { readonly state: "loading"; readonly os: OS }
  | { readonly state: "ready"; readonly activeStable?: ActiveAppInfo };

const emptySubscribe = () => () => {};

const getServerOS = (): OS | null => null;

// Reads the OS during render, hydration-safe: the server (and the hydration
// pass) sees null, the client sees the real value on the first post-hydration
// render.
function useOS(): OS | null {
  return useSyncExternalStore(emptySubscribe, getOS, getServerOS);
}

// Fetches only the active OS's stable manifest to power the primary CTA. The
// detected OS is exposed while the fetch is pending so the split button can
// render its final footprint immediately. If the OS can't be detected,
// nothing is fetched (the matrix is loaded lazily when the dropdown opens).
function useActiveAppInfo(): ActiveAppInfoStatus {
  const os = useOS();
  const [fetched, setFetched] = useState<{
    readonly activeStable?: ActiveAppInfo;
  } | null>(null);

  useEffect(() => {
    if (os === null) {
      return;
    }

    let cancelled = false;

    fetchVariantAppInfo("nitro", os)
      .then((info) => {
        if (cancelled) {
          return;
        }

        setFetched({ activeStable: info });
      })
      .catch(() => {
        if (cancelled) {
          return;
        }

        // CDN unreachable: downgrade to the plain web link.
        setFetched({});
      });

    return () => {
      cancelled = true;
    };
  }, [os]);

  if (fetched !== null) {
    return { state: "ready", activeStable: fetched.activeStable };
  }

  return os === null ? { state: "ready" } : { state: "loading", os };
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
        },
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

// Labels must match what getActiveDownload derives from the manifest for each
// OS (the universal build on Mac/Windows, the AppImage on Linux), so the
// pending shell and the resolved link render identically.
const SPLIT_BUTTON_LABELS: Record<OS, string> = {
  linux: "Download Linux",
  mac: "Download Mac Universal",
  windows: "Download Windows Universal",
};

function getActiveDownload(active: ActiveAppInfo): {
  url: string;
  text: string;
  filename: string;
} {
  switch (active.os) {
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
  }
}

interface ActiveDownload {
  readonly url?: string;
  readonly text: string;
  readonly filename?: string;
}

// Maps the fetch status to what the split button shows: the resolved download
// link, a link-less shell while the detected OS's manifest is still loading,
// or null when only the web fallback can be offered.
function resolveActiveDownload(
  status: ActiveAppInfoStatus,
): ActiveDownload | null {
  if (status.state === "loading") {
    return { text: SPLIT_BUTTON_LABELS[status.os] };
  }

  return status.activeStable === undefined
    ? null
    : getActiveDownload(status.activeStable);
}

interface DownloadAppLinkProps {
  readonly filename: string;
  readonly onClick?: () => void;
}

function DownloadAppLink({ filename, onClick }: DownloadAppLinkProps) {
  return (
    <a
      href={DOWNLOAD_BASE_URL + filename}
      download={filename}
      rel="noopener noreferrer nofollow"
      onClick={onClick}
      className="text-cc-ink hover:text-cc-white flex items-center justify-center py-1"
      aria-label={"Download " + filename}
    >
      <Icon icon="circle-arrow-down" size="lg" />
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
  const toggleRef = useRef<HTMLButtonElement>(null);
  const panelId = useId();

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
        toggleRef.current?.focus();
      }
    };

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  // No detectable OS (or the CDN was unreachable): offer the web version.
  const active = resolveActiveDownload(activeStatus);

  if (active === null) {
    return <SolidButton href={WEB_STABLE_URL}>Open Web Version</SolidButton>;
  }

  return (
    <div ref={containerRef} className="relative inline-flex">
      <a
        href={active.url}
        download={active.filename}
        rel="noopener noreferrer nofollow"
        className="bg-cc-heading text-cc-surface hover:bg-cc-white inline-flex cursor-pointer flex-col items-center justify-center rounded-l-full py-1.5 pr-4 pl-7 text-sm leading-tight font-medium no-underline transition-colors"
      >
        {active.text}
        <span className="text-xs opacity-80">Stable Build</span>
      </a>
      <button
        ref={toggleRef}
        type="button"
        aria-label="Show all downloads"
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        onClick={() => setOpen((prev) => !prev)}
        className="border-cc-surface/20 bg-cc-heading text-cc-surface hover:bg-cc-white inline-flex cursor-pointer items-center justify-center rounded-r-full border-l py-1.5 pr-5 pl-3 transition-colors"
      >
        <Icon icon="chevron-down" />
      </button>
      {open && (
        <div
          id={panelId}
          className="border-cc-card-border bg-cc-card-bg absolute top-full left-0 z-50 mt-2 max-w-[calc(100vw-2.5rem)] overflow-x-auto rounded-xl border backdrop-blur-md"
        >
          <table className="text-cc-ink w-full border-collapse text-sm">
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
                        ? "border-cc-card-border border-t"
                        : ""
                    }
                  >
                    <td className="px-3 py-1.5 font-semibold whitespace-nowrap">
                      {row.os}
                    </td>
                    <td className="text-cc-ink-dim px-3 py-1.5 whitespace-nowrap">
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
                <tr className="border-cc-card-border border-t">
                  <td
                    colSpan={4}
                    className="text-cc-ink-dim px-3 py-3 text-center"
                  >
                    {matrixLoading
                      ? "Loading downloads…"
                      : "Downloads unavailable"}
                  </td>
                </tr>
              </tbody>
            )}
            <tfoot>
              <tr className="border-cc-card-border border-t">
                <td
                  colSpan={4}
                  className="px-3 py-2 text-center whitespace-nowrap"
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
                  <span className="text-cc-ink-dim mx-2">|</span>
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
