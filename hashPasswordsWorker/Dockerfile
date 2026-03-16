FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["hashPasswordsWorker/hashPasswordsWorker.csproj", "hashPasswordsWorker/"]
COPY ["Core/Core.csproj", "Core/"]

# Восстанавливаем зависимости
RUN dotnet restore "hashPasswordsWorker/hashPasswordsWorker.csproj"

# Копируем все исходники
COPY . .

# Публикуем приложение
WORKDIR "/src/hashPasswordsWorker"
RUN dotnet publish "hashPasswordsWorker.csproj" -c Release -o /app/publish /p:UseHost=false

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Копируем опубликованные файлы
COPY --from=build /app/publish .

COPY --from=build /src/hashPasswordsWorker/appsettings*.json ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "hashPasswordsWorker.dll"]