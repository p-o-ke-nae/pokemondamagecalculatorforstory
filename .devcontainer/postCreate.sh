#!/usr/bin/env bash
set -euo pipefail

workspace_folder="${1:-$(pwd)}"

git config --global --add safe.directory "${workspace_folder}"
git config --global core.autocrlf input

cat <<'EOF'
[INFO] Dev Container is ready.
[INFO] Open the integrated terminal in this container and start the GitHub Copilot CLI agent from the container workspace.
EOF
