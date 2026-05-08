FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY wArrden/wArrden.csproj .
RUN dotnet restore
COPY wArrden/ .
RUN dotnet publish -c Release -o /app/bin

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app/bin
COPY --from=build /app/bin .
ENTRYPOINT ["dotnet", "wArrden.dll"]
