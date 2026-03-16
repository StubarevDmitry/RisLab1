FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["hashPasswordsManager/hashPasswordsManager.csproj", "hashPasswordsManager/"]
COPY ["Core/Core.csproj", "Core/"]

RUN dotnet restore "hashPasswordsManager/hashPasswordsManager.csproj"

COPY . .

WORKDIR "/src/hashPasswordsManager"
RUN dotnet publish "hashPasswordsManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

COPY --from=build /src/hashPasswordsManager/appsettings*.json ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "hashPasswordsManager.dll"]