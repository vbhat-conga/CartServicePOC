#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CartServicePOC.csproj", "."]
RUN dotnet restore "./CartServicePOC.csproj"
RUN dotnet dev-certs https
RUN dotnet dev-certs https --trust
COPY . .
WORKDIR "/src/."
RUN dotnet build "CartServicePOC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CartServicePOC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
COPY ./wait-for-it.sh /app/wait-for-it.sh
RUN chmod +x wait-for-it.sh
ENTRYPOINT ["dotnet", "CartServicePOC.dll"]