FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ICalMonitor.Worker/ICalMonitor.Worker.csproj ICalMonitor.Worker/
RUN dotnet restore ICalMonitor.Worker/ICalMonitor.Worker.csproj
COPY . .
RUN dotnet publish ICalMonitor.Worker -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ICalMonitor.Worker.dll"]
