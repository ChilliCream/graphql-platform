import type { CSSProperties } from "react";
import { token } from "../../lib/tokens";
import { StrokeIcon } from "./StrokeIcon";

export const STATUSBAR_H = 24;

const I = ({ d, fill = "none" }: { d: string; fill?: string }) => (
  <StrokeIcon d={d} size={12} strokeWidth={1.7} fill={fill} />
);
const P = {
  user: "M12 12a4 4 0 100-8 4 4 0 000 8zM4 20a8 8 0 0116 0",
  building: "M5 21V5l7-2 7 2v16M9 9h.01M9 13h.01M15 9h.01M15 13h.01",
  branch:
    "M6 3v12M6 21a3 3 0 100-6 3 3 0 000 6zM6 6a3 3 0 100-6 3 3 0 000 6zM18 9a3 3 0 100-6 3 3 0 000 6zM18 6c0 6-12 3-12 9",
  sync: "M20 11a8 8 0 10-2 5m2 0v-5h-5",
  xc: "M12 3a9 9 0 100 18 9 9 0 000-18zM9 9l6 6M15 9l-6 6",
  info: "M12 3a9 9 0 100 18 9 9 0 000-18zM12 11v5M12 8h.01",
  warn: "M12 3l9 16H3zM12 10v4M12 17h.01",
  help: "M12 3a9 9 0 100 18 9 9 0 000-18zM9.5 9a2.5 2.5 0 113 2.4V13M12 16h.01",
  screen: "M3 5h18v11H3zM9 20h6M12 16v4",
};

function Item({
  icon,
  label,
  color,
}: {
  icon: React.ReactNode;
  label: string;
  color?: string;
}) {
  return (
    <span
      style={{
        display: "flex",
        alignItems: "center",
        gap: 4,
        color: color ?? token.textSecondary,
      }}
    >
      {icon}
      {label}
    </span>
  );
}

export function StatusBar({ style }: { style?: CSSProperties }) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 14,
        height: STATUSBAR_H,
        padding: "0 12px",
        background: "#13171e",
        borderTop: `1px solid ${token.border}`,
        fontSize: 11,
        color: token.textSecondary,
        whiteSpace: "nowrap",
        ...style,
      }}
    >
      <span
        style={{
          display: "flex",
          alignItems: "center",
          gap: 5,
          color: token.successText,
        }}
      >
        <span
          style={{
            width: 7,
            height: 7,
            borderRadius: "50%",
            background: token.successText,
          }}
        />{" "}
        Online
      </span>
      <Item icon={<I d={P.user} />} label="pascal@chillicream.com" />
      <Item icon={<I d={P.building} />} label="ChilliCream" />
      <Item icon={<I d={P.branch} />} label="Default" />
      <Item icon={<I d={P.branch} />} label="No Environment" />
      <Item icon={<I d={P.sync} />} label="Synchronize" />
      <Item icon={<I d={P.xc} />} label="0" />
      <Item icon={<I d={P.info} />} label="0" />
      <Item icon={<I d={P.warn} />} label="0" />
      <span
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 14,
        }}
      >
        <Item icon={<I d={P.help} />} label="0" />
        <Item icon={<I d={P.screen} />} label="0" />
      </span>
    </div>
  );
}
