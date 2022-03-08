#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["./ServiceWorker/BackMeUp.ServiceWorker.csproj", "ServiceWorker/"]
RUN dotnet restore "ServiceWorker/BackMeUp.ServiceWorker.csproj"
COPY . .
WORKDIR "/src/ServiceWorker"
RUN dotnet build "BackMeUp.ServiceWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BackMeUp.ServiceWorker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackMeUp.ServiceWorker.dll"]
