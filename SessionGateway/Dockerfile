#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 7133

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["./SessionGateway/BackMeUp.SessionGateway.csproj", "SessionGateway/"]
RUN dotnet restore "SessionGateway/BackMeUp.SessionGateway.csproj"
COPY . .
WORKDIR "/src/SessionGateway"
RUN dotnet build "BackMeUp.SessionGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BackMeUp.SessionGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackMeUp.SessionGateway.dll"]