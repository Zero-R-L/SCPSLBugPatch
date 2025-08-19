# SCPSLBugPatch
修复了一些会导致服务器崩溃或卡顿的Bug
## 我是否应该使用这个插件？
- 在`LocalAdminLogs`中出现`Disconnecting connId=0 to prevent exploits`意味着服务器已经断开了本地主机连接，通常将导致所有玩家出现时间停止并且无法加入服务器同时服务器控制台`Delayed connection incoming from endpoint`刷屏
- 在`LocalAdminLogs`中出现`[NM] DataReceived: bad!`刷屏意味着服务器频繁接受到异常的数据包，可能正在遭受DDoS攻击，可查询攻击包数、总攻击数据大小、攻击源IP
## 插件依赖
- LabAPI
- 0Harmony
## 插件功能
- 服务器后台可使用指令 `sbdi` 来查询坏包（可能为DDoS数据包）信息
- 防止服务器断开本地主机连接
- 回合重启时自动展示本回合坏包信息
- 本插件的所有Log将保存至 `\SCP Secret Laboratory\SCPSLBugPatch.log`
## 插件功能部分展示
![104da998-697f-43ae-85a0-e517eab097f0](https://github.com/user-attachments/assets/cc7970a2-8900-4d2e-99d7-4ed709980a40)
## 如需修复更多SCPSL的BUG，请提交Issues
