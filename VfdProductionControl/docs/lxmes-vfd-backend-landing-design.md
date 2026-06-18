# LXMES 后端接入 VFD 测试方案落地设计

本文档面向当前 LXMES Django 后端和 VfdProductionControl WPF 工程，目标是把测试方案管理、设备逻辑点位、WPF 运行端读取、执行追溯上报完整落到现有 `LixiangMes` SQL Server 数据库中。

本设计保留原集成指南中的业务能力：

- 管理端维护工位、槽位、设备型号、逻辑点位、写入选项。
- 管理端维护测试方案、方案版本、流程步骤，并支持复制、排序、校验、发布。
- WPF 只读取已发布可执行方案和运行配置，不直接写方案表。
- WPF 上报执行追溯，后端可按条码、工位、槽位、操作员、方案版本、结论、时间范围查询。
- 后端记录管理写操作审计日志。

## 1. 当前 LXMES 事实

### 1.1 数据库

当前后端已连接 SQL Server 数据库：

```python
DATABASES["default"]["ENGINE"] = "mssql"
DATABASES["default"]["NAME"] = "LixiangMes"
```

建议：不新建数据库，继续使用 `LixiangMes`。VFD 新表统一使用 `vfd_` 前缀，避免和已有 MES 表名冲突。

### 1.2 已有业务模块

当前后端已经有这些业务 app：

| app | 当前职责 | 设计结论 |
| --- | --- | --- |
| `core` | 工厂、客户、部门、员工、`core.User` | 明确复用 `core.Factory` 作为所有 VFD 数据的 `factory_id`。`core.Employee` 可用于操作员工号映射。不继续扩展 `core.User` 作为 VFD 登录用户。 |
| `user` | 当前登录用户 `SysUser`、JWT 认证 | 明确复用 `user.SysUser` 作为管理端审计、创建人、更新人。 |
| `product` | 产品、工序、工艺文件 | 明确复用 `products` 和 `processes`：测试方案适用产品，测试属于某个工序。 |
| `production` | 生产计划、工单、条码扫描、生产数据 | 明确复用 `work_orders`、`barcode_relations`、`barcode_scans`、`production_data`：为 VFD 测试提供工单、条码和汇总数据上下文。 |
| `equipment` | 设备资产、状态、维护、故障 | 明确复用 `equipment`：测试台、仪表、变频器等作为设备资产参与。不要把 VFD 逻辑点位放进设备台账。 |
| `parameter` | 电表、变频器参数 | 可用于产品静态参数参考，但不替代逻辑点位。逻辑点位是通信地址抽象。 |

### 1.3 认证与权限

当前 REST Framework 默认认证类是：

```python
user.authentication.SysUserJWTAuthentication
```

该认证类根据 JWT 的 `user_id` 返回 `user.models.SysUser`。当前项目还存在 `core.User(AbstractUser)`，但 `settings.py` 未设置 `AUTH_USER_MODEL`，因此不能直接把 VFD 表的 `created_by`、`updated_by`、`AuditLog.operator` 设计为 `settings.AUTH_USER_MODEL`。

落地建议：

1. 短期落地：VFD 审计和创建人字段使用 `ForeignKey("user.SysUser")`。
2. 中长期治理：如果未来统一用户体系，再单独设计用户迁移；不要把这件事绑到 VFD 一期上线里。

### 1.4 通用模型风格

当前多数业务表继承 `core.base.FactoryScopedModel`，自动包含：

- `factory`
- `created_at`
- `updated_at`

落地建议：VFD 管理主数据和追溯主表也带 `factory` 字段，方便按工厂过滤和权限隔离。VFD 追溯明细表可通过父表间接获取工厂，但为了查询性能，关键明细表也可以冗余 `factory`。

## 2. 总体落地方案

### 2.1 新增 app

新增一个独立 app：

```text
vfd_control/
  models.py
  serializers.py
  views.py
  urls.py
  services.py
  permissions.py
  filters.py
  management/
    commands/
      seed_vfd_defaults.py
```

推荐先用单 app 落地，原因：

- 当前 LXMES app 粒度偏业务模块，不是 `apps/xxx` 包结构。
- VFD 子域初期模型虽多，但边界统一，先放一个 app 更容易接入现有项目。
- 后续若规模变大，可在 app 内拆文件或拆 app。

### 2.2 路由

在 `LXMES/urls.py` 中新增：

```python
path("api/vfd/", include("vfd_control.urls")),
```

`vfd_control.urls` 内部分两组路径：

```text
/api/vfd/admin/...
/api/vfd/runtime/...
```

这样不会和当前 `/api/production/`、`/api/equipment/`、`/api/core/` 冲突。

### 2.3 表名策略

全部新表使用 `vfd_` 前缀。

