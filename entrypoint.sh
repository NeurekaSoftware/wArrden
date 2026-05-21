#!/bin/sh
set -e

if [ -n "$PUID" ] && [ "$PUID" != "0" ] && [ -n "$PGID" ] && [ "$PGID" != "0" ]; then
  userdel app 2>/dev/null || true
  groupdel app 2>/dev/null || true

  EXISTING_USER=$(getent passwd "$PUID" | cut -d: -f1)
  if [ -n "$EXISTING_USER" ] && [ "$EXISTING_USER" != "app" ]; then
    userdel "$EXISTING_USER" 2>/dev/null || true
  fi

  EXISTING_GROUP=$(getent group "$PGID" | cut -d: -f1)
  if [ -n "$EXISTING_GROUP" ] && [ "$EXISTING_GROUP" != "app" ]; then
    groupdel "$EXISTING_GROUP" 2>/dev/null || true
  fi

  groupadd -g "$PGID" app
  useradd -u "$PUID" -g app -M app
  chown -R app:app /app
  exec gosu app "$@"
fi

chown -R 0:0 /app
exec "$@"
