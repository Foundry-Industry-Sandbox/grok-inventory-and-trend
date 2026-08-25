FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["backend/GrokInventoryAndTrend.Governance/GrokInventoryAndTrend.Governance.csproj", "backend/GrokInventoryAndTrend.Governance/"]
COPY ["backend/GrokInventoryAndTrend.Mcp/GrokInventoryAndTrend.Mcp.csproj", "backend/GrokInventoryAndTrend.Mcp/"]
COPY ["agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning/agents/", "agent-provisioning/src/GrokInventoryAndTrend.AgentProvisioning/agents/"]
RUN dotnet restore "backend/GrokInventoryAndTrend.Mcp/GrokInventoryAndTrend.Mcp.csproj" --configfile NuGet.Config --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json
COPY backend/GrokInventoryAndTrend.Governance/ backend/GrokInventoryAndTrend.Governance/
COPY backend/GrokInventoryAndTrend.Mcp/ backend/GrokInventoryAndTrend.Mcp/
COPY dataset-seed/ dataset-seed/
WORKDIR /src/backend/GrokInventoryAndTrend.Mcp
RUN dotnet publish "GrokInventoryAndTrend.Mcp.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/dataset-seed /app/dataset-seed
ENV Dataset__RootPath=/app/dataset-seed
ENV Dataset__CasesRelativePath=cases
ENV Dataset__FabricPrerequisiteSubfolder=fabric-pre-requisite-data
ENV Dataset__PromotionsFilePath=/app/dataset-seed/promotions-price-rag/promotions_price_calendar.txt
ENV Dataset__SignalQualityFilePath=/app/dataset-seed/signal-quality-rag/signal_quality_rules.txt
ENV Dataset__TrendPatternsFilePath=/app/dataset-seed/trend-patterns-rag/trend_patterns.txt
ENV Dataset__PlanningConstraintsFilePath=/app/dataset-seed/planning-constraints-rag/planning_constraints.txt
ENTRYPOINT ["dotnet", "GrokInventoryAndTrend.Mcp.dll"]
