FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ModernHotel.Core/ModernHotel.Core.csproj", "src/ModernHotel.Core/"]
COPY ["src/ModernHotel.Infrastructure/ModernHotel.Infrastructure.csproj", "src/ModernHotel.Infrastructure/"]
COPY ["src/ModernHotel.API/ModernHotel.API.csproj", "src/ModernHotel.API/"]
RUN dotnet restore "src/ModernHotel.API/ModernHotel.API.csproj"
COPY . .
RUN dotnet publish "src/ModernHotel.API/ModernHotel.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ModernHotel.API.dll"]
