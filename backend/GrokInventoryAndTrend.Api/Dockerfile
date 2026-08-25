FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["backend/GrokInventoryAndTrend.Api/GrokInventoryAndTrend.Api.csproj", "backend/GrokInventoryAndTrend.Api/"]
RUN dotnet restore "backend/GrokInventoryAndTrend.Api/GrokInventoryAndTrend.Api.csproj" --configfile NuGet.Config --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json
COPY backend/GrokInventoryAndTrend.Api/ backend/GrokInventoryAndTrend.Api/
COPY dataset-seed/ dataset-seed/
WORKDIR /src/backend/GrokInventoryAndTrend.Api
RUN dotnet publish "GrokInventoryAndTrend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false -m:1 --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/dataset-seed /app/dataset-seed
ENV Dataset__RootPath=/app/dataset-seed
ENTRYPOINT ["dotnet", "GrokInventoryAndTrend.Api.dll"]
