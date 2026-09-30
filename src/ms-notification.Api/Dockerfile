FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ms-notification.Api/ms-notification.Api.csproj", "src/ms-notification.Api/"]
RUN dotnet restore "src/ms-notification.Api/ms-notification.Api.csproj"
COPY . .
WORKDIR "/src/src/ms-notification.Api"
RUN dotnet build "./ms-notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ms-notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /App/publish .
ENTRYPOINT ["dotnet", "ms-notification.Api.dll"]
