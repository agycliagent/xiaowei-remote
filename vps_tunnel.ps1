<#
.SYNOPSIS
    小薇远程 - 通用多设备跨平台 SSH 反向隧道与高可用看门狗守护管理器 (全自动免配置版)
#>

param (
    [Parameter(Position=0)]
    [ValidateSet("Start", "Stop", "Restart", "Status", "EnableStartup", "DisableStartup", "Test", "Watchdog", "Setup")]
    [string]$Action = "Status",

    [string]$VpsHost = "72.60.198.57",
    [string]$VpsUser = "root",
    [int]$RemotePort = 0,
    [int]$LocalPort = 22,
    [string]$LocalUser = $env:USERNAME,
    [string]$KeyPath = "$HOME\.ssh\xiaowei_client_id_ed25519"
)

$EmbeddedClientPrivateKey = @"
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtzc2gtZW
QyNTUxOQAAACAfs2dl0tclx9y06Mkab7+rFX7GdrNalcuvm2nf1I6t2AAAAJjAPjoBwD46
AQAAAAtzc2gtZWQyNTUxOQAAACAfs2dl0tclx9y06Mkab7+rFX7GdrNalcuvm2nf1I6t2A
AAAECuJbM0EkY8xwr6xMsvr3p0IHugzENL95cE2QW3+EncjR+zZ2XS1yXH3LToyRpvv6sV
fsZ2s1qVy6+bad/Ujq3YAAAAFXhpYW93ZWktcmVtb3RlLWNsaWVudA==
-----END OPENSSH PRIVATE KEY-----
"@

$TrustedPublicKeys = @(
    "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQCjSwqAZAX8qDoFtBF5tIdRsvufEHkDbJylkQ2+R2tUhtxfrTvXj1pkdkYZ/ZJCty56/Sdhm92WJQzi11o7f3tqrOPVfu6GkqiwwIEtC64Y6XtQ/OJte8slEwaOlAhx0LvuxKk4Sa8uWUXYpnhXRGBCKrdjBSIrsptkIMAjb7QMfRGxn/PqGT21Eevn2MGtGoa7V6cTYsQjrxL+mccG7lj3BNeRfmbP8r7dWM9emtvIu9Pe1luw9IIOyzX2STvSU+24b1dScyn1454dopjygl2aGCFoi/fs246shrzto42xyTfi1P3JZtTyQddD6I2SgIHJcg9BsDQ1cw8YhN6FcEqPinDSkrgr7dy8a8IHii5ICF/o/xm3j/zt15977lx0lJbfNpcz6eGYHqflLuChNm5ZQbWEWYzpBNUmq5DPQPvEsD7Zmxch21SXD5Ui8+hvUJrRlsotR0msToXVqmA/plpDBUN26fMw7SV+7nUION/sPKQQ2KJPcbMlDPg3J1+ENMk= root@srv1403503",
    "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIDRvmfdoqeHs+K8I5AifH6+p8V9MZpktVF3QNY/T74ZV vps-access",
    "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIB+zZ2XS1yXH3LToyRpvv6sVfsZ2s1qVy6+bad/Ujq3Y xiaowei-remote-client"
)

function Get-MachinePort {
    if ($RemotePort -gt 0) { return $RemotePort }
    $configPath = "$PSScriptRoot\config.json"
    if (Test-Path $configPath) {
        try {
            $cfg = Get-Content $configPath -Raw | ConvertFrom-Json
            if ($cfg.RemotePort -gt 0) { return [int]$cfg.RemotePort }
        } catch {}
    }
    # Deterministic port using MD5
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($env:COMPUTERNAME)
    $hashBytes = $md5.ComputeHash($bytes)
    $num = [BitConverter]::ToUInt16($hashBytes, 0)
    return (2220 + ($num % 80))
}

$ActualPort = Get-MachinePort
$LogDir = "$HOME\.ssh"
$LogFile = "$LogDir\xiaowei_remote.log"
$TaskName = "XiaoWei_Remote_Watchdog"

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $formatted = "[$timestamp] $Message"
    Write-Host $formatted
    if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }
    Add-Content -Path $LogFile -Value $formatted -ErrorAction SilentlyContinue
}

