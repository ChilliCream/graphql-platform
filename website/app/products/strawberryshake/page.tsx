"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function StrawberryShakeRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/docs/strawberryshake/v15");
  }, [router]);

  return null;
}
