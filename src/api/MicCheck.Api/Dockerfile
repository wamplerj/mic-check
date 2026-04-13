FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/api/MicCheck.Api/MicCheck.Api.csproj", "src/api/MicCheck.Api/"]
RUN dotnet restore "src/api/MicCheck.Api/MicCheck.Api.csproj"
COPY . .
RUN dotnet build "src/api/MicCheck.Api/MicCheck.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/api/MicCheck.Api/MicCheck.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MicCheck.Api.dll"]
