# ========== 阶段一：构建 .NET 应用 ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制解决方案和项目文件
COPY TgLitePanel.sln ./
COPY src ./src
COPY tests ./tests

# 还原依赖
RUN dotnet restore "./TgLitePanel.sln"

# 发布应用
RUN dotnet publish "./src/TgLitePanel.Host/TgLitePanel.Host.csproj" -c Release -o /app/publish --no-restore

# ========== 阶段二：运行时镜像 ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# WTelegramClient 是纯 C# 库，无需原生依赖
# 仅需基础的 .NET 运行时

# 创建数据目录
RUN mkdir -p /data && chmod 755 /data

# 环境变量
ENV ASPNETCORE_URLS=http://+:7070
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DATA_DIR=/data
ENV DB_PATH=/data/app.db

# 暴露端口
EXPOSE 7070

# 复制发布的应用
COPY --from=build /app/publish ./

# 启动应用
ENTRYPOINT ["dotnet", "TgLitePanel.Host.dll"]