| 原概念名 | LXMES 落地表名 |
| --- | --- |
| `Station` | `vfd_stations` |
| `StationSlot` | `vfd_station_slots` |
| `DeviceModel` | `vfd_device_models` |
| `LogicalPoint` | `vfd_logical_points` |
| `LogicalPointWriteOption` | `vfd_logical_point_write_options` |
| `ProcessPlan` | `vfd_process_plans` |
| `ProcessPlanVersion` | `vfd_process_plan_versions` |
| `ProcessStep` | `vfd_process_steps` |
| `StationSession` | `vfd_station_sessions` |
| `DeviceRun` | `vfd_device_runs` |
| `StepRun` | `vfd_step_runs` |
| `MeasurementResult` | `vfd_measurement_results` |
| `ComparisonResult` | `vfd_comparison_results` |
| `CommandTrace` | `vfd_command_traces` |
| `AuditLog` | `vfd_audit_logs` |

## 3. 模型设计

### 3.1 通用抽象基类

建议在 `vfd_control/models.py` 中先定义两个抽象基类。

```python
class VfdAdminManagedModel(FactoryScopedModel):
    is_active = models.BooleanField(default=True, verbose_name="是否启用")
    deleted_at = models.DateTimeField(blank=True, null=True, verbose_name="删除时间")
    created_by = models.ForeignKey(
        "user.SysUser",
        on_delete=models.SET_NULL,
        blank=True,
        null=True,
        related_name="+",
        verbose_name="创建人")
    updated_by = models.ForeignKey(
        "user.SysUser",
        on_delete=models.SET_NULL,
        blank=True,
        null=True,
        related_name="+",
        verbose_name="更新人")

    class Meta:
        abstract = True


class VfdTraceModel(FactoryScopedModel):
    class Meta:
        abstract = True
```

说明：

- `VfdAdminManagedModel` 用于管理端可维护主数据。
- `VfdTraceModel` 用于追溯主表和高频查询表。
- `FactoryScopedModel` 的 `factory` 必填，符合当前 MES 多工厂风格。
- 如果某些 VFD 配置未来要跨工厂共享，再单独给 `factory` 做可空或复制到各工厂；一期不建议做全局共享，避免权限过滤复杂化。

### 3.2 工位与槽位

`vfd_stations`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `station_code` | Char(50) | 工位编码，同工厂唯一 |
| `name` | Char(200) | 工位名称 |
| `is_active` | Bool | 是否启用 |
| `deleted_at` | DateTime null | 软删除 |
| `created_at/updated_at` | DateTime | 继承 |
| `created_by/updated_by` | FK `user.SysUser` | 审计 |

约束：

- `UniqueConstraint(fields=["factory", "station_code"])`

`vfd_station_slots`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 冗余工厂，便于过滤 |
| `station` | FK `vfd_stations` | 所属工位 |
| `slot_number` | PositiveInteger | 槽位号 |
| `display_name` | Char(120) | 显示名 |
| `port_name` | Char(32) null | 串口名，例如 `COM3` |
| `baud_rate` | PositiveInteger | 默认 9600 |
| `vfd_address` | PositiveSmallInteger | VFD Modbus 地址 |
| `voltage_meter_address` | PositiveSmallInteger | 电压表地址 |
| `current_meter_address` | PositiveSmallInteger | 电流表地址 |
| `is_enabled` | Bool | 是否可用 |
| `is_active/deleted_at` |  | 管理状态 |

约束：

- `UniqueConstraint(fields=["factory", "station", "slot_number"])`
- 后端校验同槽位三个地址不能相同。
- 后端校验同一工位下启用槽位的端口和地址组合不能冲突。

如何复用 `equipment.Equipment`：

- 测试台、仪表、变频器等资产继续维护在 `equipment` 表中。
- `vfd_station_slots` 增加可空字段 `equipment = ForeignKey("equipment.Equipment", null=True, blank=True, on_delete=SET_NULL)`，用于绑定测试台或槽位主体设备。
- 如果一个槽位下还要精确绑定“电压表”“电流表”“被测变频器”等多台设备，建议新增 `vfd_slot_equipment_bindings`，字段包含 `slot`、`equipment`、`role`。`role` 可取 `TEST_BENCH`、`VFD`、`VOLTAGE_METER`、`CURRENT_METER`。
- 不建议把槽位本身直接建成 `equipment.Equipment`。槽位是运行位置和通信配置，设备台账是资产。两者关联即可。

### 3.3 设备型号、逻辑点位与写入选项

`vfd_device_models`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `model_code` | Char(100) | 型号编码，同工厂唯一 |
| `name` | Char(200) | 型号名称 |
| `device_type` | Char(50) | `VFD` / `INSTRUMENT` |
| `protocol` | Char(50) | 默认 `MODBUS_RTU` |
| `manufacturer` | Char(100) null | 厂商 |
| `description` | Char(500) null | 说明 |
| `is_active/deleted_at` |  | 管理状态 |

约束：

- `UniqueConstraint(fields=["factory", "model_code"])`

