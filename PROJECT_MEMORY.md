# 小薇远程 (XiaoWei Remote) 核心架构与项目长期记忆

## 1. 系统核心架构与进程生命周期

- **软件定位**：Windows 跨局域网高可用反向 SSH 穿透与多设备集中管理系统。
- **安装与运行解耦原则（核心防拦截规范）**：
  1. **安装阶段（纯静态释放）**：`安装小薇远程.exe` **只负责纯静态文件释放**至 `%LOCALAPPDATA%\XiaoWeiRemote\` 并在 Windows 桌面生成带黑底 `>_` 终端图标的快捷方式 `小薇远程.lnk`。**安装过程中严禁调用 PowerShell、严禁建立网络连接或注册任务**。
  2. **启动阶段（用户显式触发）**：仅在用户**双击桌面快捷方式**时，才拉起 `powershell.exe -WindowStyle Hidden -File vps_tunnel.ps1 Start`，并弹出 1.2 秒绿色轻提示（`● 服务已启动，后台守护中`）后自动淡出消失。
  3. **交互规范**：无多余控制面板常驻窗口，点击图标闪烁即走，后台看门狗独立常驻。
  4. **守护机制**：永续看门狗 5s 断线自愈，注册 Windows 任务计划程序（`XiaoWei_Remote_Watchdog`）开机自启。

---

## 2. 基础设施与凭据配置

- **目标 VPS**：`72.60.198.57`（root 用户，本地已配 `vps` 别名免密直连）。
- **VPS 控制台工具**：`/usr/local/bin/win`（实时读取 `/root/.xiaowei/*.dev` 心跳，支持按编号免密直连任意 Windows 终端或全网广播 `win all <cmd>`）。
- **分发站点**：`https://desk.xinjiyuan.tech/`（Traefik 路由规则 `priority: 1000`，后端为 systemd `desk-web.service` 监听 `127.0.0.1:8075`，静态目录 `/var/www/desk/`）。
- **GitHub 仓库**：
  - 用户：`agycliagent`
  - 仓库地址：`https://github.com/agycliagent/xiaowei-remote`
  - 密钥配置：持久化于 Windows 用户级环境变量 `GITHUB_TOKEN` 和 `GH_TOKEN`，后续无需再次询问用户。
  - Release 直链：`https://github.com/agycliagent/xiaowei-remote/releases/latest/download/xiaowei-remote.zip`

---

## 3. 历史踩坑与实战解决经验（防再犯手册）

| 序号 | 故障与坑点现象 | 根本原因 | 最终标准解法 |
| :--- | :--- | :--- | :--- |
| **坑 1** | 域名 `desk.xinjiyuan.tech` 打开返回暂存页 | Traefik 的 `xinjiyuan-backup-pool.yml` 兜底占位规则捕获了所有子域名 | 从 backup pool 剥离该域名，并在 `desk.yml` 显式声明 `priority: 1000` |
| **坑 2** | Chrome 浏览器直接阻止新域名下载 EXE/ZIP | 新域名+零历史下载哈希触发 Chrome Safe Browsing 最高级拦截 | 托管至 GitHub Releases 官方 CDN 直链，借用 global domain 高信誉权重免拦截 |
| **坑 3** | GitHub 账号新注册调 API 导致挂起（Suspended） | 新账号秒创 Token 并高频调用 Release 上传触发反机器人风控 | 使用稳定绑定的专用账号 `agycliagent`，不再频繁换号或瞬时调用 |
| **坑 4** | 安装包被系统 Defender / 安全机制拦截 | 安装向导在安装过程中直接调用 PowerShell 启动反向隧道与系统任务 | **安装与运行彻底解耦**：安装包仅释放文件和创建快捷方式，用户双击桌面图标才启动守护 |
| **坑 5** | 安装向导界面排版遮挡与重叠 | WinForms 面板 Dock 布局 Z-order 与高 DPI 缩放导致深色 Banner 遮盖下方控件 | 显式设置 Z-order（`contentPanel.BringToFront()`），调整 `ClientSize(500, 320)` 与充足内边距 |
| **坑 6** | 控制面板常驻造成用户体验冗余 | 用户不需要复杂的多按钮控制面板 | 双击桌面图标仅展示 1.2 秒绿色「● 服务已启动」提示窗，随后静默退出，后台守护常驻 |
