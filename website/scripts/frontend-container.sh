#!/usr/bin/env bash
# Run website npm tooling (yarn install, yarn dev, yarn lint, ...) inside the
# same "ChilliCream Frontend" devcontainer image that VS Code users get, from
# a plain host shell that has no devcontainer CLI, node, or yarn installed.
#
# Each git checkout (the main tree, or any linked worktree) gets its own
# container and its own named volumes for node_modules and .next, so
# dependency files never land on the checkout's host disk and two checkouts
# never share a dependency tree.
#
# Usage:
#   website/scripts/frontend-container.sh up
#   website/scripts/frontend-container.sh playwright-setup
#   website/scripts/frontend-container.sh dev
#   website/scripts/frontend-container.sh exec -- <cmd...>
#   website/scripts/frontend-container.sh status
#   website/scripts/frontend-container.sh down [--purge|--all]
set -euo pipefail

IMAGE_TAG="hc-0-frontend:dev"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEBSITE_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEVCONTAINER_DIR="${WEBSITE_DIR}/../.devcontainer/frontend"

# The checkout this copy of the script lives in: the main tree, or the
# linked worktree it was invoked from. Each checkout gets its own container
# and volumes, named after a slug derived from this path, so per-worktree
# runs never share a dependency tree.
CHECKOUT="$(cd "${WEBSITE_DIR}" && git rev-parse --show-toplevel)"
SLUG="$(basename "${CHECKOUT}")-$(printf '%s' "${CHECKOUT}" | shasum | cut -c1-8)"
# The path hash in SLUG ties CONTAINER_NAME to exactly this checkout, so a
# container name collision with a different checkout cannot happen.
CONTAINER_NAME="hc-0-frontend-${SLUG}"
NODE_MODULES_VOLUME="${CONTAINER_NAME}-node_modules"
NEXT_VOLUME="${CONTAINER_NAME}-next"

# Yarn's global package cache is content-addressed, so it is safe to share
# across every checkout: one volume, fixed name, never per-checkout.
YARN_CACHE_VOLUME="hc-0-frontend-yarn-cache"

# Fixed mount point inside the container. It does not vary by checkout: each
# checkout has its own container under its own name, so the path only needs
# to be unique per-container, not per-checkout.
CONTAINER_WORKSPACE="/workspaces/hc-0"

# State for cmd_dev's INT/TERM/EXIT trap. Script-scoped (not function-local)
# so the trap body can reference them under `set -u` even on a normal return
# from cmd_dev, when the function's own locals would already be unset.
DEV_SPAWNED=0
DEV_CHILD=''

usage() {
  cat <<'EOF'
Usage: frontend-container.sh <command> [args...]

Commands:
  up                    Build the image and start this checkout's
                        container, with named volumes for
                        website/node_modules and website/.next. Publishes
                        the dev server on 127.0.0.1:3031 and Storybook on
                        127.0.0.1:6006, loopback only, unless another
                        hc-0-frontend-* container already holds those ports
                        (then it starts unpublished and prints the holder).
  playwright-setup      Install the Playwright Chromium browser (lazy, not
                        run automatically by `up`).
  dev                   Run `yarn dev` inside the container and print the
                        host URL. Exits 1 if this container has no
                        published ports (another checkout's container holds
                        them).
  exec -- <cmd...>      Run an arbitrary command inside the container, in
                        the caller's current directory translated into the
                        container mount.
  status                Show docker ps for all hc-0-frontend-* containers
                        and this checkout's three volumes (node_modules,
                        .next, and the shared yarn cache).
  down                  Stop and remove this checkout's container. Its
                        node_modules/.next volumes are kept.
  down --purge          Also remove this checkout's node_modules/.next
                        volumes.
  down --all            Stop and remove every hc-0-frontend-* container
                        (all checkouts). Volumes are kept.
EOF
}

require_docker() {
  if ! docker info >/dev/null 2>&1; then
    echo "error: the Docker daemon is not reachable (checked with \`docker info\`)." >&2
    echo "Start Docker (OrbStack) and try again. There is no host fallback." >&2
    exit 1
  fi
}