`vfd_logical_points`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `device_model` | FK `vfd_device_models` | 所属设备型号 |
| `logical_key` | Char(100) | 逻辑点位，例如 `Vfd:Voltage` |
| `display_name` | Char(200) | 显示名 |
| `source` | Char(50) | `Vfd` / `Instrument` / `Timer` |
| `access_mode` | Char(50) | `READ` / `WRITE` / `READ_WRITE` |
| `function_code` | Char(20) | Modbus 功能码，如 `03`、`06` |
| `register_address` | Char(50) | 地址，如 `0x1003` |
| `data_type` | Char(50) | `Decimal` / `Integer` / `String` / `Boolean` |
| `unit` | Char(50) null | 单位 |
| `scale_factor` | Decimal null | 比例系数 |
| `offset` | Decimal null | 偏移 |
| `decimal_places` | PositiveSmallInteger null | 小数位 |
| `description` | Char(500) null | 说明 |
| `is_custom` | Bool | 是否自定义 |
| `is_active/deleted_at` |  | 管理状态 |

约束：

- `UniqueConstraint(fields=["factory", "device_model", "logical_key"])`
- `logical_key` 建议保持 WPF 当前使用的稳定值，例如 `Vfd:Control`、`Vfd:State`、`Vfd:Voltage`、`Instrument:Voltage`。
- `COMPARE_MEASUREMENT` 不是逻辑点位，不能写入此表。

`vfd_logical_point_write_options`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `logical_point` | FK `vfd_logical_points` | 所属写点 |
| `value` | Char(100) | 写入值，如 `1`、`6` |
| `display_text` | Char(200) | 显示文本，如“启动”、“停止” |
| `sort_order` | Integer | 排序 |
| `is_active/deleted_at` |  | 管理状态 |

约束：

- 同一逻辑点位下 `value` 唯一。

### 3.4 测试方案、版本与步骤

`vfd_process_plans`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `plan_code` | Char(100) | 方案编码，同工厂唯一 |
| `name` | Char(200) | 方案名称 |
| `product` | FK `product.Product` null | 适用产品。正常新建方案建议必填，允许为空以兼容历史和通用方案 |
| `process` | FK `product.Process` null | 所属工序。VFD 测试纳入工艺路线时建议必填 |
| `description` | Char(500) null | 说明 |
| `is_active/deleted_at` |  | 管理状态 |

建议：

- 如果当前产品资料还不稳定，可先允许 `product`、`process` 为空。
- 不建议复用 `quality.InspectionPlan`。VFD 测试方案是可执行流程，不只是质检计划。

`vfd_process_plan_versions`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `process_plan` | FK `vfd_process_plans` | 所属方案 |
| `version_number` | PositiveInteger | 版本号 |
| `status` | Char(20) | `DRAFT` / `PUBLISHED` / `ARCHIVED` |
| `is_executable` | Bool | 是否当前可执行 |
| `validated_at` | DateTime null | 最近校验通过时间 |
| `published_at` | DateTime null | 发布时间 |
| `published_by` | FK `user.SysUser` null | 发布人 |
| `source_version` | FK self null | 从哪个版本复制 |
| `remark` | Char(500) null | 备注 |

约束：

- `UniqueConstraint(fields=["factory", "process_plan", "version_number"])`
- 同一个 `process_plan` 只能有一个 `is_executable=True` 且 `status="PUBLISHED"` 的版本。SQL Server 可用事务保证；如部分索引支持有限，发布服务里加锁处理。
- 发布后不可修改步骤，修改必须复制为新草稿。

`vfd_process_steps`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `plan_version` | FK `vfd_process_plan_versions` | 所属版本 |
| `sequence` | PositiveInteger | 步骤序号 |
| `name` | Char(200) | 步骤名称 |
| `step_type` | Char(50) | `START`、`STOP`、`DELAY`、`READ_MEASUREMENT`、`READ_STRING`、`COMPARE_MEASUREMENT` |
| `target_point` | FK `vfd_logical_points` null | 单点步骤目标点 |
| `target_point_key` | Char(100) null | 目标点 key 快照 |
| `command_value` | Char(200) null | 写入值或 delay 毫秒数 |
| `compare_left_point` | FK `vfd_logical_points` null | 比对左侧点 |
| `compare_left_point_key` | Char(100) null | 左侧 key 快照 |
| `compare_right_point` | FK `vfd_logical_points` null | 比对右侧点 |
| `compare_right_point_key` | Char(100) null | 右侧 key 快照 |
| `tolerance_type` | Char(50) null | `Absolute` / `Percent` |
| `tolerance_value` | Decimal null | 容差值 |
| `rule_type` | Char(50) null | `NumericRange` / `StringEquals` |
| `lower_limit` | Decimal null | 下限 |
| `upper_limit` | Decimal null | 上限 |
| `expected_value` | Char(200) null | 期望字符串 |
| `failure_action` | Char(50) | 失败策略 |
| `max_retries` | PositiveInteger | 默认 0 |
| `affects_final_conclusion` | Bool | 是否影响最终结论 |
| `is_enabled` | Bool | 是否启用 |

约束：

- `UniqueConstraint(fields=["factory", "plan_version", "sequence"])`
- 后端保存时同步写入 `target_point_key`、`compare_left_point_key`、`compare_right_point_key`，作为配置快照。
- 后端校验发布版本不可修改。

