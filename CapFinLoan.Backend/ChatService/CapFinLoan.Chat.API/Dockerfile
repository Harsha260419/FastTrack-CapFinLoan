FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CapFinLoan.Backend ./CapFinLoan.Backend
RUN dotnet restore "CapFinLoan.Backend/ChatService/CapFinLoan.Chat.API/CapFinLoan.Chat.API.csproj"
RUN dotnet build "CapFinLoan.Backend/ChatService/CapFinLoan.Chat.API/CapFinLoan.Chat.API.csproj" -c Release -o /app/build
RUN dotnet publish "CapFinLoan.Backend/ChatService/CapFinLoan.Chat.API/CapFinLoan.Chat.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5200
ENTRYPOINT ["dotnet", "CapFinLoan.Chat.API.dll"]
