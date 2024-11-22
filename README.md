TencentCloudCls Log SDK
---

`TencentCloudCls Log SDK` 是专门为 CLS 量身打造日志上传 SDK。 一切只为满足您的需求～

### 安装

SDK 托管在 NUGET 包管理平台，可通过 NUGET 安装。

https://www.nuget.org/packages/TencentCloudCls

### CLS Endpoint

Endpoint 填写请参考[可用地域](https://cloud.tencent.com/document/product/614/18940#.E5.9F.9F.E5.90.8D)中 **API上传日志**
Tab 中的域名![image-20230403191435319](https://github.com/TencentCloud/tencentcloud-cls-sdk-js/blob/main/demo.png)

### 密钥信息

SecretId 和 SecretKey 为云API密钥，密钥信息获取请前往[密钥获取](https://console.cloud.tencent.com/cam/capi)
。并请确保密钥关联的账号具有相应的[SDK上传日志权限](https://cloud.tencent.com/document/product/614/68374#.E4.BD.BF.E7.94.A8-api-.E4.B8.8A.E4.BC.A0.E6.95.B0.E6.8D.AE)

### 同步 VS 异步

SDK 支持同步和异步两种发送模式，同步会实时将日志上传，异步会将日志在后台聚合再上传。同步上传模式拥有更好的实时性，异步上传模式拥有更好的性能，大部分场景对日志上传的实时性要求不高，因此推荐使用异步模式。

上传模式由 `ClientProfile.SendPolicy` 决定，使用 `SendPolicy.Immediate` 来指定同步上传模式，使用 `SendPolicy.SmallBatch` 或
`SendPolicy.LargeBatch` 来指定异步上传模式。

### 示例

请参考 [TencentCloudCls.Examples/Example.cs](TencentCloudCls.Examples/Example.cs)

### 配置参数详解

ClientProfile

| 参数         | 类型          | 描述                                   |
|------------|-------------|--------------------------------------|
| Credential | ICredential | 账户密钥                                 |
| Compressor | ICompressor | 压缩算法，支持 lz4 和 无压缩，默认为 lz4            |    
| SendPolicy | SendPolicy  | 日志聚合上传策略，详见下方的 `SendPolicy` 参数详解     |
| Scheme     | string      | CLS 请求协议，默认为 `https://`              |
| Endpoint   | string      | CLS 请求域名                             |
| Source     | string      | 日志来源，一般使用机器IP，作为日志 `__SOURCE__` 字段上报 |
| Hostname   | string      | 主机名，作为日志 `__HOSTNAME__` 字段上报         |
| Logger     | ILogger     | 日志输出 logger，默认不输出日志                  |

SendPolicy

| 参数               | 类型       | 描述                                                                      |
|------------------|----------|-------------------------------------------------------------------------|
| MaxBatchSize     | ulong    | 实例能缓存的日志大小上限，单位为 byte；0 表示不缓存，同步上传                                      |
| MaxBatchCount    | ulong    | 实例能缓存的日志条数上限；0 表示不缓存，同步上传                                               |    
| FlushInterval    | TimeSpan | 日志从创建到可发送的逗留时间                                                          |
| MaxRetry         | uint     | 上报日志失败时的重试次数                                                            |
| MaxRetryInterval | uint     | 上报日志失败时的重试最大间隔，重试间隔采用指数退避，第 N 次重试间隔为 `Min(2^(N-1), MaxRetryInterval)` 秒 |
| Worker           | uint     | 异步场景下上报日志的 worker 数量                                                    |
