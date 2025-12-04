# 后端 API 扩展方案（IEC62061 / 库管理 / 证据）

## 1. IEC 62061
- POST /api/iec62061/evaluate
  - 入参：SafetyFunction62061（见模块规格）
  - 出参：{ pfhd, achievedSIL, warnings[], details }
- POST /api/iec62061/report
- POST /api/iec62061/report.pdf
- GET/POST/PUT/DELETE /api/iec62061/functions/{id}
  - 管理函数及其子系统与组件；审计与版本化。

## 2. 组件安全参数库
- GET /api/library/components?query=&page=&pageSize=
- POST /api/library/components
- PUT /api/library/components/{id}
- DELETE /api/library/components/{id}
- POST /api/library/import （JSON/CSV）
- GET /api/library/export （JSON/CSV）

## 3. 互通
- POST /api/interop/import (SISTEMA/PAScal/SET)
- GET /api/interop/export?target=sistema|pascal|set

## 4. 证据库
- GET /api/evidence?type=&status=&page=&pageSize=
- POST /api/evidence （metadata + 文件上传）
- PUT /api/evidence/{id}
- DELETE /api/evidence/{id}
- POST /api/evidence/link （功能/计算/检查条目关联）

## 5. 合规映射与报告
- GET /api/compliance/matrix?projectId=
- POST /api/compliance/matrix/export (.csv/.xlsx/.json)
- POST /api/report/templates/preview
- POST /api/report/generate (.html/.pdf)

## 6. 安全与审计
- 所有写操作要求 JWT + RBAC；
- 审计日志：事件类型、资源、摘要、签名与哈希；不可变存储。

