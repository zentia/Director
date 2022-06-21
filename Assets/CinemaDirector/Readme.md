# Timeline
1. Timeline播放器脱胎于AGE编辑器CinemaDirector编辑器的结合体，总体思想将越来越趋近于Timeline。
2. Timeline仅做一个纯表现的编辑器，里面节点逻辑由业务方去实现。
3. 驱动器采用协程去驱动，协程由项目方提供。
4. 资源加载和资源管理由项目方提供。
5. 描述文件为节点文件，仅支持C#脚本，如果想热更新可自行接入huatuo或者ILRuntime等第三方插件
6. 由于希望Timeline成为一个开放的，高内聚低耦合插件，所以非常不建议将内部逻辑与业务方进行耦合。一般来说节点侧的接口已经足够支撑业务开发了。