### 3.5 执行追溯

追溯表不可被管理端普通 CRUD 修改。只允许 runtime 上报和管理端只读查询。

`vfd_station_sessions`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | WPF 生成或后端生成 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `station` | FK `vfd_stations` PROTECT | 工位 |
| `process_plan_version` | FK `vfd_process_plan_versions` PROTECT null | 使用版本 |
| `plan_version_snapshot` | JSON | 方案版本快照 |
| `operator_code` | Char(64) | WPF 操作员工号 |
| `operator_user` | FK `user.SysUser` null | 如能映射到系统用户则填写 |
| `started_at` | DateTime | 开始时间 |
| `ended_at` | DateTime null | 结束时间 |
| `conclusion` | Char(50) null | `Pass` / `Fail` / `Warning` / `None` |

`vfd_device_runs`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 设备执行 ID |
| `factory` | FK `core.Factory` | 所属工厂 |
| `session` | FK `vfd_station_sessions` CASCADE | 所属会话 |
| `slot` | FK `vfd_station_slots` PROTECT | 槽位 |
| `barcode` | Char(100) | 条码 |
| `work_order` | FK `production.WorkOrder` null | 关联工单。正常生产测试建议必填，离线调试可为空 |
| `product` | FK `product.Product` null | 冗余关联产品，便于按产品查询追溯 |
| `conclusion` | Char(50) | 结论 |
| `started_at` | DateTime | 开始 |
| `completed_at` | DateTime null | 完成 |

索引：

- `barcode`
- `started_at`
- `factory, started_at`
- `factory, conclusion`

`vfd_step_runs`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 步骤执行 ID |
| `factory` | FK `core.Factory` | 所属工厂 |
| `device_run` | FK `vfd_device_runs` CASCADE | 所属设备执行 |
| `process_step` | FK `vfd_process_steps` SET_NULL | 原步骤 |
| `sequence` | PositiveInteger | 序号快照 |
| `step_name` | Char(200) | 名称快照 |
| `step_type` | Char(50) | 类型快照 |
| `step_config_snapshot` | JSON | 目标点、命令值、规则、失败策略快照 |
| `conclusion` | Char(50) | 结论 |
| `message` | Char(500) null | 消息 |
| `started_at` | DateTime | 开始 |
| `completed_at` | DateTime null | 完成 |

`vfd_measurement_results`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | BigAutoField | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `step_run` | FK `vfd_step_runs` CASCADE | 步骤 |
| `point_key` | Char(100) | 点位 key |
| `source` | Char(50) | `Vfd` / `Instrument` |
| `numeric_value` | Decimal null | 数值 |
| `text_value` | Char(200) null | 文本 |
| `unit` | Char(50) null | 单位 |
| `conclusion` | Char(50) null | 单项结论 |
| `message` | Char(500) null | 消息 |

`vfd_comparison_results`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | BigAutoField | 主键 |
| `factory` | FK `core.Factory` | 所属工厂 |
| `step_run` | FK `vfd_step_runs` CASCADE | 步骤 |
| `left_key` | Char(100) | 左点位 |
| `right_key` | Char(100) | 右点位 |
| `primary_value` | Decimal null | 主值 |
| `reference_value` | Decimal null | 参考值 |
| `difference_value` | Decimal null | 差值 |
| `difference_percent` | Decimal null | 百分比 |
| `tolerance_type` | Char(50) null | 容差类型 |
| `tolerance_value` | Decimal null | 容差 |
| `conclusion` | Char(50) | 结论 |
| `message` | Char(500) null | 消息 |

`vfd_command_traces`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 命令追踪 ID |
| `factory` | FK `core.Factory` | 所属工厂 |
| `step_run` | FK `vfd_step_runs` CASCADE | 步骤 |
| `slot` | FK `vfd_station_slots` PROTECT | 槽位 |
| `command_name` | Char(100) | 命令名 |
| `target_point_key` | Char(100) null | 目标点 |
| `request_json` | JSON | 请求 |
| `response_json` | JSON | 响应 |
| `is_success` | Bool | 是否成功 |
| `error_code` | Char(100) null | 错误码 |
| `message` | Char(500) null | 消息 |
| `created_at` | DateTime | 时间 |

### 3.6 审计日志

`vfd_audit_logs`

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | UUID PK | 主键 |
| `factory` | FK `core.Factory` null | 所属工厂 |
| `operator` | FK `user.SysUser` null | 操作人 |
| `action` | Char(100) | 动作 |
| `target_type` | Char(100) | 目标类型 |
| `target_id` | Char(100) | 目标 ID |
| `before_json` | JSON null | 修改前 |
| `after_json` | JSON null | 修改后 |
| `created_at` | DateTime | 时间 |

必须审计：

- 方案、版本、步骤的新增、修改、删除、复制、重排、校验、发布。
- 设备型号、逻辑点位、写入选项、工位、槽位的新增、修改、删除、启停用。
- 未来如果允许追溯纠错，纠错必须单独审计；一期不允许普通修改追溯。

