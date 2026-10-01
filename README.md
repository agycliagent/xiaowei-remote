# 小薇远程 (XiaoWei Remote)

通用 Windows 跨局域网高可用反向 SSH 穿透与多设备集中管理系统。

---

## 🌟 核心设计规范

- **三阶段严格解耦（防安全拦截铁律）**：
  1. **下载分发**：直接分发 152KB 单文件原版安装程序 `XiaoWeiRemote_Setup.exe`，无任何冗余 ZIP 压缩。
  2. **安装解压（纯静态释放）**：用户打开安装包，亲手点击【立即安装】。安装程序只做纯静态文件解压与创建桌面快捷方式，安装全过程 **0 进程拉起、0 脚本执行、0 网络外联**。
  3. **运行守护（显式触发）**：用户回到桌面亲手双击【小薇远程】图标，才显式拉起后台守护并展示 1.2 秒绿色状态轻提示。

---

## 🚀 下载与安装

- **官方下载中心**：[https://desk.xinjiyuan.tech/](https://desk.xinjiyuan.tech/)
- **GitHub Release 官方直链**：[立即下载 安装小薇远程.exe (152 KB)](https://github.com/agycliagent/xiaowei-remote/releases/latest/download/XiaoWeiRemote_Setup.exe)

---

## 🛠️ 项目结构

```text
xiaowei-remote/
├── SetupWizard.cs          # C# WinForms 安装向导（纯静态解压 + 桌面快捷方式生成）
├── XiaoWeiFlashApp.cs      # C# 1.2s 极简状态轻提示启动器（小薇远程.exe）
├── vps_tunnel.ps1          # 核心反向 SSH 隧道与 5s 断线自愈看门狗
├── pkg/
│   ├── app_classic_dark.ico # 经典黑底 >_ 终端图标
│   ├── start.bat           # 手动启动脚本
│   ├── stop.bat            # 停止脚本
│   └── status.bat          # 状态查看脚本
├── index.html              # 极简黑夜风格 Web 下载中心
├── desk.yml                # Traefik 独立动态路由配置
└── PROJECT_MEMORY.md       # 项目核心架构与防踩坑手册
```

---

## 📋 使用说明

1. **安装**：运行 `XiaoWeiRemote_Setup.exe`，点击「立即安装」，程序解压至 `%LOCALAPPDATA%\XiaoWeiRemote\` 并在桌面创建「小薇远程」快捷方式。
2. **启动**：双击桌面「小薇远程」图标开启后台自愈守护。
3. **VPS 管理端**：
   ```bash
   win list           # 查看当前在线设备
   win 1              # 交互式连接 1 号设备 Windows PowerShell
   win all ipconfig   # 全网在线设备广播执行命令
   ```

---

## 📄 License
MIT License