function Get-SshExePath {
    if (Test-Path "$PSScriptRoot\OpenSSH-Win64\ssh.exe") { return "$PSScriptRoot\OpenSSH-Win64\ssh.exe" }
    if (Test-Path "C:\Windows\System32\OpenSSH\ssh.exe") { return "C:\Windows\System32\OpenSSH\ssh.exe" }
    $cmd = Get-Command "ssh.exe" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return "C:\Windows\System32\OpenSSH\ssh.exe"
}

function Update-VpsRegistration {
    try {
        $ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        $regCmd = "mkdir -p /root/.xiaowei && echo '$env:COMPUTERNAME|$LocalUser|$ActualPort|$ts' > /root/.xiaowei/$env:COMPUTERNAME.dev"
        $sshExec = Get-SshExePath
        & $sshExec -o BatchMode=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile=/dev/null -o ConnectTimeout=4 -i "$KeyPath" "$VpsUser@$VpsHost" "$regCmd" 2>$null | Out-Null
    } catch {}
}

function Remove-VpsRegistration {
    try {
        $unregCmd = "rm -f /root/.xiaowei/$env:COMPUTERNAME.dev"
        $sshExec = Get-SshExePath
        & $sshExec -o BatchMode=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile=/dev/null -o ConnectTimeout=4 -i "$KeyPath" "$VpsUser@$VpsHost" "$unregCmd" 2>$null | Out-Null
    } catch {}
}

function Set-SecureAcl {
    param([string]$Path, [bool]$IsAdminFile = $false)
    try {
        $acl = Get-Acl $Path
        $acl.SetAccessRuleProtection($true, $false)
        foreach ($rule in $acl.Access) {
            $acl.RemoveAccessRule($rule) | Out-Null
        }
        
        if ($IsAdminFile) {
            $adminSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-544")
            $systemSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-18")
            $adminRule = New-Object System.Security.AccessControl.FileSystemAccessRule($adminSid, "FullControl", "Allow")
            $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule($systemSid, "FullControl", "Allow")
            $acl.AddAccessRule($adminRule)
            $acl.AddAccessRule($systemRule)
        } else {
            $userSid = New-Object System.Security.Principal.NTAccount($env:USERNAME)
            $systemSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-18")
            $userRule = New-Object System.Security.AccessControl.FileSystemAccessRule($userSid, "FullControl", "Allow")
            $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule($systemSid, "FullControl", "Allow")
            $acl.AddAccessRule($userRule)
            $acl.AddAccessRule($systemRule)
        }
        Set-Acl -Path $Path -AclObject $acl
    } catch {}
}

