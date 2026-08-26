FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY Directory.Build.props MunicipalPlatform.sln ./
COPY apps/api/MunicipalPlatform.Api.csproj apps/api/
RUN dotnet restore apps/api/MunicipalPlatform.Api.csproj
COPY apps/api apps/api
RUN dotnet publish apps/api/MunicipalPlatform.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
RUN addgroup -S municipal && adduser -S municipal -G municipal
WORKDIR /app
COPY --from=build --chown=municipal:municipal /app/publish .
USER municipal
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MunicipalPlatform.Api.dll"]
