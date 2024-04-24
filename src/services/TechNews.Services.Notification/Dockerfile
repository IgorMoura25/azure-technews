FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["src/services/TechNews.Services.Notification/TechNews.Services.Notification.csproj", "src/services/TechNews.Services.Notification/"]
RUN dotnet restore "src/services/TechNews.Services.Notification/TechNews.Services.Notification.csproj"
COPY . .
WORKDIR "/src/src/services/TechNews.Services.Notification"
RUN dotnet build "TechNews.Services.Notification.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TechNews.Services.Notification.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechNews.Services.Notification.dll"]
