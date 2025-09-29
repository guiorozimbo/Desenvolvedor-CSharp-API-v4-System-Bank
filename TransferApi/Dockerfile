FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BankMore.sln", "/src/"]
COPY ["TransferApi/TransferApi.csproj", "TransferApi/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "TransferApi/TransferApi.csproj"
COPY . .
WORKDIR "/src/TransferApi"
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "TransferApi.dll"]