function Ensure-Prerequisites {
    if (-not (Test-Path "$HOME\.ssh")) {
        New-Item -ItemType Directory -Path "$HOME\.ssh" -Force | Out-Null
    }
    
    if (-not (Test-Path $KeyPath)) {
        [System.IO.File]::WriteAllText($KeyPath, ($EmbeddedClientPrivateKey.Trim() + "`n"), [System.Text.Encoding]::ASCII)
    }

    Set-SecureAcl -Path $KeyPath -IsAdminFile $false

    # OpenSSH Setup Strategy
    $sshCapability = Get-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0 -ErrorAction SilentlyContinue
    $usePortable = $false

    if ($sshCapability -and $sshCapability.State -eq "NotPresent") {
        try {
            Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0 -ErrorAction SilentlyContinue | Out-Null
            Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0 -ErrorAction SilentlyContinue | Out-Null
        } catch {}
    }

    $sshdService = Get-Service -Name sshd -ErrorAction SilentlyContinue
    if (-not $sshdService) {
        try { Start-Process -FilePath "dism.exe" -ArgumentList "/Online /Add-Capability /CapabilityName:OpenSSH.Server~~~~0.0.1.0 /Quiet /NoRestart" -Wait -WindowStyle Hidden -ErrorAction SilentlyContinue } catch {}
        $sshdService = Get-Service -Name sshd -ErrorAction SilentlyContinue
    }

    if (-not $sshdService) {
        $usePortable = $true
    }

    if ($usePortable) {
        $zipPath = "$PSScriptRoot\OpenSSH-Win64.zip"
        $sshDir = "$PSScriptRoot\OpenSSH-Win64"
        if ((Test-Path $zipPath) -and (-not (Test-Path $sshDir))) {
            Expand-Archive -Path $zipPath -DestinationPath $PSScriptRoot -Force
        }
        if (Test-Path "$sshDir\sshd.exe") {
            try {
                $binPath = "$sshDir\sshd.exe"
                New-Service -Name sshd -BinaryPathName $binPath -DisplayName "OpenSSH SSH Server" -StartupType Automatic -ErrorAction SilentlyContinue
                
                # Setup keys for portable
                if (-not (Test-Path "C:\ProgramData\ssh\ssh_host_ed25519_key")) {
                    & "$sshDir\ssh-keygen.exe" -A 2>$null | Out-Null
                }
                
                # Fix portable sshd_config for locale bug (Match Group administrators)
                $configPath = "C:\ProgramData\ssh\sshd_config"
                if (Test-Path $configPath) {
                    $cfgText = Get-Content $configPath -Raw
                    if ($cfgText -match "Match Group administrators") {
                        $cfgText = $cfgText -replace "Match Group administrators", "Match Group administrators,管理员"
                        Set-Content -Path $configPath -Value $cfgText -Force
                    }
                }
                Start-Service sshd -ErrorAction SilentlyContinue
            } catch {}
        }
    } else {
        if (-not (Test-Path "C:\ProgramData\ssh\ssh_host_ed25519_key")) {
            try { & "C:\Windows\System32\OpenSSH\ssh-keygen.exe" -A 2>$null | Out-Null } catch {}
        }
        if ($sshdService.Status -ne "Running") {
            Start-Service sshd -ErrorAction SilentlyContinue
        }
        Set-Service -Name sshd -StartupType Automatic -ErrorAction SilentlyContinue
        
        # Also fix standard sshd_config for Chinese Windows locale just in case
        $configPath = "C:\ProgramData\ssh\sshd_config"
        if (Test-Path $configPath) {
            $cfgText = Get-Content $configPath -Raw
            if ($cfgText -match "Match Group administrators" -and $cfgText -notmatch "管理员") {
                $cfgText = $cfgText -replace "Match Group administrators", "Match Group administrators,管理员"
                Set-Content -Path $configPath -Value $cfgText -Force
            }
        }
    }

    try {
        if (-not (Get-NetFirewallRule -Name "OpenSSH-Server-In-TCP" -ErrorAction SilentlyContinue)) {
            New-NetFirewallRule -Name "OpenSSH-Server-In-TCP" -DisplayName "OpenSSH Server (sshd)" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -ErrorAction SilentlyContinue | Out-Null
        }
    } catch {}

    $userAuth = "$HOME\.ssh\authorized_keys"
    $userKeys = if (Test-Path $userAuth) { Get-Content $userAuth -Raw } else { "" }
    foreach ($pubKey in $TrustedPublicKeys) {
        if ($userKeys -notmatch [regex]::Escape($pubKey.Trim())) {
            Add-Content -Path $userAuth -Value ($pubKey.Trim()) -Force
            $userKeys += "`n$($pubKey.Trim())"
        }
    }

    $adminAuth = "C:\ProgramData\ssh\administrators_authorized_keys"
    if (-not (Test-Path "C:\ProgramData\ssh")) {
        New-Item -ItemType Directory -Path "C:\ProgramData\ssh" -Force | Out-Null
    }
    $adminKeys = if (Test-Path $adminAuth) { Get-Content $adminAuth -Raw } else { "" }
    foreach ($pubKey in $TrustedPublicKeys) {
        if ($adminKeys -notmatch [regex]::Escape($pubKey.Trim())) {
            Add-Content -Path $adminAuth -Value ($pubKey.Trim()) -Force
            $adminKeys += "`n$($pubKey.Trim())"
        }
    }
    Set-SecureAcl -Path $adminAuth -IsAdminFile $true
}