## 4. API 设计

### 4.1 路由总览

```text
/api/vfd/admin/stations/
/api/vfd/admin/station-slots/
/api/vfd/admin/device-models/
/api/vfd/admin/logical-points/
/api/vfd/admin/logical-point-write-options/
/api/vfd/admin/process-plans/
/api/vfd/admin/process-plan-versions/
/api/vfd/admin/process-steps/
/api/vfd/admin/station-sessions/
/api/vfd/admin/device-runs/

/api/vfd/runtime/process-plan-versions/
/api/vfd/runtime/process-plan-versions/executable/
/api/vfd/runtime/stations/{id}/
/api/vfd/runtime/logical-points/
/api/vfd/runtime/device-runs/
```

### 4.2 管理端接口

所有管理端写接口要求登录。沿用当前项目 JWT：

```http
Authorization: Bearer <token>
```

管理端 ViewSet 风格：

- CRUD 使用 `ModelViewSet`。
- 追溯查询使用 `ReadOnlyModelViewSet`。
- 查询支持 `django_filters`，字段风格沿用现有项目的 `filterset_fields` 和 `search_fields`。
- 删除默认软删除：设置 `deleted_at` 或 `is_active=false`。仅未发布、未引用草稿数据允许物理删除。

#### 方案动作

| 动作 | API |
| --- | --- |
| 复制方案 | `POST /api/vfd/admin/process-plans/{id}/copy/` |
| 复制版本为草稿 | `POST /api/vfd/admin/process-plan-versions/{id}/copy-as-draft/` |
| 校验版本 | `POST /api/vfd/admin/process-plan-versions/{id}/validate/` |
| 发布版本 | `POST /api/vfd/admin/process-plan-versions/{id}/publish/` |
| 归档版本 | `POST /api/vfd/admin/process-plan-versions/{id}/archive/` |
| 重排步骤 | `POST /api/vfd/admin/process-plan-versions/{id}/reorder-steps/` |
| 复制步骤 | `POST /api/vfd/admin/process-steps/{id}/copy/` |

#### 管理端过滤建议

| 资源 | 搜索字段 | 过滤字段 |
| --- | --- | --- |
| stations | `station_code`, `name` | `factory`, `is_active` |
| station-slots | `display_name`, `port_name` | `factory`, `station`, `is_enabled`, `is_active` |
| device-models | `model_code`, `name` | `factory`, `device_type`, `is_active` |
| logical-points | `logical_key`, `display_name` | `factory`, `device_model`, `source`, `access_mode`, `data_type`, `is_active` |
| process-plans | `plan_code`, `name` | `factory`, `product`, `process`, `is_active` |
| process-plan-versions |  | `factory`, `process_plan`, `status`, `is_executable` |
| process-steps | `name`, `target_point_key` | `factory`, `plan_version`, `step_type`, `is_enabled` |
| device-runs | `barcode` | `factory`, `station_id`, `slot_id`, `operator_code`, `process_plan_version_id`, `conclusion`, `started_from`, `started_to` |

### 4.3 Runtime 接口

Runtime 接口只给 WPF 使用。

#### 获取可执行方案

```http
GET /api/vfd/runtime/process-plan-versions/executable/?factory_id={factory_id}
```

响应建议：

```json
[
  {
    "id": "version-id",
    "process_plan": {
      "id": "plan-id",
      "plan_code": "VFD-TEST-001",
      "name": "VFD 启停测试"
    },
    "version_number": 1,
    "steps": [
      {
        "id": "step-id",
        "sequence": 1,
        "name": "Start VFD",
        "step_type": "START",
        "target_point_key": "Vfd:Control",
        "command_value": "1",
        "failure_action": "STOP_SLOT_IMMEDIATELY",
        "max_retries": 0,
        "affects_final_conclusion": true
      }
    ]
  }
]
```

#### 获取工位配置

```http
GET /api/vfd/runtime/stations/{id}/
```

响应必须包含启用槽位：

```json
{
  "id": "station-id",
  "station_code": "ST-01",
  "name": "一号测试工位",
  "slots": [
    {
      "id": "slot-id",
      "slot_number": 1,
      "display_name": "1 号槽位",
      "port_name": "COM3",
      "baud_rate": 9600,
      "vfd_address": 1,
      "voltage_meter_address": 11,
      "current_meter_address": 21
    }
  ]
}
```

#### 获取逻辑点位

```http
GET /api/vfd/runtime/logical-points/?factory_id={factory_id}
```

只返回启用点位和启用写入选项。

#### 上报追溯

```http
POST /api/vfd/runtime/device-runs/
```

后端用 `transaction.atomic()` 保存：

1. `vfd_station_sessions`
2. `vfd_device_runs`
3. `vfd_step_runs`
4. `vfd_measurement_results`
5. `vfd_comparison_results`
6. `vfd_command_traces`

幂等建议：

