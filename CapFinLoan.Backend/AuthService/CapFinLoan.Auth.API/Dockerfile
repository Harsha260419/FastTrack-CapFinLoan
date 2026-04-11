FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CapFinLoan.Backend ./CapFinLoan.Backend
RUN dotnet restore "CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/CapFinLoan.Auth.API.csproj"
RUN dotnet build "CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/CapFinLoan.Auth.API.csproj" -c Release -o /app/build
RUN dotnet publish "CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/CapFinLoan.Auth.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5025
ENTRYPOINT ["dotnet", "CapFinLoan.Auth.API.dll"]
