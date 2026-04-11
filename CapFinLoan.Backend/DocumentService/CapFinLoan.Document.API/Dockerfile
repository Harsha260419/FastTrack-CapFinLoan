FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CapFinLoan.Backend ./CapFinLoan.Backend
RUN dotnet restore "CapFinLoan.Backend/DocumentService/CapFinLoan.Document.API/CapFinLoan.Document.API.csproj"
RUN dotnet build "CapFinLoan.Backend/DocumentService/CapFinLoan.Document.API/CapFinLoan.Document.API.csproj" -c Release -o /app/build
RUN dotnet publish "CapFinLoan.Backend/DocumentService/CapFinLoan.Document.API/CapFinLoan.Document.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5262
ENTRYPOINT ["dotnet", "CapFinLoan.Document.API.dll"]
