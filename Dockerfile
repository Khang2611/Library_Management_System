# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy csproj truoc de cache layer restore
COPY LibrarySystem.slnx ./
COPY src/Library.Web/Library.Web.csproj                             src/Library.Web/
COPY src/Library.Modules.Core/Library.Modules.Core.csproj           src/Library.Modules.Core/
COPY src/Library.Modules.AI/Library.Modules.AI.csproj               src/Library.Modules.AI/
COPY src/Library.Modules.ZeroTrust/Library.Modules.ZeroTrust.csproj src/Library.Modules.ZeroTrust/

# Restore tat ca projects qua solution
RUN dotnet restore LibrarySystem.slnx

# Copy toan bo source
COPY src/ src/

# Publish (khong dung --no-restore de dam bao package day du)
RUN dotnet publish src/Library.Web/Library.Web.csproj -c Release -o /app/publish

# Stage 2: Runtime (Debian-based -> dung groupadd/useradd)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Library.Web.dll"]
