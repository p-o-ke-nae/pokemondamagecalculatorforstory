#!/bin/sh

set -eu

load_secret() {
  file_path="$1"
  env_name="$2"

  if [ -n "$(printenv "$env_name" 2>/dev/null || true)" ]; then
    return
  fi

  if [ -f "$file_path" ]; then
    export "$env_name=$(tr -d '\n\r' < "$file_path")"
  fi
}

load_secret /run/secrets/nextauth_secret NEXTAUTH_SECRET
load_secret /run/secrets/google_client_id GOOGLE_CLIENT_ID
load_secret /run/secrets/google_client_secret GOOGLE_CLIENT_SECRET

exec "$@"
