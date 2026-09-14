FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY frontend/src/GrokInventoryAndTrend.WebApp/GrokInventoryAndTrend.WebApp.csproj frontend/src/GrokInventoryAndTrend.WebApp/
RUN dotnet restore frontend/src/GrokInventoryAndTrend.WebApp/GrokInventoryAndTrend.WebApp.csproj --configfile NuGet.Config --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json
COPY frontend/src/GrokInventoryAndTrend.WebApp/ frontend/src/GrokInventoryAndTrend.WebApp/
COPY dataset-seed/ dataset-seed/
WORKDIR /src/frontend/src/GrokInventoryAndTrend.WebApp
RUN dotnet publish -c Release -o /app/publish --no-restore
RUN test -s /app/publish/wwwroot/_framework/blazor.web.js \
    && grep -Eq '"Route"[[:space:]]*:[[:space:]]*"_framework/blazor\.web\.js"' /app/publish/GrokInventoryAndTrend.WebApp.staticwebassets.endpoints.json \
    || { echo "Published frontend is missing the Blazor script or its static asset endpoint." >&2; exit 1; }

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/dataset-seed /app/dataset-seed
ENV DatasetSeed__RootPath=/app/dataset-seed
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GrokInventoryAndTrend.WebApp.dll"]
