#!/bin/zsh

# Find all __MISMATCH__ folders
find . -type d -name "__MISMATCH__" | while read mismatch_dir; do
  # Get the parent __snapshots__ directory
  snapshot_dir="$(dirname "$mismatch_dir")"

  # Copy all files from __MISMATCH__ to __snapshots__
  cp "$mismatch_dir"/* "$snapshot_dir/"
done

echo "Done!"
