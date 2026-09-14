#!/usr/bin/env bash
# Run website npm tooling (yarn install, yarn dev, yarn lint, ...) inside the
# same "ChilliCream Frontend" devcontainer image that VS Code users get, from
# a plain host shell that has no devcontainer CLI, node, or yarn installed.
#
# Usage:
#   website/scripts/frontend-container.sh up
#   website/scripts/frontend-container.sh playwright-setup
#   website/scripts/frontend-container.sh dev
#   website/scripts/frontend-container.sh exec -- <cmd...>
#   website/scripts/frontend-container.sh logs
#   website/scripts/frontend-container.sh status
#   website/scripts/frontend-container.sh down
set -euo pipefail

IMAGE_TAG="hc-0-frontend:dev"
CONTAINER_NAME="hc-0-frontend"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEBSITE_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEVCONTAINER_DIR="${WEBSITE_DIR}/../.devcontainer/frontend"

usage() {
  cat <<'EOF'
Usage: frontend-container.sh <command> [args...]

Commands:
  up                    Build the image and start the long-lived container.
                        Publishes the dev server on 127.0.0.1:3031 and
                        Storybook on 127.0.0.1:6006. Loopback only.
  playwright-setup      Install the Playwright Chromium browser (lazy, not
                        run automatically by `up`).
  dev                   Run `yarn dev` inside the container and print the
                        host URL.
  exec -- <cmd...>      Run an arbitrary command inside the container, in
                        the caller's current directory translated into the
                        container mount.
  logs                  Follow the container's logs.
  status                Show the container's docker ps entry.
  down                  Stop and remove the container.
EOF
}

require_docker() {
  if ! docker info >/dev/null 2>&1; then
    echo "error: the Docker daemon is not reachable (checked with \`docker info\`)." >&2
    echo "Start Docker (OrbStack) and try again. There is no host fallback." >&2
    exit 1
  fi
}

# The repository root to bind-mount. Using the common git dir's parent
# (rather than --show-toplevel) means this also resolves correctly when the
# script is invoked from a linked worktree that lives under the repo root:
# the common dir is always the main repo's `.git`, so its parent is the one
# true repo root regardless of which worktree we are standing in.
repo_root() {
  local common_dir
  common_dir="$(cd "${WEBSITE_DIR}" && git rev-parse --path-format=absolute --git-common-dir)"
  dirname "${common_dir}"
}

container_root() {
  printf '/workspaces/%s' "$(basename "$(repo_root)")"
}

# Translate a host path under the repo root into the equivalent path inside
# the container mount. A path outside the website directory (e.g. the repo
# root or a worktree root) falls back to the website directory itself, since
# every command this wrapper runs (yarn install/dev/lint/format) needs to run
# from website/ regardless of where the wrapper was invoked from.
translate_cwd() {
  local host_path="$1" root container base rel
  root="$(repo_root)"
  container="$(container_root)"
  case "${host_path}" in
    "${WEBSITE_DIR}" | "${WEBSITE_DIR}"/*) base="${host_path}" ;;
    *) base="${WEBSITE_DIR}" ;;
  esac
  rel="${base#"${root}"}"
  printf '%s%s' "${container}" "${rel}"
}

# The container path for *this script's* website directory (the main tree's
# website/ or a worktree's website/, whichever copy of the script is
# running). Reusing translate_cwd keeps this in sync with a single source of
# truth for the mount layout instead of assuming repo_root's child is always
# named "website".
container_website_dir() {
  translate_cwd "${WEBSITE_DIR}"
}

container_running() {
  [ -n "$(docker ps --filter "name=^/${CONTAINER_NAME}$" --filter status=running -q)" ]
}

container_exists() {
  [ -n "$(docker ps -a --filter "name=^/${CONTAINER_NAME}$" -q)" ]
}

ensure_running() {
  require_docker
  if ! container_running; then
    echo "error: ${CONTAINER_NAME} is not running. Run \`frontend-container.sh up\` first." >&2
    exit 1
  fi
}

cmd_up() {
  if [ "$#" -gt 0 ]; then
    echo "error: 'up' takes no arguments" >&2
    exit 2
  fi

  require_docker

  echo "==> Building ${IMAGE_TAG} from ${DEVCONTAINER_DIR}"
  docker build -t "${IMAGE_TAG}" -f "${DEVCONTAINER_DIR}/dockerfile" "${DEVCONTAINER_DIR}"

  if container_running; then
    echo "==> ${CONTAINER_NAME} is already running, reusing it"
  else
    if container_exists; then
      echo "==> Removing stopped ${CONTAINER_NAME} container"
      docker rm -f "${CONTAINER_NAME}" >/dev/null
    fi

    local root workspace
    root="$(repo_root)"
    workspace="$(container_root)"

    echo "==> Starting ${CONTAINER_NAME} (mounting ${root} at ${workspace})"
    docker run -d \
      --name "${CONTAINER_NAME}" \
      --shm-size=512m \
      -e CHILLICREAM_FRONTEND_ENV=devcontainer \
      -e NEXT_TELEMETRY_DISABLED=1 \
      --user node \
      -v "${root}:${workspace}" \
      -w "$(container_website_dir)" \
      -p 127.0.0.1:3031:3001 \
      -p 127.0.0.1:6006:6006 \
      "${IMAGE_TAG}" \
      sleep infinity >/dev/null
  fi

  echo "==> Running yarn install --immutable inside the container"
  docker exec -w "$(container_website_dir)" "${CONTAINER_NAME}" yarn install --immutable
}

cmd_playwright_setup() {
  ensure_running
  docker exec -w "$(container_website_dir)" "${CONTAINER_NAME}" yarn playwright install chromium
}

cmd_dev() {
  ensure_running
  echo "==> Dev server: http://localhost:3031"
  docker exec -w "$(container_website_dir)" "${CONTAINER_NAME}" yarn dev
}

cmd_exec() {
  if [ "$#" -eq 0 ]; then
    echo "Usage: frontend-container.sh exec -- <cmd...>" >&2
    exit 2
  fi
  ensure_running

  local tty_flags=(-i)
  if [ -t 0 ] && [ -t 1 ]; then
    tty_flags+=(-t)
  fi

  local container_cwd
  container_cwd="$(translate_cwd "$(pwd)")"
  docker exec "${tty_flags[@]}" -w "${container_cwd}" "${CONTAINER_NAME}" "$@"
}

cmd_logs() {
  require_docker
  docker logs -f "${CONTAINER_NAME}"
}

cmd_status() {
  require_docker
  docker ps -a --filter "name=^/${CONTAINER_NAME}$"
}

cmd_down() {
  require_docker
  docker rm -f "${CONTAINER_NAME}" >/dev/null 2>&1 || true
  echo "==> ${CONTAINER_NAME} stopped and removed"
}

main() {
  if [ "$#" -eq 0 ]; then
    usage
    exit 2
  fi

  local command="$1"
  shift

  case "${command}" in
    up) cmd_up "$@" ;;
    playwright-setup) cmd_playwright_setup "$@" ;;
    dev) cmd_dev "$@" ;;
    exec)
      if [ "${1:-}" = "--" ]; then
        shift
      fi
      cmd_exec "$@"
      ;;
    logs) cmd_logs "$@" ;;
    status) cmd_status "$@" ;;
    down) cmd_down "$@" ;;
    -h | --help | help)
      usage
      ;;
    *)
      echo "error: unknown command '${command}'" >&2
      usage
      exit 2
      ;;
  esac
}

main "$@"