- WPF 传入 `session.id`、`device_run.id`、`steps[].id`。
- 如果 `device_run.id` 已存在，后端返回已有记录或 409。建议一期返回 409，避免重复写入。

## 5. WPF DTO 映射

当前 WPF 使用：

```csharp
StepCommand(CommandType, Target, Value)
StepFailurePolicy(FailureAction, MaxRetries)
StepRule(...)
```

后端使用扁平字段，HTTP 仓储负责转换。

| 后端字段 | WPF 字段 |
| --- | --- |
| `step_type=START` | `CommandType="Start"` |
| `step_type=STOP` | `CommandType="Stop"` |
| `step_type=DELAY` | `CommandType="Delay"` |
| `step_type=READ_MEASUREMENT` | `CommandType="ReadMeasurement"` |
| `step_type=READ_STRING` | `CommandType="ReadString"` |
| `step_type=COMPARE_MEASUREMENT` | `CommandType="CompareMeasurement"` |
| `target_point_key` | `Target` |
| `command_value` | `Value` |
| `compare_left_point_key + compare_right_point_key` | `Target="{left}|{right}"` |
| `tolerance_type + tolerance_value` | `Value="{type}:{value}"` |
| `rule_type/lower_limit/upper_limit` | `StepRule.NumericRange(...)` |
| `rule_type/expected_value` | `StepRule.StringEquals(...)` |

失败策略映射：

| 后端 | WPF |
| --- | --- |
| `CONTINUE_AND_MARK_FAIL` | `ContinueAndMarkFail` |
| `CONTINUE_AS_WARNING` | `ContinueAsWarning` |
| `STOP_SLOT_IMMEDIATELY` | `StopSlotImmediately` |
| `PAUSE_ALL_SLOTS` | `PauseAllSlots` |
| `RETRY_THEN_STOP` | `RetryThenStop` |
| `REQUIRE_OPERATOR_CONFIRM` | `RequireOperatorConfirm` |

## 6. WPF 接入设计

### 6.1 `HttpProcessPlanRepository`

实现当前接口：

```csharp
IProcessPlanRepository
```

职责：

- `ListExecutableVersionsAsync` 调用 `/api/vfd/runtime/process-plan-versions/executable/`。
- `ListAsync` 可基于可执行版本响应聚合出方案列表。
- `GetAsync` 调用版本详情或方案详情。
- `SaveAsync` 在 WPF 操作端禁用，返回业务错误或抛出 `NotSupportedException`。

### 6.2 `HttpTraceRepository`

当前 WPF 的 `ITraceRepository` 是细粒度保存接口。后端 runtime 上报设计为一次性事务写入，因此 HTTP 版仓储建议做内存聚合：

- `SaveSessionStartedAsync`：缓存 session。
- `SaveDeviceRunAsync`：缓存 device run。
- `SaveStepRunAsync`：缓存 step。
- `SaveMeasurementResultAsync`：挂到对应 step。
- `SaveComparisonResultAsync`：挂到对应 step。
- `SaveCommandTraceAsync`：挂到对应 step。
- 当 device run 完成或调度器明确结束时，调用 `POST /api/vfd/runtime/device-runs/`。

如果一期难以准确判断提交时机，可以在 WPF 现有执行服务完成后新增显式 `FlushDeviceRunAsync(deviceRunId)`。

## 7. 后端服务设计

建议把复杂规则放到 `vfd_control/services.py`，ViewSet 只负责编排输入输出。

### 7.1 `ProcessPlanVersionService`

职责：

- 创建首个草稿版本。
- 从已发布或草稿版本复制新草稿。
- 校验版本。
- 发布版本。
- 归档版本。
- 重排步骤。

发布使用事务：

1. 查询版本和步骤，加锁。
2. 校验版本必须是草稿。
3. 校验步骤完整性。
4. 将同一方案其他版本 `is_executable=False`。
5. 当前版本设为 `status="PUBLISHED"`、`is_executable=True`、`published_at=now`。
6. 写 `vfd_audit_logs`。

### 7.2 `RuntimeTraceIngestService`

职责：

- 校验上报 JSON。
- 校验 station、slot、version 属于同一 factory。
- 校验 slot 属于 station。
- 幂等检查 device_run id。
- 使用事务保存完整追溯。
- 保存步骤配置快照，确保后续方案修改不影响历史查询。

### 7.3 `AuditService`

职责：

- 接收 `request.user`。
- 写入 action、target、before、after。
- 不依赖 Django admin。

## 8. 校验规则

### 8.1 步骤保存校验

| 步骤类型 | 校验 |
| --- | --- |
| `START` / `STOP` | 必须选择可写或读写点位，必须有 `command_value`。 |
| `DELAY` | `command_value` 必须是大于等于 0 的整数毫秒。 |
| `READ_MEASUREMENT` | 必须选择可读或读写点位；如填上下限，至少一个不为空。 |
| `READ_STRING` | 必须选择可读或读写点位；`expected_value` 可空。 |
| `COMPARE_MEASUREMENT` | 左右点位必填、不能相同，必须是数值可读点位，必须有容差类型和值。 |