function Stop-TunnelProcesses {
    Write-Host "正在停止小薇远程反向隧道与看门狗守护..."
    Remove-VpsRegistration

    $sshProcs = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -like "ssh*.exe" -and $_.CommandLine -like "*$ActualPort`:localhost*"-and $_.CommandLine -like "*$VpsHost*"
    }
    foreach ($proc in $sshProcs) {
        Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Host "已终止 SSH 隧道进程 (PID: $($proc.ProcessId))"
    }

    $wdProcs = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -like "powershell*.exe" -and $_.CommandLine -like "*vps_tunnel.ps1*" -and $_.CommandLine -like "*-Action Watchdog*" -and $_.ProcessId -ne $PID
    }
    foreach ($proc in $wdProcs) {
        Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Host "已终止看门狗监控进程 (PID: $($proc.ProcessId))"
    }
}

function Run-Watchdog {
    Ensure-Prerequisites
    Write-Log "小薇远程看门狗已启动 | 主机: $env:COMPUTERNAME | 用户: $LocalUser | 端口: $ActualPort"
    Update-VpsRegistration

    $sshExec = Get-SshExePath
    $tunnelArgs = @(
        "-N",
        "-R", "${ActualPort}:localhost:${LocalPort}",
        "-o", "ServerAliveInterval=15",
        "-o", "ServerAliveCountMax=3",
        "-o", "ExitOnForwardFailure=yes",
        "-o", "StrictHostKeyChecking=accept-new",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "LogLevel=ERROR",
        "-i", "$KeyPath",
        "$VpsUser@$VpsHost"
    )
    
    $hbJob = Start-Job -ScriptBlock {
        param($sshPath, $key, $vps, $name, $usr, $port)
        while ($true) {
            Start-Sleep -Seconds 45
            try {
                $ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
                $cmd = "mkdir -p /root/.xiaowei && echo '$name|$usr|$port|$ts' > /root/.xiaowei/$name.dev"
                & $sshPath -o BatchMode=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile=/dev/null -o ConnectTimeout=4 -i "$key" "$vps" "$cmd" 2>$null | Out-Null
            } catch {}
        }
    } -ArgumentList $sshExec, $KeyPath, "$VpsUser@$VpsHost", $env:COMPUTERNAME, $LocalUser, $ActualPort

    while ($true) {
        try {
            Update-VpsRegistration
            Write-Log "正在建立 SSH 反向隧道 (端口 $ActualPort)..."
            $process = Start-Process -FilePath $sshExec -ArgumentList $tunnelArgs -PassThru -NoNewWindow -Wait
            Write-Log "SSH 隧道已断开 (退出码: $($process.ExitCode))，5 秒后尝试重新连接..."
        }
        catch {
            Write-Log "启动 SSH 隧道失败: $($_.Exception.Message)"
        }
        Start-Sleep -Seconds 5
    }
}

