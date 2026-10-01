# 小薇远程 (XiaoWei Remote)

通用 Windows 跨局域网高可用反向 SSH 穿透与多设备集中管理系统。

---

## 🌟 核心特性

- **开箱即用 · 纯静态解耦安装**：安装包 `安装小薇远程.exe` 仅负责静态解压与创建桌面快捷方式，安装过程绝不调用脚本或网络，彻底规避安全软件误拦截。
- **极简交互 · 闪烁即走**：双击桌面「小薇远程」黑色终端图标，展示 1.2 秒绿色轻提示（`● 服务已启动，后台守护中`）后自动退出界面，守护进程后台常驻。
- **高可用永续看门狗**：内置 5 秒断线心跳自愈机制，支持开机自动注册系统计划任务。
- **VPS 集中免密管控**：服务端通过 `win` CLI 工具实时感知节点上下线，支持按节点编号直连或 `win all <cmd>` 全局广播执行命令。

---

## 🚀 下载与安装

- **官方下载门户**：[https://desk.xinjiyuan.tech/](https://desk.xinjiyuan.tech/)
- **GitHub Release 官方直链**：[下载最新小薇远程 (ZIP)](https://github.com/agycliagent/xiaowei-remote/releases/latest/download/xiaowei-remote.zip)
- **EXE 安装包直链**：[下载 XiaoWeiRemote_Setup.exe](https://github.com/agycliagent/xiaowei-remote/releases/latest/download/XiaoWeiRemote_Setup.exe)

---

## 🛠️ 项目结构

```text
xiaowei-remote/
├── SetupWizard.cs          # C# WinForms 安装向导（纯静态释放与快捷方式创建）
├── XiaoWeiFlashApp.cs      # C# 1.2s 极简绿色状态提示启动器（小薇远程.exe）
├── vps_tunnel.ps1          # 核心反向 SSH 隧道与 5s 心跳看门狗
├── pkg/
│   ├── app_classic_dark.ico # 经典黑底 >_ 终端图标
│   ├── start.bat           # 启动脚本
│   ├── stop.bat            # 停止脚本
│   └── status.bat          # 状态查看脚本
├── index.html              # 极简黑夜风格 Web 下载中心
├── desk.yml                # Traefik 独立动态路由配置
└── PROJECT_MEMORY.md       # 项目长期架构与踩坑防犯手册
```

---

## 📋 运行规范

1. **安装**：运行 `安装小薇远程.exe`，点击「立即安装」，程序解压至 `%LOCALAPPDATA%\XiaoWeiRemote\` 并在桌面创建快捷方式。
2. **启动**：双击桌面「小薇远程」图标即可启动后台隧道守护。
3. **VPS 节点管理**：
   ```bash
   win list           # 查看当前在线设备
   win 1              # 交互式连接 1 号设备 Windows PowerShell
   win all ipconfig   # 向全网所有在线设备广播执行命令
   ```

---

## 📄 License
MIT License
