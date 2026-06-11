#!/bin/bash
set -euo pipefail

# Only run in Claude Code on the web (remote) sessions — local machines have their own SDK.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(pwd)}"

# Install the .NET 8 SDK from the Ubuntu archive if it isn't already present.
# Idempotent: container state is cached after the hook completes, so later sessions
# skip the install. NOTE: the .NET binary CDN (builds.dotnet.microsoft.com / aka.ms)
# is blocked by the network policy, so dotnet-install.sh does not work here — but the
# Ubuntu archive ships dotnet-sdk-8.0 and is reachable.
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Installing .NET 8 SDK via apt ..."
  export DEBIAN_FRONTEND=noninteractive
  # Tolerate blocked third-party PPAs (deadsnakes/ondrej) — the Ubuntu archive,
  # which is what we need, updates fine.
  apt-get update || true
  apt-get install -y dotnet-sdk-8.0
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export DOTNET_NOLOGO=1"
  } >> "$CLAUDE_ENV_FILE"
fi

# Warm up NuGet restore so the first build in the session is fast (api.nuget.org is
# reachable). Non-fatal: a transient feed hiccup shouldn't block the session start.
dotnet restore "$PROJECT_DIR/FitWifFrens.sln" \
  || echo "warning: 'dotnet restore' did not complete; the first build will restore on demand."

echo ".NET ready: $(dotnet --version)"
