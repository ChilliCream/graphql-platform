#!/usr/bin/env bash

# Prepare the downstream repositories for the first release of a new minor or major version:
# create the release/vX.Y branch where it is missing and make it the default branch where it
# is the newest one. Uses the gh CLI with your login.

set -euo pipefail

if (( $# != 1 )); then
    echo "Usage: ./prepare-release.sh <version>" >&2
    exit 2
fi

version="$1"
IFS=. read -r major minor _ <<< "$version"
lane="release/v$major.$minor"
lane_key=$(( major * 1000 + minor ))

repos=(
    graphql-platform-commercial
    nitro-client-publish
    nitro-client-upload
    nitro-client-validate
    nitro-fusion-publish
    nitro-fusion-upload
    nitro-fusion-validate
    nitro-mcp-publish
    nitro-mcp-upload
    nitro-mcp-validate
    nitro-openapi-publish
    nitro-openapi-upload
    nitro-openapi-validate
    nitro-schema-download
    nitro-schema-publish
    nitro-schema-upload
    nitro-schema-validate
)

# nitro-azure-pipelines-tasks is released for stable versions only.
if [[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    repos+=(nitro-azure-pipelines-tasks)
fi

for name in "${repos[@]}"; do
    repo="ChilliCream/$name"
    lanes=$(gh api --paginate "repos/$repo/git/matching-refs/heads/release/v" \
        --jq '.[] | select(.ref | test("/v[0-9]+\\.[0-9]+$")) | "\(.ref | ltrimstr("refs/heads/release/v")) \(.object.sha)"')

    exists=false
    newest_key=$lane_key
    base_key=-1
    base_sha=""
    while read -r lane_version sha; do
        if [[ -z "$lane_version" ]]; then
            continue
        fi
        IFS=. read -r lane_major lane_minor <<< "$lane_version"
        key=$(( lane_major * 1000 + lane_minor ))
        if (( key > newest_key )); then
            newest_key=$key
        fi
        if (( key == lane_key )); then
            exists=true
        elif (( key < lane_key && key > base_key )); then
            base_key=$key
            base_sha="$sha"
        fi
    done <<< "$lanes"

    if [[ "$exists" == "false" && -n "$base_sha" ]]; then
        echo "$repo: creating $lane"
        gh api "repos/$repo/git/refs" --silent -f ref="refs/heads/$lane" -f sha="$base_sha"
        exists=true
    fi

    if [[ "$exists" == "true" ]] && (( newest_key == lane_key )); then
        default_branch=$(gh api "repos/$repo" --jq .default_branch)
        if [[ "$default_branch" != "$lane" ]]; then
            echo "$repo: making $lane the default branch"
            gh api --method PATCH "repos/$repo" --silent -f default_branch="$lane"
        fi
    fi
done
