# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy csproj truoc de cache layer restore
COPY LibrarySystem.slnx ./
COPY backend/src/Library.Web/Library.Web.csproj                             backend/src/Library.Web/
COPY backend/src/Library.Modules.Core/Library.Modules.Core.csproj           backend/src/Library.Modules.Core/
COPY backend/src/Library.Modules.AI/Library.Modules.AI.csproj               backend/src/Library.Modules.AI/
COPY backend/src/Library.Modules.ZeroTrust/Library.Modules.ZeroTrust.csproj backend/src/Library.Modules.ZeroTrust/

# Restore tat ca projects qua solution
RUN dotnet restore LibrarySystem.slnx

# Copy toan bo source
COPY backend/src/ backend/src/

# Publish (khong dung --no-restore de dam bao package day du)
RUN dotnet publish backend/src/Library.Web/Library.Web.csproj -c Release -o /app/publish

# Stage 2: Runtime (Debian-based -> dung groupadd/useradd)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Library.Web.dll"]
