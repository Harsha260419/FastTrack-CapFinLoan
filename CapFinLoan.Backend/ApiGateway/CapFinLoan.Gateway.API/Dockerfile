FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CapFinLoan.Backend ./CapFinLoan.Backend
RUN dotnet restore "CapFinLoan.Backend/ApiGateway/CapFinLoan.Gateway.API/CapFinLoan.Gateway.API.csproj"
RUN dotnet build "CapFinLoan.Backend/ApiGateway/CapFinLoan.Gateway.API/CapFinLoan.Gateway.API.csproj" -c Release -o /app/build
RUN dotnet publish "CapFinLoan.Backend/ApiGateway/CapFinLoan.Gateway.API/CapFinLoan.Gateway.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8002
ENTRYPOINT ["dotnet", "CapFinLoan.Gateway.API.dll"]
