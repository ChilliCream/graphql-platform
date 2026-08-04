import { token } from "../../lib/tokens";
import { UnderlineTab } from "@/src/nitro/primitives/UnderlineTab";
import {
  IconApiGateway,
  IconClose,
  IconPlus,
  IconSave,
  IconMore,
  IconChevronDown,
  IconLock,
  IconSettings,
} from "../icons";

export const GW_DOCTABS_H = 38;
export const GW_VIEWNAV_H = 36;
export const GW_HEADER_H = GW_DOCTABS_H + GW_VIEWNAV_H;

export type GatewayView =
  | "Overview"
  | "Monitoring"
  | "Logs"
  | "Schema"
  | "Deployments"
  | "Changelog"
  | "Operations"
  | "Clients"
  | "Stages";

const VIEWS: GatewayView[] = [
  "Overview",
  "Monitoring",
  "Logs",
  "Schema",
  "Deployments",
  "Changelog",
  "Operations",
  "Clients",
  "Stages",
];

export function GatewayChrome({ activeView }: { activeView: GatewayView }) {
  return (
    <>
      <div
        style={{
          height: GW_DOCTABS_H,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "flex-end",
          gap: 6,
          padding: "0 8px",
          background: token.bg,
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <div
          style={{
            position: "relative",
            height: 30,
            display: "flex",
            alignItems: "center",
            gap: 6,
            padding: "0 10px",
            borderRadius: "5px 5px 0 0",
            border: `1px solid ${token.border}`,
            background: token.surface,
            color: token.text,
            maxWidth: 180,
          }}
        >
          <IconApiGateway size={12} color={token.icObject} />
          <span style={{ fontSize: 12.5, whiteSpace: "nowrap" }}>
            EShops Gateway
          </span>
          <span style={{ color: token.textSecondary, display: "flex" }}>
            <IconClose size={11} color="currentColor" />
          </span>
        </div>
        <span
          style={{
            display: "flex",
            gap: 8,
            marginLeft: 6,
            paddingBottom: 6,
            color: token.textSecondary,
          }}
        >
          <span data-testid="gw-plus" style={{ display: "flex" }}>
            <IconPlus size={15} color="currentColor" />
          </span>
          <IconSave size={15} color="currentColor" />
          <IconMore size={15} color="currentColor" />
        </span>
      </div>
      <div
        style={{
          height: GW_VIEWNAV_H,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 20,
          padding: "0 12px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        {VIEWS.map((v) => {
          const on = v === activeView;
          return (
            <UnderlineTab
              key={v}
              testId={`gw-view-${v}`}
              label={v}
              active={on}
              height="100%"
            />
          );
        })}
        <span
          style={{
            marginLeft: "auto",
            display: "flex",
            alignItems: "center",
            gap: 6,
            fontSize: 12,
            color: token.text,
            border: `1px solid ${token.border}`,
            borderRadius: 5,
            padding: "4px 8px",
          }}
        >
          Production <IconChevronDown size={12} color={token.textSecondary} />
        </span>
        <span
          style={{
            display: "flex",
            alignItems: "center",
            gap: 5,
            fontSize: 12,
            color: token.text,
          }}
        >
          <IconLock size={12} color={token.textSecondary} />
          eshops.fusion.cloud
        </span>
        <span style={{ color: token.textSecondary, display: "flex" }}>
          <IconSettings size={14} color="currentColor" />
        </span>
      </div>
    </>
  );
}
