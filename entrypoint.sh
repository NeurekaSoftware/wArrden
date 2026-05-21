#!/bin/sh
set -e

if [ -n "$PUID" ] && [ "$PUID" != "0" ] && [ -n "$PGID" ] && [ "$PGID" != "0" ]; then
  groupdel app 2>/dev/null || true
  groupadd -g "$PGID" app
  userdel app 2>/dev/null || true
  useradd -u "$PUID" -g app app
  chown -R app:app /app
  exec gosu app "$@"
fi

exec "$@"
