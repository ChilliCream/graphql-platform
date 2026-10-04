#!/usr/bin/env bash
# Commits are signed with an SSH key (gpg.format=ssh). The dev container inherits
# the host's ~/.gitconfig and a forwarded SSH agent, but no key files, so the
# public key that user.signingKey points at does not exist here and every commit
# fails. Recreate it from the agent; the private key never leaves the host.
set -euo pipefail

[[ "$(git config --get gpg.format || true)" == "ssh" ]] || exit 0

key_path="$(git config --get user.signingKey || true)"
case "${key_path}" in
  key::*) exit 0 ;;  # key material is inline, no file needed
  "") key_path="${HOME}/.ssh/id_ed25519.pub" ;;
  "~/"*) key_path="${HOME}/${key_path#\~/}" ;;
esac

if [[ -s "${key_path}" ]]; then
  exit 0
fi

key="$(ssh-add -L 2>/dev/null | grep -m1 '^ssh-ed25519 ' || true)"
if [[ -z "${key}" ]]; then
  echo "git signing: no ed25519 key in the SSH agent, ${key_path} not written." >&2
  echo "git signing: check that the host agent has your key and is forwarded." >&2
  exit 0
fi

mkdir -p "$(dirname "${key_path}")"
echo "${key}" > "${key_path}"
echo "git signing: wrote ${key_path} from the forwarded SSH agent."
