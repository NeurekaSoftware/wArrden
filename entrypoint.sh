#!/bin/sh
set -e

if [ -n "$PUID" ] && [ "$PUID" != "0" ] && [ -n "$PGID" ] && [ "$PGID" != "0" ]; then
  userdel app 2>/dev/null || true
  groupdel app 2>/dev/null || true
  groupadd -g "$PGID" app
  useradd -u "$PUID" -g app -M app
  chown -R app:app /app
  exec gosu app "$@"
fi

exec "$@"
