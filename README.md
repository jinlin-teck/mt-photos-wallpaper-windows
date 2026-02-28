# MT-Photos Wallpaper

将 MT-Photos 相册中的照片设置为 Windows 桌面壁纸的工具。

## 功能特点

- 从 MT-Photos 账户获取相册列表
- 选择相册设置为壁纸来源
- 自动轮播更换壁纸
- 本地图片缓存，支持设置最大缓存数量和大小
- 支持多种图片显示模式（缩放、居中、拉伸等）
- 系统托盘运行，最小化到托盘
- 开机自启动

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime（仅框架依赖版本需要）

## 使用方法

1. 首次运行需要配置连接信息：
   - **Server URL**: MT-Photos 服务器地址
   - **Username**: 你的用户名
   - **Password**: 你的密码

2. 点击 "Test Connection" 测试连接是否正常

3. 在 "Album Selection" 中选择要使用的相册

4. 设置更换间隔（默认 30 分钟）

5. 点击 "Save Settings" 保存配置

## 图片显示模式

| 模式 | 说明 |
|------|------|
| Zoom | 缩放填充（保持比例） |
| Centered | 居中显示 |
| Scaled | 适应屏幕 |
| Stretched | 拉伸填充 |
| Spanned | 跨屏显示 |
| Wallpaper | 使用系统壁纸设置 |

## 常见问题

**连接失败怎么办？**
- 确认 Server URL 格式正确（包含 http:// 或 https://）
- 检查用户名和密码是否正确
- 确保 MT-Photos 服务器可访问

**壁纸不自动更换？**
- 检查是否勾选了 "Pause Rotation"
- 确认更换间隔设置是否正确

## 下载安装

从 [Releases](https://github.com/your-repo/releases) 下载最新版本。

下载 `publish-self-contained.zip` 或 `publish-fdd.zip` 后解压：

- **自包含版本** (`publish-self-contained/`)：单文件，156MB，不需要安装 .NET Runtime
- **框架依赖版本** (`publish-fdd-nonsingle/`)：体积小（约 1.2MB），适合电脑上已安装了 [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

解压后直接运行 `MTPhotosWallpaper.exe`。

## 技术支持

如遇到问题，请在 GitHub 提交 Issue。
