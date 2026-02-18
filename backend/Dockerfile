FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CurrencyConverter.slnx .
COPY src/CurrencyConverter.API/CurrencyConverter.API.csproj src/CurrencyConverter.API/
COPY src/CurrencyConverter.Application/CurrencyConverter.Application.csproj src/CurrencyConverter.Application/
COPY src/CurrencyConverter.Domain/CurrencyConverter.Domain.csproj src/CurrencyConverter.Domain/
COPY src/CurrencyConverter.Infrastructure/CurrencyConverter.Infrastructure.csproj src/CurrencyConverter.Infrastructure/
RUN dotnet restore

COPY . .
RUN dotnet publish src/CurrencyConverter.API/CurrencyConverter.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CurrencyConverter.API.dll"]
