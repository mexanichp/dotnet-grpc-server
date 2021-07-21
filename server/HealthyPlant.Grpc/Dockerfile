FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["server/HealthyPlant.Grpc/HealthyPlant.Grpc.csproj", "HealthyPlant.Grpc/"]
COPY ["server/HealthyPlant.Data/HealthyPlant.Data.csproj", "HealthyPlant.Data/"]
COPY ["server/HealthyPlant.Domain/HealthyPlant.Domain.csproj", "HealthyPlant.Domain/"]
COPY ["server/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "HealthyPlant.Grpc/HealthyPlant.Grpc.csproj"
COPY protos /protos
COPY server .
WORKDIR "/src/HealthyPlant.Grpc"
RUN dotnet build "HealthyPlant.Grpc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HealthyPlant.Grpc.csproj" -c Release -o /app/publish

FROM build AS tools
RUN dotnet tool install --global dotnet-trace

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=tools /root/.dotnet/tools /usr/local/bin
ENTRYPOINT ["dotnet", "HealthyPlant.Grpc.dll"]