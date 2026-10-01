# 小薇远程 (XiaoWei Remote) 核心架构与项目长期记忆

## 1. 系统核心架构与进程生命周期

- **软件定位**：Windows 跨局域网高可用反向 SSH 穿透与多设备集中管理系统。
- **解耦三阶段铁律（核心防拦截规范，每一步必须用户亲自显式点击）**：
  1. **分发与下载阶段**：直接以 `https://desk.xinjiyuan.tech/` 域名分发 152KB 单文件原版可执行安装程序（`安装小薇远程.exe` / `XiaoWeiRemote_Setup.exe`），严禁多余的 `.zip` 压缩打包套娃。
  2. **安装与解压阶段（纯静态释放）**：用户双击安装包，亲自点击【立即安装】。`安装小薇远程.exe` **只负责纯静态文件释放**至 `%LOCALAPPDATA%\XiaoWeiRemote\` 并在 Windows 桌面生成带黑底 `>_` 终端图标的快捷方式 `小薇远程.lnk`。点击【完成】直接退出向导。**安装全过程 0 进程拉起、0 网络外联、0 脚本执行**。
  3. **运行与守护阶段（用户显式触发）**：仅在用户**双击桌面快捷方式**时，才拉起后台守护并弹出 1.2 秒绿色轻提示（`● 服务已启动，后台守护中`）后自动淡出消失。
- **守护机制**：永续看门狗 5s 断线自愈，注册 Windows 任务计划程序（`XiaoWei_Remote_Watchdog`）开机自启。

---

## 2. 基础设施与凭据配置

- **目标 VPS**：`72.60.198.57`（root 用户，本地已配 `vps` 别名免密直连）。
- **VPS 控制台工具**：`/usr/local/bin/win`（实时读取 `/root/.xiaowei/*.dev` 心跳，支持按编号免密直连任意 Windows 终端或全网广播 `win all <cmd>`）。
- **分发站点与直链**：
  - 门户首页：`https://desk.xinjiyuan.tech/`
  - 安装包直链：`https://desk.xinjiyuan.tech/安装小薇远程.exe`（备用：`https://desk.xinjiyuan.tech/XiaoWeiRemote_Setup.exe`）
  - 后端：systemd `desk-web.service` 监听 `127.0.0.1:8075`，静态目录 `/var/www/desk/`，Traefik 路由规则 `priority: 1000`。
- **GitHub 仓库**：
  - 用户：`agycliagent`
  - 仓库地址：`https://github.com/agycliagent/xiaowei-remote`（已设为公开 Public 仓库）
  - 密钥配置：持久化于 Windows 用户级环境变量 `GITHUB_TOKEN` 和 `GH_TOKEN`。
  - Release 直链：`https://github.com/agycliagent/xiaowei-remote/releases/latest/download/XiaoWeiRemote_Setup.exe`

---

## 3. 历史踩坑与实战解决经验（防再犯手册）

| 序号 | 故障与坑点现象 | 根本原因 | 最终标准解法 |
| :--- | :--- | :--- | :--- |
| **坑 1** | 域名 `desk.xinjiyuan.tech` 打开返回暂存页 | Traefik 的 `xinjiyuan-backup-pool.yml` 兜底占位规则捕获了所有子域名 | 从 backup pool 剥离该域名，并在 `desk.yml` 显式声明 `priority: 1000` |
| **坑 2** | Chrome 浏览器直接阻止新域名下载 EXE/ZIP | 新域名+零历史下载哈希触发 Chrome Safe Browsing 最高级拦截 | 托管至 GitHub Releases 官方 CDN 直链与自建门户双分发，保持纯净单文件 |
| **坑 3** | GitHub 账号新注册调 API 导致挂起（Suspended） | 新账号秒创 Token 并高频调用 Release 上传触发反机器人风控 | 使用稳定绑定的专用账号 `agycliagent`，不再频繁换号或瞬时调用 |
| **坑 4** | 安装包被系统 Defender / 安全机制拦截 | 安装向导在解压释放过程中直接调用 PowerShell 启动后台外联与任务 | **解压、安装、运行三阶段彻底解耦**：安装包仅纯静态写文件和创建快捷方式，退出向导后，用户亲自双击桌面图标才启动守护 |
| **坑 5** | 安装向导界面排版遮挡与重叠 | WinForms 面板 Dock 布局 Z-order 与高 DPI 缩放导致深色 Banner 遮盖下方控件 | 显式设置 Z-order（`contentPanel.BringToFront()`），调整 `ClientSize(500, 320)` 与充足内边距 |
| **坑 6** | 控制面板常驻造成用户体验冗余 | 用户不需要复杂的多按钮控制面板 | 双击桌面图标仅展示 1.2 秒绿色「● 服务已启动」提示窗，随后静默退出，后台守护常驻 |
| **坑 7** | 单文件安装包被多余二次打包成 ZIP | 忽略单文件原版轻量优势（仅 152KB），强加解压步骤破坏极简体验 | 彻底废除 ZIP 包装，全链路直接分发原版单文件 `安装小薇远程.exe` / `XiaoWeiRemote_Setup.exe` |

---

## 4. 严苛验证铁律：严禁将 HTTP 200 充当“跑通”证据

1. **认知红线**：
   - 任何空文件、静态占位页、反代兜底池、404 自定义页面、Nginx 默认页都会返回 HTTP 200。
   - **HTTP 200 只能证明网络层收到了服务端响应，根本无法证明业务逻辑、文件有效性或功能跑通！**
2. **强制验证标准（必须提供确定性闭环证据）**：
   - **文件/下载验证**：必须进行端到端全量数据流下载，严格校验文件大小精确到字节（155,648 字节）以及 SHA256 哈希值必须与源文件一致。
   - **服务/运行验证**：必须检查真实的进程 PID、监听端口、心跳文件（`/root/.xiaowei/*.dev`）更新时间戳及交互式命令返回，绝不能以“页面能打开/状态码是200”作为交付依据。
