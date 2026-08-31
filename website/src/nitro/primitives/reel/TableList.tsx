import { motion, useTransform, type MotionValue } from "motion/react";
import { token } from "../../lib/tokens";
import { StrokeIcon } from "./StrokeIcon";

export interface TableRow {
  label: string;
  value: string;
  mono?: boolean;
  color?: string;
}

export interface TableListProps {
  title: string;
  rows: TableRow[];
  progress: MotionValue<number>;
  playWindow: [number, number];
  titleColor?: string;
}

export function TableList({
  title,
  rows,
  progress,
  playWindow,
  titleColor,
}: TableListProps) {
  const [w0, w1] = playWindow;
  const headerOpacity = useTransform(progress, [w0, w0 + 0.02], [0, 1], {
    clamp: true,
  });
  const span = Math.max(0.001, w1 - (w0 + 0.03));

  return (
    <div style={{ marginBottom: 14 }}>
      <motion.div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 13,
          fontWeight: 600,
          color: titleColor ?? token.textStrong,
          marginBottom: 6,
          opacity: headerOpacity,
        }}
      >
        <StrokeIcon d="M6 9l6 6 6-6" size={13} strokeWidth={1.8} />
        {title}
      </motion.div>
      <div style={{ display: "flex", flexDirection: "column" }}>
        {rows.map((r, i) => {
          const s = w0 + 0.03 + (span * i) / Math.max(1, rows.length);
          return <Row key={r.label} row={r} progress={progress} at={s} />;
        })}
      </div>
    </div>
  );
}

function Row({
  row,
  progress,
  at,
}: {
  row: TableRow;
  progress: MotionValue<number>;
  at: number;
}) {
  const opacity = useTransform(progress, [at, at + 0.03], [0, 1], {
    clamp: true,
  });
  const y = useTransform(progress, [at, at + 0.03], [4, 0], { clamp: true });
  const multiline = row.value.includes("\n");
  return (
    <motion.div
      style={{
        display: "flex",
        flexDirection: multiline ? "column" : "row",
        gap: multiline ? 4 : 8,
        padding: "5px 0",
        borderTop: `1px solid ${token.border}`,
        opacity,
        y,
      }}
    >
      <span
        style={{
          flex: multiline ? undefined : "0 0 34%",
          fontSize: 12,
          color: token.textSecondary,
        }}
      >
        {row.label}
      </span>
      <span
        style={{
          flex: 1,
          fontSize: row.mono ? 11.5 : 12,
          fontFamily: row.mono ? token.mono : token.font,
          color: row.color ?? token.text,
          whiteSpace: multiline ? "pre-wrap" : "normal",
          wordBreak: "break-word",
          lineHeight: row.mono ? "17px" : 1.4,
          ...(multiline && row.mono
            ? {
                background: token.bg,
                border: `1px solid ${token.border}`,
                borderRadius: 4,
                padding: "8px 10px",
              }
            : {}),
        }}
      >
        {row.value}
      </span>
    </motion.div>
  );
}