### 8.2 发布校验

- 至少有一个启用步骤。
- 启用步骤序号不重复。
- 所有引用点位存在且启用。
- 发布版本不可再修改。
- 同一方案同一时刻只能有一个可执行版本。

### 8.3 删除校验

- 已发布版本不能物理删除。
- 已产生追溯的方案、版本、步骤、工位、槽位不能物理删除。
- 被步骤引用的逻辑点位不能物理删除，只能停用。

## 9. 数据初始化

新增命令：

```powershell
python manage.py seed_vfd_defaults --factory-id <factory_id>
```

初始化内容：

- `vfd_device_models`
  - `VFD_DEFAULT`
  - `METER_DEFAULT`
- `vfd_logical_points`
  - `Vfd:Control`
  - `Vfd:State`
  - `Vfd:Voltage`
  - `Instrument:Voltage`
- `vfd_logical_point_write_options`
  - `Vfd:Control` 的启动值 `1`
  - `Vfd:Control` 的停止值 `6`

命令必须幂等：

- 已存在同 `factory + model_code` 不重复创建。
- 已存在同 `factory + device_model + logical_key` 不重复创建。
- 已存在同 `logical_point + value` 不重复创建。

## 10. 与现有表的关系建议

### 10.1 参与原则

已有 MES 表确定参与 VFD 子域，但参与方式必须受控：

```text
已有表 = 主数据、生产上下文、资产上下文、条码上下文、汇总结果
VFD 新表 = 测试方案、测试步骤、通信点位、步骤级追溯
```

也就是说，已有表不承载 VFD 测试流程定义，也不承载每一步通信明细；这些仍由 `vfd_` 新表负责。

### 10.2 确定参与的已有表

| 已有表 | 参与方式 | 写入策略 |
| --- | --- | --- |
| `core_factories` | 所有 VFD 主数据、方案、追溯均带 `factory_id` | VFD 表外键引用，不反向改工厂表 |
| `products` | 测试方案适用产品；执行追溯冗余产品 | VFD 表外键引用 |
| `processes` | 测试方案所属工序；VFD 测试纳入产品工艺路线 | VFD 表外键引用 |
| `work_orders` | 某次 VFD 测试所属工单 | `vfd_device_runs.work_order` 外键引用 |
| `equipment` | 测试台、仪表、变频器等作为设备资产 | 槽位或槽位设备绑定表外键引用 |
| `sys_user` | 管理端审计、创建人、更新人 | VFD 表外键引用 |
| `barcode_relations` | 查询主条码、子条码关系 | 只读查询，不写步骤明细 |
| `barcode_scans` | 记录条码进入或完成 VFD 测试工序的扫描事件 | 只写测试工序事件，不写每个测试步骤 |
| `production_data` | 写入最终测试结论和关键汇总参数 | 只写汇总值，不写完整追溯 |

### 10.3 推荐关联字段

在 VFD 新表上增加这些外键或字段：

- `vfd_process_plans.product -> product.Product`
- `vfd_process_plans.process -> product.Process`
- `vfd_device_runs.work_order -> production.WorkOrder`
- `vfd_device_runs.product -> product.Product`
- `vfd_station_slots.equipment -> equipment.Equipment`
- 可选新增 `vfd_slot_equipment_bindings.equipment -> equipment.Equipment`
- `vfd_audit_logs.operator -> user.SysUser`
- `vfd_process_plan_versions.published_by -> user.SysUser`

### 10.4 `barcode_scans` 使用边界

`barcode_scans` 可以记录这些事件：

- 条码进入 VFD 测试工序。
- 条码完成 VFD 测试工序。
- 条码被 WPF 操作端扫描并绑定到工单、工序、槽位。

`barcode_scans.parameters` 可以写轻量 JSON，例如：

```json
{
  "source": "VFD_WPF",
  "station_id": "station-id",
  "slot_id": "slot-id",
  "device_run_id": "device-run-id",
  "event": "VFD_TEST_STARTED"
}
```

不要把每个测试步骤、每次 Modbus 请求响应、测量明细写入 `barcode_scans.parameters`。这些数据写入 `vfd_step_runs`、`vfd_measurement_results`、`vfd_comparison_results`、`vfd_command_traces`。

### 10.5 `production_data` 使用边界

VFD 测试完成后，可以把最终结论和关键参数写入 `production_data`，便于现有 MES 报表直接使用。

推荐写入示例：

| parameter_name | parameter_value |
| --- | --- |
| `VFD_TEST_CONCLUSION` | `Pass` / `Fail` / `Warning` |
| `VFD_OUTPUT_VOLTAGE` | 实测输出电压 |
| `INSTRUMENT_VOLTAGE` | 仪表电压 |
| `VOLTAGE_DIFF` | 差值 |
| `VFD_TEST_DEVICE_RUN_ID` | 对应 `vfd_device_runs.id` |

`production_data` 不保存完整步骤追溯，只保存汇总指标和 VFD 追溯 ID。

### 10.6 不建议修改已有表结构

