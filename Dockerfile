# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY bici_bussiness.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/out --nologo

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out ./
EXPOSE 8080
# Render inyecta $PORT; shell-form para expandirla. SQLite se crea por seed al arrancar.
CMD ASPNETCORE_URLS=http://*:$PORT dotnet bici_bussiness.dll
