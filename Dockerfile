# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt/lists,sharing=locked \
    apt-get update \
    && apt-get install -y --no-install-recommends libgpiod2
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RpiHost/RpiHost.csproj", "RpiHost/"]
RUN dotnet restore "RpiHost/RpiHost.csproj"
COPY . .
WORKDIR "/src/RpiHost"
RUN dotnet build "RpiHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RpiHost.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RpiHost.dll"]
