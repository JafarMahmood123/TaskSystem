# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all csproj files first for faster layer caching
COPY ["TaskSystem.Api/TaskSystem.Api.csproj", "TaskSystem.Api/"]
COPY ["TaskSystem.Infrastructure/TaskSystem.Infrastructure.csproj", "TaskSystem.Infrastructure/"]
COPY ["TaskSystem.Application/TaskSystem.Application.csproj", "TaskSystem.Application/"]
COPY ["TaskSystem.Domain/TaskSystem.Domain.csproj", "TaskSystem.Domain/"]

# Restore dependencies
RUN dotnet restore "TaskSystem.Api/TaskSystem.Api.csproj"

# Copy everything else and build the application
COPY . .
WORKDIR "/src/TaskSystem.Api"
RUN dotnet build "TaskSystem.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "TaskSystem.Api.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskSystem.Api.dll"]