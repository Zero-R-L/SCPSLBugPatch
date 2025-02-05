# SCPSLBugPatch
修复了一些会导致服务器崩溃和卡顿的Bug
## 我是否应该使用这个插件？
- 如果你的服务器出现了回合结束后所有玩家卡在正在连接服务器的界面的问题，并且在日志中出现\
`Disconnecting connId=0 to prevent exploits from an Exception in MessageHandler: ObjectDisposedException Cannot access a disposed object.`
- 如果你的服务器日志文件太大，并且在日志中出现以下类似刷屏\
`[NM] DataReceived: bad!`\
`Found 'null' entry in observing list for connectionId=114514. Please call NetworkServer.Destroy to destroy networked objects. Don't use GameObject.Destroy.`
- 如果你的服务器经常遭受DDoS攻击，并且希望知道攻击包数、总攻击数据大小、攻击源IP
## 插件依赖
0Harmony
## 插件功能
- 服务器后台可使用指令 `sbnw` 来查询坏包（DDoS数据包）信息
- 屏蔽了一些无用的刷屏DebugWarning日志输出（Northwood的Shit）
- 防止服务器为防止意外而断开服务器主机连接并Log
- 回合重启时自动展示本回合坏包信息
- 本插件的Log将保存至 `%Roming%\SCP Secret Laboratory\SCPSLBugPatch.log`
