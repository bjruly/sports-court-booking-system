# ===== Giai đoạn 1: build bằng .NET 8 SDK =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .csproj trước để tận dụng cache khi restore
COPY backend/src/SportsCourtBooking.Domain/*.csproj SportsCourtBooking.Domain/
COPY backend/src/SportsCourtBooking.Application/*.csproj SportsCourtBooking.Application/
COPY backend/src/SportsCourtBooking.Infrastructure/*.csproj SportsCourtBooking.Infrastructure/
COPY backend/src/SportsCourtBooking.Web/*.csproj SportsCourtBooking.Web/
RUN dotnet restore SportsCourtBooking.Web/SportsCourtBooking.Web.csproj

# Copy toàn bộ code rồi publish
COPY backend/src/ .
RUN dotnet publish SportsCourtBooking.Web/SportsCourtBooking.Web.csproj -c Release -o /app/publish --no-restore

# ===== Giai đoạn 2: chạy bằng runtime nhẹ =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8 trong container mặc định nghe cổng 8080
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SportsCourtBooking.Web.dll"]
