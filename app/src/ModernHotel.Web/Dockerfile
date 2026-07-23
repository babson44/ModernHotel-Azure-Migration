FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ModernHotel.Core/ModernHotel.Core.csproj", "src/ModernHotel.Core/"]
COPY ["src/ModernHotel.Web/ModernHotel.Web.csproj", "src/ModernHotel.Web/"]
RUN dotnet restore "src/ModernHotel.Web/ModernHotel.Web.csproj"
COPY . .
RUN dotnet publish "src/ModernHotel.Web/ModernHotel.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ModernHotel.Web.dll"]
