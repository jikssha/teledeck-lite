<div align="center">

# 🚀 Teledeck

### *轻量级 Telegram 多账号管理面板*

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?style=flat-square&logo=blazor)](https://blazor.net/)
[![WTelegramClient](https://img.shields.io/badge/MTProto-WTelegramClient-0088CC?style=flat-square&logo=telegram)](https://github.com/wiz0u/WTelegramClient)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat-square&logo=docker)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

---

*基于纯 C# MTProto 实现的轻量级 Telegram 多账号聊天面板*


</div>

---

## ✨ 特性

<table>
<tr>
<td width="50%">

### 🪶 轻量级架构
- **纯 C# 实现** - 无需编译原生库
- **单账号约 10MB** - 50+ 账号仅需 ~500MB 内存
- **1C1G 可运行** - 适配低配服务器

</td>
<td width="50%">

### ⚡ 快速部署
- **2-5 分钟构建** - 
- **一键启动** - `docker compose up -d`
- **零配置** - 自动创建数据库和目录

### 🆕 版本提示
- 顶部栏会显示当前版本号；若检测到 GitHub 上有更新，会提示“新版本”并可一键跳转到仓库查看。
- 需要在 `src/TgLitePanel.Host/appsettings.json` 配置 `AppUpdate:RepositoryUrl`（或用环境变量 `APPUPDATE__REPOSITORYURL` 覆盖）。

</td>
</tr>
<tr>
<td width="50%">

### 🎨 现代化 UI
- **Material Design 3** - 基于 MudBlazor 的暗色主题
- **实时更新** - Blazor Server 交互组件
- **响应式布局** - 适配桌面和移动端

</td>
<td width="50%">

### 🔐 安全优先
- **无敏感日志** - 验证码和密码不记录
- **会话加密** - 安全的会话文件存储
- **速率限制** - 批量操作内置保护

</td>
</tr>
</table>

---

## 📊 性能对比

| 指标 | TDLib (旧方案) | WTelegramClient (当前) | 改进幅度 |
|:----:|:-------------:|:----------------------:|:-------:|
| **单账号内存** | ~80MB | ~10MB | ↓ 87.5% |
| **50 账号内存** | 4-8GB | ~500MB | ↓ 90% |
| **Docker 构建** | 30-60 分钟 | 2-5 分钟 | ↓ 90% |
| **最低配置** | 4C8G | 1C1G | ✅ 省钱 |
| **原生依赖** | libtdjson.so | 无 | ✅ 纯托管 |

---

## 🏗️ 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                    🎨 Blazor Server UI                      │
│              Material Design 3 + MudBlazor                  │
├─────────────────────────────────────────────────────────────┤
│                     📦 业务模块层                            │
│            账号管理  │  聊天界面  │  系统设置                 │
├─────────────────────────────────────────────────────────────┤
│                   🔌 核心抽象层                              │
│          ITdClient  │  ITdClientManager  │  Services        │
├─────────────────────────────────────────────────────────────┤
│                    ⚙️ 基础设施层                             │
│    ┌─────────────────────┐    ┌─────────────────────┐      │
│    │   WTelegram Client  │    │    EF Core + SQLite │      │
│    │   纯 C# MTProto     │    │      数据持久化      │      │
│    └─────────────────────┘    └─────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

### 项目结构

```
Teledeck/
├── 📁 src/
│   ├── 📁 TgLitePanel.Host/                 # 主入口，Blazor Server
│   ├── 📁 TgLitePanel.Core.Abstractions/    # 接口与模型定义
│   ├── 📁 TgLitePanel.Core.Modules/         # 模块系统核心
│   ├── 📁 TgLitePanel.Infrastructure.WTelegram/    # WTelegramClient 封装
│   ├── 📁 TgLitePanel.Infrastructure.Persistence/  # EF Core + SQLite
│   └── 📁 modules/
│       ├── 📁 TgLitePanel.Module.Accounts/  # 账号管理模块
│       ├── 📁 TgLitePanel.Module.Chat/      # 聊天界面模块
│       └── 📁 TgLitePanel.Module.Settings/  # 系统设置模块
├── 📁 tests/
│   └── 📁 TgLitePanel.Tests/                # 单元测试
├── 📁 docker/
│   ├── 📄 Dockerfile                        # 多阶段构建
│   ├── 📄 docker-compose.yml                # 一键部署
│   └── 📄 .env.example                      # 环境变量模板
└── 📄 TgLitePanel.sln                       # 解决方案文件
```

---

## 🚀 快速开始

### 前置要求

- 一台 VPS 服务器（最低 1C1G）
- 已安装 [Docker](https://docs.docker.com/get-docker/) + [Docker Compose](https://docs.docker.com/compose/install/)
- 从 [my.telegram.org](https://my.telegram.org) 获取 API ID 和 API Hash
- （可选）一个域名，用于 HTTPS 访问

### 第一步：克隆仓库

```bash
# 克隆仓库
git clone https://github.com/YOUR_USERNAME/Teledeck.git
cd Teledeck/docker

# 复制环境变量模板
cp .env.example .env

# 编辑配置（重要：务必修改默认密码！）
nano .env
```

### 第二步：启动服务

```bash
# 构建并启动（首次运行需要 2-5 分钟）
docker compose up -d --build

# 查看日志
docker compose logs -f
```

### 第三步：初始配置

1. 打开浏览器访问 `http://服务器IP:7070`
2. 使用 `.env` 中配置的账号密码登录
3. 进入 **设置** → 配置 Telegram `API ID` 和 `API Hash`
4. 进入 **账号管理** → 添加你的第一个 Telegram 账号

---

## 🌐 配置 Caddy 反向代理（推荐）

使用 Caddy 可以轻松实现 HTTPS 和域名访问，自动申请和续期 SSL 证书。

### 方式一：独立安装 Caddy

#### 1. 安装 Caddy

```bash
# Debian/Ubuntu
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update
sudo apt install caddy

# CentOS/RHEL
sudo yum install yum-plugin-copr
sudo yum copr enable @caddy/caddy
sudo yum install caddy
```

#### 2. 配置 Caddyfile

```bash
sudo nano /etc/caddy/Caddyfile
```

写入以下内容（将 `tg.example.com` 替换为你的域名）：

```caddyfile
tg.example.com {
    # 自动 HTTPS（Caddy 会自动申请 Let's Encrypt 证书）

    # 反向代理到 Teledeck
    reverse_proxy localhost:7070

    # 可选：启用压缩
    encode gzip zstd

    # 可选：自定义日志
    log {
        output file /var/log/caddy/teledeck.log
        format json
    }
}
```

#### 3. 启动 Caddy

```bash
# 重载配置
sudo systemctl reload caddy

# 查看状态
sudo systemctl status caddy

# 设置开机自启
sudo systemctl enable caddy
```

### 方式二：Docker Compose 集成 Caddy

修改 `docker-compose.yml`，添加 Caddy 服务：

```yaml
services:
  tglitepanel:
    build:
      context: ..
      dockerfile: docker/Dockerfile
    # 移除 ports 映射，只通过 Caddy 访问
    # ports:
    #   - "7070:7070"
    environment:
      DATA_DIR: ${DATA_DIR:-/data}
      DB_PATH: ${DB_PATH:-/data/app.db}
      ADMIN_INIT_USER: ${ADMIN_INIT_USER:-admin}
      ADMIN_INIT_PASS: ${ADMIN_INIT_PASS:-change-me}
    volumes:
      - ../data:/data
    restart: unless-stopped
    networks:
      - teledeck-net

  caddy:
    image: caddy:2-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
      - caddy_config:/config
    restart: unless-stopped
    networks:
      - teledeck-net
    depends_on:
      - tglitepanel

networks:
  teledeck-net:
    driver: bridge

volumes:
  caddy_data:
  caddy_config:
```

在 `docker/` 目录下创建 `Caddyfile`：

```bash
nano docker/Caddyfile
```

写入以下内容：

```caddyfile
tg.example.com {
    reverse_proxy tglitepanel:7070
    encode gzip zstd
}
```

启动服务：

```bash
docker compose up -d --build
```

### DNS 配置

在你的域名 DNS 管理面板添加 A 记录：

| 类型 | 名称 | 值 | TTL |
|:----:|:----:|:--:|:---:|
| A | tg | 你的服务器 IP | 600 |

等待 DNS 生效后（通常几分钟），即可通过 `https://tg.example.com` 访问面板。

### 验证 HTTPS

```bash
# 检查证书状态
curl -I https://tg.example.com

# 应该看到类似输出：
# HTTP/2 200
# server: Caddy
```

---

## ⚙️ 配置说明

### 环境变量

| 变量 | 默认值 | 说明 |
|:-----|:-------|:-----|
| `DATA_DIR` | `/data` | 数据存储目录 |
| `DB_PATH` | `/data/app.db` | SQLite 数据库路径 |
| `ADMIN_INIT_USER` | `admin` | 初始管理员用户名 |
| `ADMIN_INIT_PASS` | `change-me` | 初始管理员密码（**请务必修改！**） |

### 数据存储

| 路径 | 说明 |
|:-----|:-----|
| `/data/app.db` | SQLite 数据库（账号信息、配置、用户） |
| `/data/accounts/<id>/session.dat` | WTelegram 会话文件 |
| `/data/tmp/` | 临时文件（自动清理） |

---

## 📦 导入导出

### 会话导入格式

Teledeck 支持灵活的会话文件导入：

**单账号导入**
```
archive.zip
├── 1234567890.session    # 会话文件（必需）
├── 1234567890.json       # 元数据（可选）
└── 2fa.txt               # 二级密码（可选）
```

**批量导入**
```
archive.zip
├── +1234567890/
│   ├── +1234567890.session
│   ├── +1234567890.json
│   └── 2fa.txt           # 自动解析并存储
├── +9876543210/
│   ├── +9876543210.session
│   └── +9876543210.json
└── ...
```

> **提示**：`2fa.txt` 文件中的密码会自动解析并保存到数据库，登录时自动使用。

---

## 🛠️ 本地开发

```bash
# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test

# 启动开发服务器
dotnet run --project src/TgLitePanel.Host
```

### 技术栈

| 层级 | 技术 |
|:-----|:-----|
| **前端** | Blazor Server, MudBlazor, Material Design 3 |
| **后端** | ASP.NET Core 8, C# 12 |
| **Telegram** | WTelegramClient（纯 C# MTProto 实现） |
| **数据库** | Entity Framework Core, SQLite |
| **容器化** | Docker, Docker Compose |
| **反向代理** | Caddy（自动 HTTPS） |

---

## 🔒 安全说明

### 安全措施

- ✅ **无敏感日志** - 验证码和二级密码永不记录到日志
- ✅ **会话加密** - WTelegramClient 处理安全的会话存储
- ✅ **速率限制** - 批量操作内置保护机制
- ✅ **密码哈希** - 管理员密码使用 Argon2id 安全哈希
- ✅ **ZIP 炸弹防护** - 导入验证防止恶意压缩包

### 不支持的功能

本项目专注于个人多账号管理，**不支持**以下功能：

- ❌ 群发消息 / 自动化任务
- ❌ 媒体消息（图片、视频、语音）
- ❌ 加密聊天（Secret Chat）
- ❌ 语音/视频通话

> ⚠️ **提示**：请遵守 Telegram 服务条款，合理使用本项目。

---

## 📋 开发路线

- [ ] 多语言支持（i18n）
- [ ] 账号分组与标签
- [ ] 消息搜索功能
- [ ] Webhook 通知
- [ ] 账号健康监控
- [ ] 备份恢复优化

---

## 🤝 贡献指南

欢迎提交 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m '添加某个功能'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

---

## 📄 许可证

本项目基于 MIT 许可证开源 - 详见 [LICENSE](LICENSE) 文件。

---

---

<div align="center">

**用 ❤️ 和 ☕ 制作**

*如果这个项目对你有帮助，欢迎点个 ⭐*

</div>
