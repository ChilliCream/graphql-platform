"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function HotChocolateRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/docs/hotchocolate/v15");
  }, [router]);

  return null;
}
