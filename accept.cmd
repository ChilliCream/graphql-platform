:; set -eo pipefail
:; SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
:; ${SCRIPT_DIR}/accept.sh "$@"
:; exit $?

./build.cmd accept
