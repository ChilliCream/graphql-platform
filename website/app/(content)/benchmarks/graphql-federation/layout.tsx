import type { ReactNode } from "react";
import { BenchmarkPreferences } from "@/src/components/benchmarks/BenchmarkPreferences";

interface BenchmarkLayoutProps {
  readonly children: ReactNode;
}

export default function BenchmarkLayout({ children }: BenchmarkLayoutProps) {
  return <BenchmarkPreferences>{children}</BenchmarkPreferences>;
}
