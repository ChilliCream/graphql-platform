#!/bin/bash

echo "Marking all unshipped APIs as shipped..."

find ./src -name "PublicAPI.Unshipped.txt" -exec sh -c 'echo "$1"; basedir="$(dirname "$1")"; cp "$1" "$basedir/PublicAPI.Shipped.txt"' _ {} \;

echo "Marked all unshipped APIs as shipped!"