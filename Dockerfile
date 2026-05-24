FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION
ARG TARGETARCH
WORKDIR /app
COPY wArrden/wArrden.csproj .
RUN dotnet restore -r linux-$TARGETARCH
COPY wArrden/ .
RUN dotnet publish -c Release -r linux-$TARGETARCH -o /app/bin

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:10.0-alpine
ARG APP_VERSION
ARG PUID
ARG PGID
ENV APP_VERSION=$APP_VERSION
ENV PUID=$PUID
ENV PGID=$PGID
ENV PATH="/app/bin:$PATH"
RUN apk add --no-cache su-exec sqlite-libs tzdata
WORKDIR /app
COPY --from=build /app/bin /app/bin
# The bundled libe_sqlite3.so (from SQLitePCLRaw) targets glibc and fails on Alpine's musl.
# Replace it with a symlink to Alpine's own musl-compiled libsqlite3 instead.
RUN rm -f /app/bin/libe_sqlite3.so \
 && ln -s /usr/lib/libsqlite3.so.0 /app/bin/e_sqlite3.so \
 && echo '#!/bin/sh' > /app/bin/clear-missing \
 && echo 'dotnet /app/bin/wArrden.dll clear-missing "$@"' >> /app/bin/clear-missing \
 && echo '#!/bin/sh' > /app/bin/clear-upgrades \
 && echo 'dotnet /app/bin/wArrden.dll clear-upgrades "$@"' >> /app/bin/clear-upgrades \
 && chmod +x /app/bin/clear-missing /app/bin/clear-upgrades
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
VOLUME ["/app/data"]
ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "/app/bin/wArrden.dll"]
