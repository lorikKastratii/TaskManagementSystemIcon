# ---------------------------------------------------------------------------
# Backend (ASP.NET Core Web API) — multi-stage build.
# Build context is the repository root (TaskManagementSystem/).
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first using only project files so Docker can cache the layer.
COPY TaskManager.slnx ./
COPY src/TaskManager.Domain/TaskManager.Domain.csproj src/TaskManager.Domain/
COPY src/TaskManager.Application/TaskManager.Application.csproj src/TaskManager.Application/
COPY src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
COPY src/TaskManager.API/TaskManager.API.csproj src/TaskManager.API/
RUN dotnet restore src/TaskManager.API/TaskManager.API.csproj

# Copy the rest of the sources and publish.
COPY src/ src/
RUN dotnet publish src/TaskManager.API/TaskManager.API.csproj -c Release -o /app/publish

# ---------------------------------------------------------------------------
# Runtime image.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TaskManager.API.dll"]
