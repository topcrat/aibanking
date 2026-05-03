FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AIBanking.AIAgent/AIBanking.AIAgent.csproj", "AIBanking.AIAgent/"]
COPY ["AIBanking/AIBanking.csproj", "AIBanking/"]
COPY ["AIBanking.Data/AIBanking.Data.csproj", "AIBanking.Data/"]
COPY ["AIBanking.ServiceDefaults/AIBanking.ServiceDefaults.csproj", "AIBanking.ServiceDefaults/"]
RUN dotnet restore "./AIBanking.AIAgent/AIBanking.AIAgent.csproj"
COPY . .
WORKDIR "/src/AIBanking.AIAgent"
RUN dotnet build "./AIBanking.AIAgent.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AIBanking.AIAgent.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIBanking.AIAgent.dll"]