function Start-TunnelBackground {
    Ensure-Prerequisites
    Stop-TunnelProcesses

    Set-StartupTask -Silent $true

    $scriptPath = $PSCommandPath
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "powershell.exe"
    $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$scriptPath`" -Action Watchdog -RemotePort $ActualPort"
    $psi.CreateNoWindow = $true
    $psi.UseShellExecute = $false
    
    [System.Diagnostics.Process]::Start($psi) | Out-Null
    Start-Sleep -Seconds 2
    Show-Status
}

function Show-Status {
    $tunnelProc = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -like "ssh*.exe" -and $_.CommandLine -like "*$ActualPort`:localhost*" -and $_.CommandLine -like "*$VpsHost*"
    }
    $wdProc = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -like "powershell*.exe" -and $_.CommandLine -like "*vps_tunnel.ps1*" -and $_.CommandLine -like "*-Action Watchdog*"
    }
    $task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue

    Write-Host "================= 小薇远程状态 ================="
    Write-Host "当前电脑名称    : $env:COMPUTERNAME"
    Write-Host "当前系统用户名  : $LocalUser"
    Write-Host "目标 VPS 主机    : $VpsUser@$VpsHost"
    Write-Host "当前分配穿透端口 : $ActualPort -> 本地 $LocalPort"
    Write-Host "Windows SSH 服务 : $((Get-Service -Name sshd -ErrorAction SilentlyContinue).Status)"
    Write-Host "看门狗监控进程   : $(if ($wdProc) { "运行中 (PID: $($wdProc.ProcessId))" } else { "未运行" })"
    Write-Host "SSH 反向隧道     : $(if ($tunnelProc) { "已连通 (PID: $($tunnelProc.ProcessId))" } else { "未连通" })"
    Write-Host "开机自启守护     : $(if ($task) { "已配置 ($($task.State))" } else { "未配置" })"
    Write-Host "================================================"
    if ($tunnelProc) {
        Write-Host "`n[进入提示] 在 VPS 运行 'win' 查看所有电脑列表，或执行:" -ForegroundColor Cyan
        Write-Host "ssh -p $ActualPort $LocalUser@localhost`n" -ForegroundColor Green
    }
}

function Set-StartupTask {
    param([bool]$Silent = $false)
    Ensure-Prerequisites
    $scriptPath = $PSCommandPath
    $regKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $regName = "XiaoWei_Remote"
    $regValue = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$scriptPath`" -Action Watchdog -RemotePort $ActualPort"

    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if ($isAdmin) {
        try {
            $action = New-ScheduledTaskAction -Execute "powershell.exe" `
                -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$scriptPath`" -Action Watchdog -RemotePort $ActualPort"
            $trigger = New-ScheduledTaskTrigger -AtStartup
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit (New-TimeSpan -Days 0)
            $principal = New-ScheduledTaskPrincipal -UserId "$env:USERNAME" -LogonType S4U -RunLevel Highest
            Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force | Out-Null
            if (-not $Silent) { Write-Host "成功注册系统级开机自启任务: $TaskName" }
            return
        } catch {}
    }

    try {
        Set-ItemProperty -Path $regKey -Name $regName -Value $regValue -Force | Out-Null
        if (-not $Silent) { Write-Host "成功注册用户级开机自启 (HKCU Run): $regName" }
    } catch {}
}

function Remove-StartupTask {
    if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
    }
    Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "XiaoWei_Remote" -ErrorAction SilentlyContinue
    Write-Host "已注销开机自启配置。"
}

function Test-Connectivity {
    Write-Host "正在从 VPS 反向测试登录当前电脑 ($LocalUser@localhost:$ActualPort)..."
    try {
        $sshExec = Get-SshExePath
        $result = & $sshExec -o BatchMode=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile=/dev/null -o ConnectTimeout=5 -i "$KeyPath" $VpsUser@$VpsHost "ssh -o BatchMode=yes -o StrictHostKeyChecking=accept-new -o UserKnownHostsFile=/dev/null -o ConnectTimeout=5 -p $ActualPort $LocalUser@localhost 'echo CONNECT_OK'"
        if ($result -match "CONNECT_OK") {
            Write-Host "双向连通性测试通过！互通正常。" -ForegroundColor Green
        } else {
            Write-Host "连通性测试未返回预期结果: $result" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "连通性测试失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

switch ($Action) {
    "Start"          { Start-TunnelBackground }
    "Stop"           { Stop-TunnelProcesses; Show-Status }
    "Restart"        { Stop-TunnelProcesses; Start-TunnelBackground }
    "Status"         { Show-Status }
    "EnableStartup"  { Set-StartupTask; Show-Status }
    "DisableStartup" { Remove-StartupTask; Show-Status }
    "Test"           { Test-Connectivity }
    "Watchdog"       { Run-Watchdog }
}
