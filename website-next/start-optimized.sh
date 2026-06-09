#!/usr/bin/env bash
# Recreate the release workflow locally: optimize images, generate git
# metadata, build the static site, build the Docker image and run it in a
# throwaway container on a random free port. Prints a clickable link to open
# the page. Ctrl+C (or any exit) stops and removes the container.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

IMAGE_TAG="website-next-local:ci"
CONTAINER_NAME="website-next-local-$$"

cleanup() {
  echo ""
  echo "==> Stopping and removing container ${CONTAINER_NAME}"
  docker rm -f "${CONTAINER_NAME}" >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

echo "==> Optimizing images"
yarn optimize-images

echo "==> Generating git metadata"
yarn generate-git-metadata

echo "==> Building website (next build)"
yarn build

echo "==> Building Docker image ${IMAGE_TAG}"
docker build -f Dockerfile -t "${IMAGE_TAG}" .

echo "==> Starting container on a random port"
# Publish container port 80 to an ephemeral host port bound to localhost.
docker run -d --name "${CONTAINER_NAME}" -p 127.0.0.1::80 "${IMAGE_TAG}" >/dev/null

# Resolve the host port Docker assigned (format: 127.0.0.1:PORT).
HOST_PORT="$(docker port "${CONTAINER_NAME}" 80 | head -n1 | sed 's/.*://')"
if [ -z "${HOST_PORT}" ]; then
  echo "Failed to determine the published host port." >&2
  exit 1
fi

URL="http://localhost:${HOST_PORT}"
echo ""
echo "======================================================================"
echo "  Website is running at: ${URL}"
echo "  (Cmd/Ctrl+Click the link above to open it)"
echo "  Press Ctrl+C to stop and remove the container."
echo "======================================================================"
echo ""

# Stream container logs in the foreground so the script stays alive until the
# user hits Ctrl+C, at which point the EXIT trap cleans up the container.
docker logs -f "${CONTAINER_NAME}"
