FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BankMore.sln", "/src/"]
COPY ["AccountApi/AccountApi.csproj", "AccountApi/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "AccountApi/AccountApi.csproj"
COPY . .
WORKDIR "/src/AccountApi"
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "AccountApi.dll"]



