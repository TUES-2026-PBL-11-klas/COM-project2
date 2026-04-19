# tva buildva vsichki dependencyta
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "PMProject.slnx"
WORKDIR /src/PM.API
RUN dotnet publish -c Release -o /app/out

# tva buildva samo aplikaciqta, bez da buildva dependencyta (po leko i byrzo)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/out ./

EXPOSE 5130

ENTRYPOINT ["dotnet", "PM.API.dll"]
