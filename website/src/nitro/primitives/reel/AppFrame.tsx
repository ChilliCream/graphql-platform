/**
 * AppFrame — the shared Nitro desktop-IDE shell that frames every tab screen: a 50px icon
 * rail + optional aside + a 36px toolbar slot + main content + a 23px footer. Static chrome,
 * authored in the reel canvas px.
 */
import type { ComponentType, CSSProperties, ReactNode } from "react";
import { token } from "../../lib/tokens";
import {
  NitroLogo as NitroMark,
  IconDocuments,
  IconEnvironment,
  IconHistory,
  IconAccount,
  IconSettings,
  IconOrganization,
  IconWorkspace,
  IconSync,
  IconOnline,
  IconErrorCircle,
  IconInfo,
  IconWarning,
  IconShortcuts,
  IconHelp,
  type IconProps,
} from "../icons";

const RAIL_W = 50;
export const FOOTER_H = 23;

// The real Nitro desktop rail: Documents / Environments / History explorers (app/shell/sidebar.tsx).
export type RailKey = "documents" | "environments" | "history";

const RAIL_ITEMS: { key: RailKey; Icon: ComponentType<IconProps> }[] = [
  { key: "documents", Icon: IconDocuments },
  { key: "environments", Icon: IconEnvironment },
  { key: "history", Icon: IconHistory },
];

function RailButton({
  Icon,
  active,
}: {
  Icon: ComponentType<IconProps>;
  active?: boolean;
}) {
  return (
    <div
      style={{
        position: "relative",
        width: RAIL_W,
        height: RAIL_W,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: active ? token.textStrong : token.textSecondary,
      }}
    >
      {active && (
        <span
          style={{
            position: "absolute",
            left: 0,
            top: 7,
            bottom: 7,
            width: 3,
            borderRadius: "0 3px 3px 0",
            background: token.graphEdgeActive,
          }}
        />
      )}
      <div
        style={{
          width: 36,
          height: 36,
          borderRadius: 4,
          background: active ? token.highlight : "transparent",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Icon size={20} />
      </div>
    </div>
  );
}

function NitroLogo() {
  return (
    <div
      style={{
        width: 36,
        height: 36,
        margin: "8px auto",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: token.textStrong,
      }}
    >
      <NitroMark size={28} />
    </div>
  );
}

function Footer({ counts = [0, 0, 0] }: { counts?: [number, number, number] }) {
  const item = (icon: ReactNode, label: string, color?: string) => (
    <span
      style={{
        display: "flex",
        alignItems: "center",
        gap: 5,
        color: color ?? token.textSecondary,
      }}
    >
      {icon}
      {label}
    </span>
  );
  const sz = 13;
  return (
    <div
      style={{
        height: FOOTER_H,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 14,
        padding: "0 8px",
        background: token.surface,
        borderTop: `1px solid ${token.border}`,
        fontSize: 11,
        color: token.textSecondary,
        whiteSpace: "nowrap",
      }}
    >
      {item(
        <IconOnline size={9} color={token.successText} />,
        "Online",
        token.successText,
      )}
      {item(
        <IconAccount size={sz} color="currentColor" />,
        "pascal@chillicream.com",
      )}
      {item(<IconOrganization size={sz} color="currentColor" />, "ChilliCream")}
      {item(<IconWorkspace size={sz} color="currentColor" />, "Default")}
      {item(
        <IconEnvironment size={sz} color="currentColor" />,
        "No Environment",
      )}
      {item(<IconSync size={sz} color="currentColor" />, "Synchronize")}
      <span
        style={{
          display: "flex",
          alignItems: "center",
          gap: 10,
          marginLeft: 4,
        }}
      >
        {item(
          <IconErrorCircle
            size={sz}
            color={counts[0] ? token.errorText : "currentColor"}
          />,
          String(counts[0]),
          counts[0] ? token.errorText : undefined,
        )}
        {item(<IconInfo size={sz} color="currentColor" />, String(counts[1]))}
        {item(
          <IconWarning
            size={sz}
            color={counts[2] ? token.warning : "currentColor"}
          />,
          String(counts[2]),
          counts[2] ? token.warning : undefined,
        )}
      </span>
      <span
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 12,
        }}
      >
        <IconShortcuts size={sz} color="currentColor" />
        <IconHelp size={sz} color="currentColor" />
      </span>
    </div>
  );
}

export interface AppFrameProps {
  railActive: RailKey;
  /** optional left explorer pane (e.g. a document tree) */
  aside?: ReactNode;
  asideWidth?: number;
  /** optional 36px toolbar above the content */
  toolbar?: ReactNode;
  children: ReactNode;
  footerCounts?: [number, number, number];
  width?: number;
  height?: number;
  style?: CSSProperties;
}

export function AppFrame({
  railActive,
  aside,
  asideWidth = 256,
  toolbar,
  children,
  footerCounts,
  width = 1504,
  height = 940,
  style,
}: AppFrameProps) {
  return (
    <div
      aria-hidden
      style={{
        position: "relative",
        width,
        height,
        display: "flex",
        flexDirection: "column",
        background: token.bg,
        color: token.text,
        ...style,
      }}
    >
      <div style={{ flex: 1, minHeight: 0, display: "flex" }}>
        {/* icon rail */}
        <div
          style={{
            width: RAIL_W,
            flex: "0 0 auto",
            background: token.surface,
            borderRight: `1px solid ${token.border}`,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
          }}
        >
          <NitroLogo />
          {RAIL_ITEMS.map((it) => (
            <RailButton
              key={it.key}
              Icon={it.Icon}
              active={it.key === railActive}
            />
          ))}
          <div style={{ marginTop: "auto" }} />
          <RailButton Icon={IconAccount} />
          <RailButton Icon={IconSettings} />
        </div>

        {/* optional aside */}
        {aside && (
          <div
            style={{
              width: asideWidth,
              flex: "0 0 auto",
              background: token.surface,
              borderRight: `1px solid ${token.border}`,
              minWidth: 0,
              overflow: "hidden",
            }}
          >
            {aside}
          </div>
        )}

        {/* main */}
        <div
          style={{
            flex: 1,
            minWidth: 0,
            display: "flex",
            flexDirection: "column",
            background: token.surface,
          }}
        >
          {toolbar && (
            <div
              style={{
                height: 36,
                flex: "0 0 auto",
                display: "flex",
                alignItems: "center",
                padding: "0 8px",
                borderBottom: `1px solid ${token.border}`,
              }}
            >
              {toolbar}
            </div>
          )}
          <div style={{ flex: 1, minHeight: 0, position: "relative" }}>
            {children}
          </div>
        </div>
      </div>
      <Footer counts={footerCounts} />
    </div>
  );
}
