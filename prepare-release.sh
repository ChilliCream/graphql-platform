#!/usr/bin/env bash

# Prepares the downstream repositories for the first release of a new minor or major version.
#
# Downstream repositories keep one release/vX.Y branch (a lane) per platform minor, and their
# default branch is the newest lane. For the lane of the given version, this script:
#
#   - creates the lane from the tip of the highest lower lane where it is missing, and
#   - makes the lane the default branch where it is the newest lane of the repository.
#
# Run it before tagging the first version of a new minor or major. Patch releases and later
# previews of a minor change nothing. The script uses the gh CLI with your login, needs admin
# rights on the repositories whose default branch it changes, and does not create tags.
#
# Usage: ./prepare-release.sh <version> [--dry-run]

set -euo pipefail

owner=ChilliCream

# Built and published by the lane's own workflows for every release.
commercial_repo=graphql-platform-commercial

# Released for every version. Their release workflow runs from the default branch.
action_repos=(
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

# Released for stable versions only.
stable_only_repos=(
    nitro-azure-pipelines-tasks
)

usage() {
    echo "Usage: ./prepare-release.sh <version> [--dry-run]" >&2
    exit 2
}

version=""
dry_run=false
for arg in "$@"; do
    case "$arg" in
        --dry-run) dry_run=true ;;
        -*) usage ;;
        *)
            if [[ -n "$version" ]]; then
                usage
            fi
            version="$arg"
            ;;
    esac
done
if [[ -z "$version" ]]; then
    usage
fi

version_re='^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?$'
if [[ ! "$version" =~ $version_re ]]; then
    echo "'$version' is not a version (X.Y.Z or X.Y.Z-suffix)." >&2
    exit 2
fi
major="${BASH_REMATCH[1]}"
minor="${BASH_REMATCH[2]}"
suffix="${BASH_REMATCH[4]:-}"

lane="release/v$major.$minor"
lane_key=$(( major * 100000 + minor ))
lane_ref_re='^refs/heads/release/v([0-9]+)\.([0-9]+)$'

if ! command -v gh > /dev/null; then
    echo "The gh CLI is required: https://cli.github.com" >&2
    exit 1
fi
if ! login=$(gh api user --jq .login); then
    echo "The gh CLI is not logged in. Run: gh auth login" >&2
    exit 1
fi

echo "Version $version uses lane $lane. Checking as $login."
echo

problems=()
actions=()

problem() {
    problems+=("$1: $2")
    echo "  problem: $2"
}

# Decides what one repository needs and records it in actions. Nothing is changed here, so
# every repository is checked before the first change is made.
plan_repo() {
    local repo="$1"
    local info default can_push can_admin refs ref sha key
    local lane_sha="" lower_key=-1 lower_lane="" lower_sha="" newest_key=-1
    local create=false make_default=false

    echo "$owner/$repo"

    if ! info=$(gh api "repos/$owner/$repo" \
        --jq '[.default_branch, (.permissions.push // false), (.permissions.admin // false)] | @tsv'); then
        problem "$repo" "the repository could not be read"
        return
    fi
    IFS=$'\t' read -r default can_push can_admin <<< "$info"

    if ! refs=$(gh api --paginate "repos/$owner/$repo/git/matching-refs/heads/release/v" \
        --jq '.[] | [.ref, .object.sha] | @tsv'); then
        problem "$repo" "its branches could not be read"
        return
    fi

    while IFS=$'\t' read -r ref sha; do
        if [[ ! "$ref" =~ $lane_ref_re ]]; then
            continue
        fi
        key=$(( 10#${BASH_REMATCH[1]} * 100000 + 10#${BASH_REMATCH[2]} ))
        if (( key > newest_key )); then
            newest_key=$key
        fi
        if (( key == lane_key )); then
            lane_sha="$sha"
        elif (( key < lane_key && key > lower_key )); then
            lower_key=$key
            lower_lane="${ref#refs/heads/}"
            lower_sha="$sha"
        fi
    done <<< "$refs"

    if (( newest_key < 0 )); then
        problem "$repo" "it has no release/vX.Y branch to start from"
        return
    fi

    if [[ -z "$lane_sha" ]]; then
        if (( lower_key < 0 )); then
            echo "  skipped: $lane is older than every lane, so this repository does not serve it"
            return
        fi
        if (( newest_key > lane_key )); then
            problem "$repo" "$lane is missing although a newer lane exists; create it by hand"
            return
        fi
        create=true
        if [[ "$can_push" != "true" ]]; then
            problem "$repo" "you need write access to create $lane"
        fi
    fi

    # Only the newest lane becomes the default branch. A release of an older minor leaves it alone.
    if (( lane_key >= newest_key )) && [[ "$default" != "$lane" ]]; then
        make_default=true
        if [[ "$can_admin" != "true" ]]; then
            problem "$repo" "you need admin rights to make $lane the default branch"
        fi
    fi

    if [[ "$create" == "true" ]]; then
        actions+=("create|$repo|$lower_lane|$lower_sha")
        echo "  create $lane from $lower_lane (${lower_sha:0:7})"
    fi
    if [[ "$make_default" == "true" ]]; then
        actions+=("default|$repo|$default|")
        echo "  default branch: $default -> $lane"
    fi
    if [[ "$create" != "true" && "$make_default" != "true" ]]; then
        echo "  nothing to do"
    fi
}

plan_repo "$commercial_repo"
for repo in "${action_repos[@]}"; do
    plan_repo "$repo"
done
for repo in "${stable_only_repos[@]}"; do
    if [[ -z "$suffix" ]]; then
        plan_repo "$repo"
    else
        echo "$owner/$repo"
        echo "  skipped: released for stable versions only"
    fi
done
echo

if (( ${#problems[@]} > 0 )); then
    echo "Nothing was changed. Resolve these problems first:" >&2
    for entry in "${problems[@]}"; do
        echo "  $owner/$entry" >&2
    done
    exit 1
fi

if (( ${#actions[@]} == 0 )); then
    echo "Everything is ready for $version."
    exit 0
fi

if [[ "$dry_run" == "true" ]]; then
    echo "Dry run: nothing was changed."
    exit 0
fi

for action in "${actions[@]}"; do
    IFS='|' read -r kind repo from sha <<< "$action"
    case "$kind" in
        create)
            if ! gh api "repos/$owner/$repo/git/refs" --silent \
                -f ref="refs/heads/$lane" -f sha="$sha"; then
                echo "Stopped at $owner/$repo. Fix the problem and run the script again." >&2
                exit 1
            fi
            echo "$owner/$repo: created $lane from $from"
            ;;
        default)
            if ! gh api --method PATCH "repos/$owner/$repo" --silent \
                -f default_branch="$lane"; then
                echo "Stopped at $owner/$repo. Fix the problem and run the script again." >&2
                exit 1
            fi
            echo "$owner/$repo: default branch is now $lane (was $from)"
            ;;
    esac
done

echo
echo "Everything is ready for $version."