# Translate a host path under this checkout into the equivalent path inside
# the container mount. A path outside the website directory (e.g. the
# checkout root) falls back to the website directory itself, since every
# command this wrapper runs (yarn install/dev/lint/format) needs to run from
# website/ regardless of where the wrapper was invoked from.
translate_cwd() {
  local host_path="$1" base rel
  case "${host_path}" in
    "${WEBSITE_DIR}" | "${WEBSITE_DIR}"/*) base="${host_path}" ;;
    *) base="${WEBSITE_DIR}" ;;
  esac
  rel="${base#"${CHECKOUT}"}"
  printf '%s%s' "${CONTAINER_WORKSPACE}" "${rel}"
}

# The container path for *this script's* website directory (the main tree's
# website/ or a worktree's website/, whichever copy of the script is
# running). Reusing translate_cwd keeps this in sync with a single source of
# truth for the mount layout.
container_website_dir() {
  translate_cwd "${WEBSITE_DIR}"
}

container_running() {
  [ -n "$(docker ps --filter "name=^/${CONTAINER_NAME}$" --filter status=running -q)" ]
}

container_exists() {
  [ -n "$(docker ps -a --filter "name=^/${CONTAINER_NAME}$" -q)" ]
}

# Prints the name of a running hc-0-frontend-* container that currently
# publishes host port 3031, or nothing if none does. Used both to decide
# whether `up` can publish ports and to tell `dev` who is holding them.
container_port_holder_name() {
  docker ps --filter "name=^/hc-0-frontend-" --filter "publish=3031" --format '{{.Names}}' | head -n1
}

ensure_running() {
  require_docker
  if ! container_running; then
    echo "error: ${CONTAINER_NAME} is not running. Run \`frontend-container.sh up\` first." >&2
    exit 1
  fi
}

# Starts this checkout's container. "$@" are extra `docker run` flags (the
# port publishes), appended before the image tag; passing none starts the
# container without published ports.
#
# Only what the website tooling actually reads is bound in, each at its own
# destination, so CONTAINER_WORKSPACE itself is never a bind-mount
# destination: website/ (read-write, the one bind every command needs),
# the file: dependency on ../src/Mocha/src/mocha-visualizer (read-only),
# root .editorconfig (read-only, read by Prettier), and .git (read-only,
# for repo metadata). A compromised dependency under website/ therefore
# cannot reach src/, templates/, global.json, nuget.config, .github/ or the
# root docs, and nothing mounts .claude at all, so the container can never
# read or write the host's Claude Code configuration.
#
# The checkout's .git is mounted read-only so code running in the container
# cannot plant host-executed commands (git hooks, core.hooksPath/
# core.fsmonitor, filter drivers in .git/config).
run_container() {
  local mount_flags=(
    -v "${CHECKOUT}/website:${CONTAINER_WORKSPACE}/website"
    -v "${CHECKOUT}/src/Mocha/src/mocha-visualizer:${CONTAINER_WORKSPACE}/src/Mocha/src/mocha-visualizer:ro"
    -v "${CHECKOUT}/.editorconfig:${CONTAINER_WORKSPACE}/.editorconfig:ro"
    -v "${CHECKOUT}/.git:${CONTAINER_WORKSPACE}/.git:ro"
    -v "${NODE_MODULES_VOLUME}:${CONTAINER_WORKSPACE}/website/node_modules"
    -v "${NEXT_VOLUME}:${CONTAINER_WORKSPACE}/website/.next"
    -v "${YARN_CACHE_VOLUME}:/home/node/.yarn/berry/cache"
  )

  docker run -d \
    --name "${CONTAINER_NAME}" \
    --shm-size=512m \
    -e CHILLICREAM_FRONTEND_ENV=devcontainer \
    -e NEXT_TELEMETRY_DISABLED=1 \
    --user node \
    "${mount_flags[@]}" \
    -w "$(container_website_dir)" \
    "$@" \
    "${IMAGE_TAG}" \
    sleep infinity >/dev/null
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

    echo "==> Starting ${CONTAINER_NAME} (mounting ${CHECKOUT} at ${CONTAINER_WORKSPACE})"
    local holder
    holder="$(container_port_holder_name)"
    if [ -n "${holder}" ]; then
      echo "==> port 3031 is already published by ${holder}; starting ${CONTAINER_NAME} without published ports" >&2
      run_container
    else
      run_container -p 127.0.0.1:3031:3001 -p 127.0.0.1:6006:6006
    fi

    echo "==> Fixing ownership of the node_modules, .next, and yarn cache volumes"
    # Mounting a volume below /home/node/.yarn/berry/cache creates its
    # parent directories too, /home/node/.yarn and /home/node/.yarn/berry,
    # which Docker materializes as root:root since neither exists in the
    # image. Yarn also writes a sibling of cache under berry/ (its
    # immutable-cache index), so both parents need to be node-owned too,
    # not just the cache volume's own root.
    docker exec --user root "${CONTAINER_NAME}" chown node:node \
      "${CONTAINER_WORKSPACE}/website/node_modules" \
      "${CONTAINER_WORKSPACE}/website/.next" \
      /home/node/.yarn \
      /home/node/.yarn/berry \
      /home/node/.yarn/berry/cache
  fi

  echo "==> Running yarn install --immutable inside the container"
  docker exec -w "$(container_website_dir)" "${CONTAINER_NAME}" yarn install --immutable
}

cmd_playwright_setup() {
  ensure_running
  docker exec -w "$(container_website_dir)" "${CONTAINER_NAME}" yarn playwright install chromium
}

# Populates the global TTY_FLAGS array with the `docker exec` flags for the
# current stdio: always interactive, plus a tty when both stdin and stdout
# are terminals (so an interactive Ctrl+C reaches the exec'd process
# directly). Shared by cmd_exec and cmd_dev.
compute_tty_flags() {
  TTY_FLAGS=(-i)
  if [ -t 0 ] && [ -t 1 ]; then
    TTY_FLAGS+=(-t)
  fi
}

stop_dev() {
  docker exec "${CONTAINER_NAME}" pkill -f 'next dev' >/dev/null 2>&1 || true
}

cmd_dev() {
  # DEV_SPAWNED/DEV_CHILD are set only once the background `docker exec`
  # below is actually started. The traps are installed here, before that
  # spawn (and before the "already running" check can exit early), so a
  # signal in that gap has nothing to clean up: the guard keeps the early
  # exit path from killing a dev server this invocation did not start.
  trap '
    if [ "${DEV_SPAWNED:-0}" -eq 1 ]; then
      kill "${DEV_CHILD:-}" 2>/dev/null || true
      stop_dev
      DEV_SPAWNED=0
      DEV_CHILD=''
    fi
  ' INT TERM EXIT

  ensure_running

  local holder
  holder="$(container_port_holder_name)"
  if [ "${holder}" != "${CONTAINER_NAME}" ]; then
    if [ -n "${holder}" ]; then
      echo "error: ${CONTAINER_NAME} has no published ports; port 3031 is held by ${holder}. Run \`frontend-container.sh down\` there, then \`frontend-container.sh up\` here again." >&2
    else
      echo "error: ${CONTAINER_NAME} has no published ports. Run \`frontend-container.sh down\` then \`frontend-container.sh up\` again here to reclaim port 3031." >&2
    fi
    exit 1
  fi

  if docker exec "${CONTAINER_NAME}" pgrep -f 'next dev' >/dev/null 2>&1; then
    echo "error: a dev server is already running in ${CONTAINER_NAME}; run \`frontend-container.sh down\` or \`frontend-container.sh exec -- pkill -f \"next dev\"\` first" >&2
    exit 1
  fi

  echo "==> Dev server: http://localhost:3031"

  # This exec is backgrounded for signal cleanup, so it must not attach a TTY.
  docker exec -w "$(container_website_dir)" "${CONTAINER_NAME}" yarn dev &
  DEV_CHILD=$!
  DEV_SPAWNED=1

  wait "${DEV_CHILD}"
}

cmd_exec() {
  if [ "$#" -eq 0 ]; then
    echo "Usage: frontend-container.sh exec -- <cmd...>" >&2
    exit 2
  fi
  ensure_running

  compute_tty_flags
  local tty_flags=("${TTY_FLAGS[@]}")

  local container_cwd
  container_cwd="$(translate_cwd "$(pwd)")"
  docker exec "${tty_flags[@]}" -w "${container_cwd}" "${CONTAINER_NAME}" "$@"
}

cmd_status() {
  require_docker
  # hc-0-frontend (no trailing dash) is the retired single-container name
  # from before per-checkout containers (hc-0-rou); included as a one-time
  # migration so a leftover from that version is still visible.
  docker ps -a --filter "name=^/hc-0-frontend-" --filter "name=^/hc-0-frontend$"
  echo
  echo "Volumes for ${CHECKOUT}:"
  docker volume ls --filter "name=${NODE_MODULES_VOLUME}" --filter "name=${NEXT_VOLUME}" --filter "name=${YARN_CACHE_VOLUME}"
}

# Removes one volume if it exists, leaving a volume that was never created
# (already purged, or never populated) as a no-op rather than an error.
# A `docker volume rm` that fails on a volume which does exist (still
# attached to a container, say) is left to fail loudly: its stderr reaches
# the caller and `set -e` stops the script with that non-zero exit status.
purge_volume() {
  local vol="$1"
  if [ -n "$(docker volume ls -q --filter "name=^${vol}$")" ]; then
    docker volume rm "${vol}"
  fi
}

cmd_down() {
  # Argument shape is checked before require_docker, so a bad invocation
  # (wrong arg count, unknown option) exits 2 with usage even when the
  # Docker daemon is unreachable.
  if [ "$#" -gt 1 ]; then
    echo "error: 'down' takes at most one argument (--purge or --all)" >&2
    usage >&2
    exit 2
  fi

  case "${1:-}" in
    "" | --purge | --all) : ;;
    *)
      echo "error: unknown down option '$1'" >&2
      usage >&2
      exit 2
      ;;
  esac

  require_docker

  case "${1:-}" in
    "")
      docker rm -f "${CONTAINER_NAME}" >/dev/null 2>&1 || true
      echo "==> ${CONTAINER_NAME} stopped and removed"
      ;;
    --purge)
      docker rm -f "${CONTAINER_NAME}" >/dev/null 2>&1 || true
      purge_volume "${NODE_MODULES_VOLUME}"
      purge_volume "${NEXT_VOLUME}"
      echo "==> ${CONTAINER_NAME} stopped and removed, along with its node_modules and .next volumes"
      ;;
    --all)
      local name
      # hc-0-frontend (no trailing dash) is the retired single-container
      # name from before per-checkout containers (hc-0-rou); included as a
      # one-time migration so a leftover from that version is removed too.
      for name in $(docker ps -a --filter "name=^/hc-0-frontend-" --filter "name=^/hc-0-frontend$" --format '{{.Names}}'); do
        docker rm -f "${name}" >/dev/null 2>&1 || true
        echo "==> ${name} stopped and removed"
      done
      ;;
  esac
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
