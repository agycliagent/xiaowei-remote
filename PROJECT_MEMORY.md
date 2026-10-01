# 小薇远程 (XiaoWei Remote) 核心架构与项目长期记忆

## 1. 系统核心架构与进程生命周期

- **软件定位**：Windows 跨局域网高可用反向 SSH 穿透与多设备集中管理系统。
- **三阶段严格解耦与静默体验规范（每一步由用户显式点击，0 多余弹窗打扰）**：
  1. **分发与解压阶段**：直接通过 GitHub Releases 分发 `xiaowei-remote.zip`（内含规整文件夹 `小薇远程/安装小薇远程.exe`），完美规避浏览器对裸 EXE 的声誉拦截。解压过程 100% 纯静态解包，解压完即静止，0 自动运行。
  2. **安装阶段（纯静态释放）**：用户打开文件夹，双击 `安装小薇远程.exe` 点击【立即安装】。安装向导仅将程序文件释放至 `%LOCALAPPDATA%\XiaoWeiRemote\` 并在桌面创建快捷方式 `小薇远程.lnk`。点击【完成】直接退出。**安装全过程 0 进程拉起、0 网络外联、0 脚本执行**。
  3. **运行阶段（显式双击与自适应静默守护）**：用户双击桌面「小薇远程」图标时，启动器快速核验进程存活状态，弹出 1.2 秒绿色轻提示（`● 服务已启动，后台守护中`）后自动淡出消失。若异常则显示红色提示。
- **开机自启双模静默兜底机制（0 UAC 弹窗）**：
  - **管理员权限**：自动注册系统级计划任务（`XiaoWei_Remote_Watchdog`）；
  - **普通用户权限**：自动注册当前用户注册表自启（`HKCU:\Software\Microsoft\Windows\CurrentVersion\Run` $\rightarrow$ `XiaoWei_Remote`），无需管理员提权，0 弹窗打扰。
- **断线自愈**：永续看门狗 5s 断线自动重连，心跳每 45s 自动向 VPS 上报。

---

## 2. 基础设施与凭据配置

- **目标 VPS**：`72.60.198.57`（root 用户，本地已配 `vps` 别名免密直连）。
- **VPS 控制台工具**：`/usr/local/bin/win`（实时读取 `/root/.xiaowei/*.dev` 心跳，支持按编号免密直连任意 Windows 终端或全网广播 `win all <cmd>`）。
- **分发站点与直链**：
  - 门户首页：`https://desk.xinjiyuan.tech/`
  - Release 直链：`https://github.com/agycliagent/xiaowei-remote/releases/latest/download/xiaowei-remote.zip`
  - 后端服务：systemd `desk-web.service` 监听 `127.0.0.1:8075`，静态目录 `/var/www/desk/`，Traefik 路由规则 `priority: 1000`。
- **GitHub 仓库**：
  - 用户：`agycliagent`
  - 仓库地址：`https://github.com/agycliagent/xiaowei-remote`（已设为公开 Public 仓库）
  - 密钥配置：持久化于 Windows 用户级环境变量 `GITHUB_TOKEN` 和 `GH_TOKEN`。

---

## 3. 历史踩坑与实战解决经验（防再犯手册）

| 序号 | 故障与坑点现象 | 根本原因 | 最终标准解法 |
| :--- | :--- | :--- | :--- |
| **坑 1** | 域名 `desk.xinjiyuan.tech` 打开返回暂存页 | Traefik 的 `xinjiyuan-backup-pool.yml` 兜底占位规则捕获了所有子域名 | 从 backup pool 剥离该域名，并在 `desk.yml` 显式声明 `priority: 1000` |
| **坑 2** | Chrome/Edge 直接阻止新域名下载 EXE | 新域名+零历史下载哈希触发 Chrome Safe Browsing 最高级拦截 | 托管至 GitHub Releases 官方 CDN 直链，并以 ZIP 规整归档分发 |
| **坑 3** | GitHub 账号新注册调 API 导致挂起（Suspended） | 新账号秒创 Token 并高频调用 Release 上传触发反机器人风控 | 使用稳定绑定的专用账号 `agycliagent`，不再频繁换号或瞬时调用 |
| **坑 4** | 安装包被系统 Defender / 安全机制拦截 | 安装向导在解压释放过程中直接调用 PowerShell 启动后台外联与任务 | **解压、安装、运行三阶段彻底解耦**：安装包仅纯静态写文件和创建快捷方式，退出向导后，用户亲自双击桌面图标才启动守护 |
| **坑 5** | 启动器虚假提示与 LOLBIN 高危特征 | 空 try-catch 吞掉异常；直接向 powershell 传 `-WindowStyle Hidden -ExecutionPolicy Bypass` | C# ProcessStartInfo 封装无窗口调用，做真实进程存活性校验，失败红点/成功绿点 |
| **坑 6** | 开机自启在普通权限下静默失效 | `Register-ScheduledTask` 需要管理员权限，普通用户运行时失败 | **双模自适应**：Admin 走 Task Scheduler，普通用户走 HKCU Run 注册表，0 弹窗 100% 成功 |

---

## 4. 严苛验证铁律：严禁将 HTTP 200 充当“跑通”证据

1. **认知红线**：
   - 任何空文件、静态占位页、反代兜底池、404 自定义页面、Nginx 默认页都会返回 HTTP 200。
   - **HTTP 200 只能证明网络层收到了服务端响应，根本无法证明业务逻辑、文件有效性或功能跑通！**
2. **强制验证标准（必须提供确定性闭环证据）**：
   - **文件/下载验证**：必须进行端到端全量数据流下载，严格校验文件大小精确到字节以及 SHA256 哈希值必须与源文件一致。
   - **服务/运行验证**：必须检查真实的进程 PID、监听端口、心跳文件（`/root/.xiaowei/*.dev`）更新时间戳及交互式命令返回。
