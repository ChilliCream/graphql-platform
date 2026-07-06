/**
 * TopBar — the Nitro / Banana Cake Pop app chrome above a dashboard: a browser-style
 * tab strip, a nav-tab row with a "Development" dropdown +
 * window controls, and a "Development Stage" + date-range row. Static reel chrome.
 */
import type { CSSProperties } from "react";
import { token } from "../../lib/tokens";

export const TOPBAR_H = 112;

const NAV = [
  "Overview",
  "Monitoring",
  "Deployments",
  "Changelog",
  "Operations",
  "Clients",
  "Stages",
];

const I = ({
  d,
  size = 15,
  sw = 1.6,
  fill = "none",
}: {
  d: string;
  size?: number;
  sw?: number;
  fill?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill={fill}
    stroke="currentColor"
    strokeWidth={sw}
    strokeLinecap="round"
    strokeLinejoin="round"
    style={{ flex: "0 0 auto" }}
  >
    <path d={d} />
  </svg>
);
const ICON = {
  close: "M6 6l12 12M18 6L6 18",
  plus: "M12 5v14M5 12h14",
  save: "M5 4h11l3 3v13H5zM8 4v5h7M8 14h8",
  more: "M6 12h.01M12 12h.01M18 12h.01",
  chevron: "M6 9l6 6 6-6",
  refresh: "M20 11a8 8 0 10-2 5m2 0v-5h-5",
  lock: "M6 11h12v9H6zM9 11V8a3 3 0 016 0v3",
  gear: "M12 9a3 3 0 100 6 3 3 0 000-6zM19 12a7 7 0 00-.1-1l2-1.5-2-3.4-2.3 1a7 7 0 00-1.7-1l-.3-2.6H9.4l-.3 2.6a7 7 0 00-1.7 1l-2.3-1-2 3.4L5 11a7 7 0 000 2l-2 1.5 2 3.4 2.3-1a7 7 0 001.7 1l.3 2.6h4.2l.3-2.6a7 7 0 001.7-1l2.3 1 2-3.4-2-1.5a7 7 0 00.1-1z",
  calendar: "M5 6h14v14H5zM5 10h14M9 4v4M15 4v4",
};

export interface TopBarProps {
  active?: string;
  /** the operation name shown in the stage row when in the operation view */
  stage?: string;
  width?: number;
  style?: CSSProperties;
}

export function TopBar({
  active = "Monitoring",
  stage = "Development Stage",
  width,
  style,
}: TopBarProps) {
  return (
    <div style={{ width: width ?? "100%", color: token.text, ...style }}>
      {/* row 1 — browser tab strip */}
      <div
        style={{
          display: "flex",
          alignItems: "flex-end",
          gap: 8,
          height: 38,
          borderBottom: `1px solid ${token.border}`,
          paddingLeft: 4,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            height: 30,
            padding: "0 12px",
            background: token.surface,
            border: `1px solid ${token.border}`,
            borderBottom: "none",
            borderRadius: "6px 6px 0 0",
            minWidth: 150,
          }}
        >
          <span
            style={{
              width: 12,
              height: 12,
              borderRadius: "50%",
              background: token.active,
              flex: "0 0 auto",
            }}
          />
          <span
            style={{ fontSize: 13, fontStyle: "italic", color: token.text }}
          >
            GitHub
          </span>
          <span
            style={{
              marginLeft: "auto",
              color: token.textSecondary,
              display: "flex",
            }}
          >
            <I d={ICON.close} size={13} />
          </span>
        </div>
        <span
          style={{
            color: token.textSecondary,
            display: "flex",
            paddingBottom: 6,
          }}
        >
          <I d={ICON.plus} size={15} />
        </span>
        <span
          style={{
            color: token.textSecondary,
            display: "flex",
            paddingBottom: 6,
          }}
        >
          <I d={ICON.save} size={15} />
        </span>
        <span
          style={{
            color: token.textSecondary,
            display: "flex",
            paddingBottom: 6,
          }}
        >
          <I d={ICON.more} size={15} />
        </span>
      </div>

      {/* row 2 — nav tabs + window controls */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          height: 36,
          borderBottom: `1px solid ${token.border}`,
          paddingLeft: 4,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 22,
            height: "100%",
          }}
        >
          {NAV.map((t) => {
            const on = t === active;
            return (
              <span
                key={t}
                style={{
                  position: "relative",
                  display: "flex",
                  alignItems: "center",
                  height: "100%",
                  fontSize: 13,
                  color: on ? token.textStrong : token.textSecondary,
                }}
              >
                {t}
                {on && (
                  <span
                    style={{
                      position: "absolute",
                      left: 0,
                      right: 0,
                      bottom: -1,
                      height: 2,
                      background: token.accent,
                    }}
                  />
                )}
              </span>
            );
          })}
        </div>
        <div
          style={{
            marginLeft: "auto",
            display: "flex",
            alignItems: "center",
            gap: 10,
            color: token.textSecondary,
            fontSize: 12,
          }}
        >
          <span
            style={{
              display: "flex",
              alignItems: "center",
              gap: 4,
              color: token.text,
            }}
          >
            Development <I d={ICON.chevron} size={13} />
          </span>
          <span style={{ width: 1, height: 16, background: token.border }} />
          <I d={ICON.refresh} size={15} />
          <span style={{ width: 1, height: 16, background: token.border }} />
          <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <I d={ICON.lock} size={14} /> N/A
          </span>
          <I d={ICON.gear} size={15} />
        </div>
      </div>

      {/* row 3 — stage + date range */}
      <div style={{ display: "flex", alignItems: "center", height: 38 }}>
        <span style={{ fontSize: 13, color: token.text }}>{stage}</span>
        <div
          style={{
            marginLeft: "auto",
            display: "flex",
            alignItems: "center",
            gap: 8,
            height: 28,
            padding: "0 10px",
            border: `1px solid ${token.border}`,
            borderRadius: 5,
            background: token.surface,
            color: token.text,
            fontSize: 12,
          }}
        >
          <span style={{ color: token.textSecondary, display: "flex" }}>
            <I d={ICON.calendar} size={13} />
          </span>
          02/06/2024 06:12 AM - 02/06/2024 06:12 AM
        </div>
      </div>
    </div>
  );
}
