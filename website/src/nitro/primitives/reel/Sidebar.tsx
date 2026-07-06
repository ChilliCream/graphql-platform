import type { CSSProperties } from "react";
import { token } from "../../lib/tokens";

export const SIDEBAR_W = 264;
const RAIL_W = 44;

interface Node {
  name: string;
  kind: "folder" | "op" | "file";
  depth: number;
  open?: boolean;
  color?: string;
}

const PINK = "#bf3989";
const GOLD = "#d9a521";

const TREE: Node[] = [
  { name: "Airbnb", kind: "folder", depth: 0, open: true, color: PINK },
  { name: "Bookings", kind: "folder", depth: 1, open: true, color: PINK },
  { name: "bookingById", kind: "op", depth: 2 },
  { name: "CreateBooking", kind: "op", depth: 2 },
  { name: "User", kind: "folder", depth: 1, color: PINK },
  { name: "Banana Cake Pop", kind: "folder", depth: 0, color: GOLD },
  { name: "EShops", kind: "folder", depth: 0, open: true, color: GOLD },
  { name: "Subgraphs", kind: "folder", depth: 1, open: true, color: PINK },
  { name: "createOrder", kind: "op", depth: 2 },
  { name: "SearchProducts", kind: "op", depth: 2 },
  { name: "Example", kind: "folder", depth: 0, open: true, color: PINK },
  { name: "Gateway", kind: "folder", depth: 1, color: GOLD },
  { name: "Fusion", kind: "folder", depth: 0, color: PINK },
  { name: "Netflix", kind: "folder", depth: 0, color: GOLD },
  { name: "Paypal", kind: "folder", depth: 0, color: PINK },
  { name: "Shopify", kind: "folder", depth: 0, color: GOLD },
];

const Svg = ({
  d,
  size = 14,
  sw = 1.5,
  color,
  fill = "none",
}: {
  d: string;
  size?: number;
  sw?: number;
  color?: string;
  fill?: string;
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill={fill}
    stroke={color ?? "currentColor"}
    strokeWidth={sw}
    strokeLinecap="round"
    strokeLinejoin="round"
    style={{ flex: "0 0 auto" }}
  >
    <path d={d} />
  </svg>
);
const P = {
  folder: "M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z",
  caretR: "M9 6l6 6-6 6",
  caretD: "M6 9l6 6 6-6",
  newFile: "M14 3v5h5M14 3H6v18h12V8zM12 12v6M9 15h6",
  newFolder:
    "M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2zM12 11v6M9 14h6",
  newDoc: "M14 3v5h5M14 3H6v18h12V8zM9 13h6M9 16h6",
  search: "M11 4a7 7 0 100 14 7 7 0 000-14zM20 20l-4-4",
  docs: "M8 3h8l4 4v14H8zM8 3v4h-4v14h10M16 3v4h4",
  atom: "M12 12a2 2 0 100-4 2 2 0 000 4zM12 4c5 0 9 3.6 9 8s-4 8-9 8-9-3.6-9-8M3 12c2.5-4.3 6.3-7 9-7s6.5 2.7 9 7",
  clock: "M12 3a9 9 0 100 18 9 9 0 000-18zM12 7v5l3 2",
};

function FileIcon() {
  return (
    <svg
      width="11"
      height="13"
      viewBox="0 0 11 13"
      fill="none"
      stroke={token.info}
      strokeWidth="1"
      strokeLinejoin="round"
      style={{ flex: "0 0 auto" }}
    >
      <path d="M1 1.2A1 1 0 012 0h4l4 3.6V11.8a1 1 0 01-1 1H2a1 1 0 01-1-1zM6 0v4h4" />
    </svg>
  );
}

export interface SidebarProps {
  selected?: string;
  style?: CSSProperties;
}

export function Sidebar({ selected = "createOrder", style }: SidebarProps) {
  const rail = [P.docs, P.atom, P.clock];
  return (
    <div
      aria-hidden
      style={{
        display: "flex",
        height: "100%",
        background: token.bg,
        borderRight: `1px solid ${token.borderStrong}`,
        ...style,
      }}
    >
      <div
        style={{
          width: RAIL_W,
          flex: "0 0 auto",
          borderRight: `1px solid ${token.border}`,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          paddingTop: 10,
          gap: 14,
        }}
      >
        <div
          style={{
            width: 26,
            height: 26,
            borderRadius: "50%",
            background: "#0d1117",
            border: `1px solid ${token.border}`,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <span
            style={{
              width: 11,
              height: 11,
              borderRadius: "50%",
              background: token.active,
            }}
          />
        </div>
        {rail.map((d, i) => (
          <div
            key={i}
            style={{
              position: "relative",
              width: RAIL_W,
              display: "flex",
              justifyContent: "center",
              color: i === 0 ? token.active : token.textSecondary,
            }}
          >
            {i === 0 && (
              <span
                style={{
                  position: "absolute",
                  left: 0,
                  top: -3,
                  bottom: -3,
                  width: 2,
                  background: token.active,
                }}
              />
            )}
            <Svg d={d} size={19} sw={1.5} />
          </div>
        ))}
      </div>

      <div
        style={{
          flex: 1,
          minWidth: 0,
          display: "flex",
          flexDirection: "column",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "11px 10px 7px",
          }}
        >
          <span
            style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
          >
            Documents
          </span>
          <span style={{ display: "flex", gap: 8, color: token.textSecondary }}>
            <Svg d={P.newFile} size={14} />
            <Svg d={P.newFolder} size={14} />
            <Svg d={P.newDoc} size={14} />
          </span>
        </div>
        <div style={{ margin: "0 8px 7px" }}>
          <div
            style={{
              height: 26,
              borderRadius: 5,
              background: token.surface,
              border: `1px solid ${token.border}`,
              display: "flex",
              alignItems: "center",
              gap: 6,
              padding: "0 8px",
              fontSize: 11,
              color: token.textSecondary,
            }}
          >
            <Svg d={P.search} size={12} sw={1.6} />
            Filter…
          </div>
        </div>
        <div style={{ overflow: "hidden", flex: 1 }}>
          {TREE.map((n) => {
            const isSel = n.kind === "op" && n.name === selected;
            return (
              <div
                key={n.name + n.depth}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 5,
                  height: 21,
                  paddingLeft: 8 + n.depth * 13,
                  paddingRight: 8,
                  background: isSel ? token.surface : "transparent",
                  borderLeft: isSel
                    ? `2px solid ${token.accent}`
                    : "2px solid transparent",
                }}
              >
                {n.kind === "folder" ? (
                  <>
                    <span
                      style={{
                        color: token.textSecondary,
                        display: "flex",
                        width: 9,
                      }}
                    >
                      <Svg d={n.open ? P.caretD : P.caretR} size={9} sw={2} />
                    </span>
                    <Svg d={P.folder} size={14} sw={1.4} color={n.color} />
                  </>
                ) : (
                  <>
                    <span style={{ width: 9 }} />
                    <FileIcon />
                  </>
                )}
                <span
                  style={{
                    fontSize: 12,
                    color: isSel ? token.textStrong : token.text,
                    whiteSpace: "nowrap",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                  }}
                >
                  {n.name}
                </span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
