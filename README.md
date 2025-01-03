# eot-server.net

#### 介绍
本开源库为物联网边缘终端EOBox大数据平台服务.NET版本。eoiot开源项目分为五大模块，从边缘终端到服务器，从前端到后台，从Web服务到App，提供全方位开源代码，MIT许可协议，可无任何后顾之忧直接用于商业项目。本项目的目的在于打造一个专用于小微型用途的低成本的、高弹性的物联网实用平台。

采用多线程Socket异步IO模型，支持大规模终端并发。支持多线程MySQL数据库操作。支持网络包日志文件。支持TCP/HJ212环保协议。可直接对接基于STM32 F407嵌入式数采仪终端（EOBox，请参考另一开源库eot-embdtub）

采用.NET Core API实现对终端信息的存储、编辑，状态管理，物联网数据参数实时数据查询，历史数据查询。实现了API访问权限底层架构，以及建立在架构之上的基础框架功能，包括账号、部门、角色、权限、菜单等功能。（前后端分离，依赖于前端Vue3.0界面，请参考另一开源库eot-webui3）

全部模块使用原生代码，不依赖于第三方，能从原理上理解整个系统核心要点，做到安全可控可扩展。

详细信息请查看API参考注释

#### 软件架构
目前暂为预览版本，后续进一步完善，使用Microsoft Visual Studio Community 2022，.NET 6.0进行开发

eot-server.net分为两大模块，EOTServer采用C/S架构，TCP Server为物联网终端提供网络数据采集服务，目前以实现HJ212协议，可进行多协议扩展。另一模块EOTWebService采用B/S架构，Web API为前端管理页面提供功能接口。

两大模块既可以用于小型应用合并执行，EOTServer也可独立启动 C/S和B/S分开运行，后面我们会提供分布式大规模弹性多服务器部署方案，可实现百万级终端大数据并发处理。

#### 使用说明
代码使用Microsoft Visual Studio Community 2022 (64 位) 个人社区版本（免费）直接打开，无需下载依赖任何第三方库。

数据库使用Oracle MySQL Community Server（开源免费）8.0，推荐官方免费的MySQL Workbench可视化工具，直接导入db/eotgate.sql脚本。本系统提供了独立的SQL语句执行系统，不再使用存储过程，避免过多依赖于数据库，可更好的进行多数据库迁移。

为了和终端兼容，并扩展方便，采用了yml配置文件格式。平台一共使用两个配置文件server.yml和web.yml分别对应C/S模块和B/S模块。

完善扩展了HJ212协议，可通过HJ212进行终端固件版本升级，预留了版本回滚机制。采用Base64可见字符传输二进制版本文件，虽然加大了传输量，但很好的兼容了HJ212协议和移远EC模块指令。


#### 参与贡献

1.  Fork 本仓库
2.  新建 Feat_xxx 分支
3.  提交代码
4.  新建 Pull Request


#### 特技

1.  使用 Readme\_XXX.md 来支持不同的语言，例如 Readme\_en.md, Readme\_zh.md
2.  Gitee 官方博客 [blog.gitee.com](https://blog.gitee.com)
3.  你可以 [https://gitee.com/explore](https://gitee.com/explore) 这个地址来了解 Gitee 上的优秀开源项目
4.  [GVP](https://gitee.com/gvp) 全称是 Gitee 最有价值开源项目，是综合评定出的优秀开源项目
5.  Gitee 官方提供的使用手册 [https://gitee.com/help](https://gitee.com/help)
6.  Gitee 封面人物是一档用来展示 Gitee 会员风采的栏目 [https://gitee.com/gitee-stars/](https://gitee.com/gitee-stars/)
