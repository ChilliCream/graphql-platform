#!/bin/zsh

# Use a variable to keep track of whether any directories were found
found_mismatch=false

# Use a variable to check if the header has been printed
header_printed=false

# Find all __MISMATCH__ folders
find . -type d -name "__MISMATCH__" | while read mismatch_dir; do
  # Mark that we found a mismatch directory
  found_mismatch=true

  # Get the parent __snapshots__ directory
  snapshot_dir="$(dirname "$mismatch_dir")"

  # If there are files in the directory, copy them
  if [ "$(ls -A "$mismatch_dir")" ]; then
    for file in "$mismatch_dir"/*; do
      if [ -f "$file" ]; then
        cp "$file" "$snapshot_dir/"

        # Print the header only once
        if [ "$header_printed" = false ]; then
          echo "Found Updated Snapshots:"
          header_printed=true
        fi

        # Extract relevant path details and print
        relative_path="${snapshot_dir#./src/HotChocolate/}" # strip the common prefix
        echo "- $relative_path/$(basename "$file")"
      fi
    done
  fi

  # Remove the __MISMATCH__ directory
  rm -r "$mismatch_dir"
done

# Check if any directories were found
if [ "$found_mismatch" = false ]; then
  echo "All snapshots are up to date!"
else
  echo "\nDone!"
fi
