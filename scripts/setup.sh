#!/bin/sh
# One-time developer setup: enables repository git hooks and restores packages.
set -e
cd "$(dirname "$0")/.."

git config core.hooksPath .githooks
chmod +x .githooks/*
echo "Git hooks enabled (.githooks)."

dotnet restore
echo "Setup complete."
