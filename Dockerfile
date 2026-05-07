FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ExamScheduler.csproj", "./"]
RUN dotnet restore "ExamScheduler.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "ExamScheduler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExamScheduler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExamScheduler.dll"]
