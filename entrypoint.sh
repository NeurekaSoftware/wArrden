#!/bin/sh
set -e

if [ -n "$PUID" ] && [ "$PUID" != "0" ] && [ -n "$PGID" ] && [ "$PGID" != "0" ]; then
  deluser app 2>/dev/null || true
  delgroup app 2>/dev/null || true

  EXISTING_USER=$(getent passwd "$PUID" | cut -d: -f1)
  if [ -n "$EXISTING_USER" ] && [ "$EXISTING_USER" != "app" ]; then
    deluser "$EXISTING_USER" 2>/dev/null || true
  fi

  EXISTING_GROUP=$(getent group "$PGID" | cut -d: -f1)
  if [ -n "$EXISTING_GROUP" ] && [ "$EXISTING_GROUP" != "app" ]; then
    delgroup "$EXISTING_GROUP" 2>/dev/null || true
  fi

  addgroup -g "$PGID" app
  adduser -S -u "$PUID" -G app app
  chown -R app:app /app
  exec su-exec app "$@"
fi

chown -R 0:0 /app
exec "$@"
