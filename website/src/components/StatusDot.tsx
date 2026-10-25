export type HealthStatus = "healthy" | "warning" | "error";

export const HEALTH_COLOR: Record<HealthStatus, string> = {
  healthy: "var(--color-cc-success)",
  warning: "var(--color-cc-warning)",
  error: "#f0786a",
};

interface StatusDotProps {
  readonly status: HealthStatus;
  readonly pulse?: boolean;
}

export function StatusDot({ status, pulse = false }: StatusDotProps) {
  const color = HEALTH_COLOR[status];
  return (
    <span className="relative inline-flex h-2 w-2 shrink-0">
      {pulse && (
        <span
          className="absolute inline-flex h-full w-full rounded-full opacity-60 motion-safe:animate-ping"
          style={{ backgroundColor: color }}
          aria-hidden
        />
      )}
      <span
        className="relative inline-flex h-2 w-2 rounded-full"
        style={{
          backgroundColor: color,
          boxShadow: `0 0 8px color-mix(in srgb, ${color} 66.67%, transparent)`,
        }}
      />
    </span>
  );
}
