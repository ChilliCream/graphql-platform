"use client";

import { useEffect } from "react";

interface GlobalErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function GlobalError({ error, reset }: GlobalErrorProps) {
  useEffect(() => {
    // A ChunkLoadError means the build that rendered this tab's HTML no
    // longer exists on the server (another deploy shipped newer hashes).
    // A full reload fetches the current HTML and its matching chunks.
    if (isChunkLoadError(error)) {
      window.location.reload();
    }
  }, [error]);

  return (
    <html>
      <body>
        <p>
          Something went wrong while loading chillicream.com. Please reload the
          page.
        </p>
        <button type="button" onClick={reset}>
          Try again
        </button>
      </body>
    </html>
  );
}

function isChunkLoadError(error: Error): boolean {
  return (
    error.name === "ChunkLoadError" ||
    /Loading chunk \d+ failed/i.test(error.message)
  );
}