一期不建议修改这些已有表结构：

- `production_plans`
- `work_orders`
- `equipment`
- `products`
- `processes`
- `barcode_relations`
- `barcode_scans`
- `production_data`
- `meter_parameters`
- `inverter_parameters`

原因：

- 当前字段语义已经服务通用 MES。
- 通过 VFD 新表外键即可参与，不需要给老表加 VFD 专用字段。
- 避免影响已有页面、接口和报表。
- 如果后续报表性能需要，再考虑在现有汇总表上加索引或增加专用汇总表。

## 11. 安全与配置

### 11.1 数据库密码

当前 `settings.py` 中存在明文数据库密码。VFD 接入前建议迁移为环境变量：

```python
"NAME": os.getenv("DB_NAME", "LixiangMes")
"USER": os.getenv("DB_USER")
"PASSWORD": os.getenv("DB_PASSWORD")
"HOST": os.getenv("DB_HOST")
"PORT": os.getenv("DB_PORT")
```

### 11.2 权限

当前全局 `DEFAULT_PERMISSION_CLASSES` 被注释。VFD 接口应在 ViewSet 上显式声明：

```python
permission_classes = [IsAuthenticated]
```

Runtime 接口是否要求登录有两种方案：

1. 内网可信 + 工位密钥：给 WPF 配置固定 station token。
2. 继续使用 JWT：WPF 登录后带 Bearer token。

建议一期使用 JWT，和当前管理端一致。后续如果 WPF 现场不方便登录，再增加 station token。

### 11.3 CORS

当前 `CORS_ORIGIN_ALLOW_ALL=True`。正式部署建议改成白名单，只允许管理前端地址。

## 12. 测试清单

后端至少覆盖：

1. 管理员创建工位和槽位成功。
2. 槽位三个地址重复时保存失败。
3. 创建设备型号、逻辑点位、写入选项成功。
4. 创建方案、草稿版本、步骤成功。
5. 比对步骤缺少右侧点位时校验失败。
6. 发布无步骤版本失败。
7. 发布后同方案只有一个可执行版本。
8. 已发布版本不允许修改步骤。
9. 被步骤引用的逻辑点位不能物理删除。
10. Runtime 可执行方案接口只返回已发布且 `is_executable=True` 的版本。
11. Runtime 工位接口只返回启用槽位。
12. Runtime 追溯上报使用事务；故意构造错误步骤时不产生半成品 `DeviceRun`。
13. 按条码可查回步骤、测量、比对、命令追踪。
14. 管理端写操作生成 `vfd_audit_logs`。
15. 所有 VFD 管理端接口支持 `factory` 过滤。

WPF 至少覆盖：

1. `HttpProcessPlanRepository` 能把后端 `READ_MEASUREMENT` 映射为 `ReadMeasurement`。
2. `COMPARE_MEASUREMENT` 能映射为 `Target="left|right"` 和 `Value="Absolute:2"`。
3. `HttpTraceRepository` 能把细粒度保存调用聚合为一次上报 JSON。
4. 上报失败时给操作端明确错误，而不是静默丢失。

## 13. 实施顺序

1. 新建 `vfd_control` app，注册到 `INSTALLED_APPS`。
2. 在 `LXMES/urls.py` 增加 `path("api/vfd/", include("vfd_control.urls"))`。
3. 定义 `VfdAdminManagedModel`、核心模型和 migration。
4. 实现 `seed_vfd_defaults --factory-id`。
5. 实现设备型号、逻辑点位、写入选项、工位、槽位管理端 CRUD。
6. 实现方案、版本、步骤 CRUD 和复制、重排、校验、发布动作。
7. 实现 runtime 可执行方案、工位配置、逻辑点位接口。
8. 实现 runtime 追溯批量上报接口和事务保存。
9. 实现管理端追溯只读查询。
10. WPF 新增 `HttpProcessPlanRepository`。
11. WPF 新增 `HttpTraceRepository` 或显式 flush 机制。
12. 管理前端接入 VFD 维护页面和追溯页面。
13. 收敛安全配置：数据库环境变量、CORS 白名单、显式权限。

## 14. 最终建议

推荐方案是：

```text
同库 LixiangMes
新增 vfd_control app
新增 vfd_ 前缀表承载测试方案、步骤、点位和详细追溯
明确复用 core_factories、products、processes、work_orders、equipment、sys_user、barcode_relations、barcode_scans
barcode_scans 只记录 VFD 测试工序事件，不记录步骤明细
production_data 写最终结论和关键汇总参数
短期沿用 user.SysUser 做审计
所有 VFD 主数据带 factory
API 使用 /api/vfd/admin 与 /api/vfd/runtime
WPF 通过 HttpProcessPlanRepository 读取方案
WPF 通过批量型 HttpTraceRepository 上报追溯
```

这个方案让已有 MES 表参与生产上下文和结果汇总，同时把 VFD 测试流程、通信点位和步骤级追溯留在独立子域内，既能复用现有结构，又能避免污染原有生产、设备、条码和质量模块。
