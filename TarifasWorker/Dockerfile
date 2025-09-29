FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BankMore.sln", "/src/"]
COPY ["TarifasWorker/TarifasWorker.csproj", "TarifasWorker/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "TarifasWorker/TarifasWorker.csproj"
COPY . .
WORKDIR "/src/TarifasWorker"
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "TarifasWorker.dll"]



