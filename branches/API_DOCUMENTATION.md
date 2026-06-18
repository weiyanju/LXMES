# LXMES 接口文档

## 1. 基本说明

- 项目类型：Django + Django REST Framework
- 接口根路径：`/api`
- 认证方式：JWT Bearer Token
- 默认分页：`page=1`，`page_size=10`
- 媒体文件访问：`/media/<path>`

## 2. 认证说明

大部分 `ViewSet` 接口要求登录后访问，请在请求头中携带：

```http
Authorization: Bearer <access_token>
Content-Type: application/json
```

登录成功后可获得：

- `token`：访问令牌
- `refresh`：刷新令牌

### 2.1 登录与令牌

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `POST` | `/api/user/login?username=<username>&password=<password>` | 登录 |
| `POST` | `/api/token/refresh/` | 刷新 access token |
| `GET` | `/api/user/test` | 测试接口 |
| `GET` | `/api/user/jwt_test` | 生成测试 token |

### 2.2 登录响应示例

```json
{
  "code": 200,
  "token": "access_token",
  "refresh": "refresh_token",
  "user": {
    "id": 1,
    "username": "admin"
  },
  "roles": "admin",
  "menuList": []
}
```

## 3. 通用规则

### 3.1 Router 生成的标准接口

除显式说明外，所有 `ModelViewSet` 资源均包含以下标准接口：

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `GET` | `/<resource>/` | 分页列表 |
| `POST` | `/<resource>/` | 新增 |
| `GET` | `/<resource>/{id}/` | 详情 |
| `PUT` | `/<resource>/{id}/` | 全量更新 |
| `PATCH` | `/<resource>/{id}/` | 部分更新 |
| `DELETE` | `/<resource>/{id}/` | 删除 |

### 3.2 通用分页返回

多数列表接口返回 DRF 标准分页结构：

```json
{
  "count": 100,
  "next": "http://host/api/xxx/?page=2",
  "previous": null,
  "results": []
}
```

### 3.3 通用查询参数

- `page`：页码
- `page_size`：每页数量
- `query`：模糊搜索关键字，部分模块支持

## 4. 用户模块 `/api/user`

### 4.1 标准资源

资源路径：`/api/user/users/`

主要字段：

- `username`
- `password`
- `avatar`
- `departments`
- `email`
- `phonenumber`
- `status`
- `remark`

补充只读字段：

- `id`
- `create_time`
- `update_time`
- `login_date`
- `roles`

### 4.2 扩展接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `POST` | `/api/user/users/check-username/` | 校验用户名是否已存在 |
| `POST` | `/api/user/users/{id}/reset-password/` | 重置密码为 `123456` |
| `POST` | `/api/user/users/{id}/change-password/` | 修改密码 |
| `PATCH` | `/api/user/users/{id}/toggle-status/` | 启停账号 |
| `POST` | `/api/user/users/{id}/grant-roles/` | 分配角色 |
| `POST` | `/api/user/users/upload-avatar/` | 上传头像 |
| `PATCH` | `/api/user/users/update-avatar/` | 更新当前登录用户头像 |
| `DELETE` | `/api/user/users/batch-delete/` | 批量删除用户 |

### 4.3 请求示例

修改密码：

```json
{
  "oldPassword": "123456",
  "newPassword": "654321"
}
```

分配角色：

```json
{
  "roleIds": [1, 2]
}
```

批量删除：

```json
{
  "ids": [3, 4, 5]
}
```

## 5. 角色模块 `/api/role`

### 5.1 兼容旧接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `GET` | `/api/role/listAll` | 查询全部角色 |
| `POST` | `/api/role/search` | 按名称分页搜索角色 |
| `POST` | `/api/role/save` | 新增或修改角色 |
| `GET` | `/api/role/action?id={id}` | 角色详情 |
| `DELETE` | `/api/role/action` | 删除角色 |
| `GET` | `/api/role/menus?id={id}` | 查询角色菜单权限 |
| `POST` | `/api/role/grant` | 角色授权菜单 |

### 5.2 标准资源

资源路径：`/api/role/roles/`

主要字段：

- `name`
- `code`
- `remark`

### 5.3 扩展接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `DELETE` | `/api/role/roles/batch-delete/` | 批量删除角色 |
| `GET` | `/api/role/roles/{id}/menus/` | 查询角色已有菜单 ID |
| `POST` | `/api/role/roles/{id}/grant-menus/` | 分配角色菜单 |

授权菜单请求体：

```json
{
  "menuIds": [1, 2, 3]
}
```

## 6. 菜单模块 `/api/menu`

### 6.1 兼容旧接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `GET` | `/api/menu/treeList` | 获取当前用户菜单树 |
| `POST` | `/api/menu/save` | 新增或修改菜单 |
| `GET` | `/api/menu/action?id={id}` | 菜单详情 |
| `DELETE` | `/api/menu/action` | 删除菜单 |

### 6.2 标准资源

资源路径：`/api/menu/menus/`

主要字段：

- `name`
- `icon`
- `parent_id`
- `order_num`
- `path`
- `component`
- `menu_type`
- `perms`
- `remark`

### 6.3 扩展接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `DELETE` | `/api/menu/menus/batch-delete/` | 批量删除菜单 |
| `GET` | `/api/menu/menus/tree/` | 完整菜单树 |
| `GET` | `/api/menu/menus/user-tree/` | 当前登录用户菜单树 |
| `GET` | `/api/menu/menus/role-tree/?role_id={id}` | 指定角色菜单树 |

## 7. 大屏统计模块 `/api/screen`

这些接口均为只读 `GET` 接口，无分页：

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `GET` | `/api/screen/defect-records/` | 不良记录列表 |
| `GET` | `/api/screen/top5-performance/` | 员工绩效 Top5 |
| `GET` | `/api/screen/sales-order-ratio/` | 销售订单状态占比 |
| `GET` | `/api/screen/defect-pie/` | 不良类型饼图数据 |
| `GET` | `/api/screen/process-top5/` | 工序计划 Top5 |
| `GET` | `/api/screen/work-orders/` | 工单列表 |

## 8. 部门模块 `/api/department`

资源路径：`/api/department/departments/`

主要字段：

- `name`
- `remark`

扩展接口：

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `DELETE` | `/api/department/departments/batch-delete/` | 批量删除部门 |

## 9. 条码录入模块 `/api/barcode`

资源路径：`/api/barcode/records/`

主要字段：

- `code`
- `scan_time`
- `remark`

## 10. 基础资料模块 `/api/core`

### 10.1 工厂

资源路径：`/api/core/factories/`

主要字段：

- `factory_code`
- `factory_name`
- `address`
- `contact_person`
- `contact_phone`
- `status`

### 10.2 客户

资源路径：`/api/core/customers/`

主要字段：

- `customer_code`
- `customer_name`
- `contact_person`
- `contact_phone`
- `email`
- `address`
- `status`

### 10.3 部门

资源路径：`/api/core/departments/`

主要字段：

- `department_code`
- `department_name`
- `parent`
- `status`
- `factory`

### 10.4 员工

资源路径：`/api/core/employees/`

主要字段：

- `employee_code`
- `name`
- `department`
- `position`
- `phone`
- `status`
- `factory`

### 10.5 用户

资源路径：`/api/core/users/`

主要字段：

- `username`
- `name`
- `email`
- `role`
- `department`
- `factory`
- `is_active`
- `password`

## 11. 产品模块 `/api/product`

### 11.1 产品

资源路径：`/api/product/products/`

主要字段：

- `product_code`
- `product_name`
- `product_type`
- `specification`
- `product_image`
- `status`
- `factory`

### 11.2 工序

资源路径：`/api/product/processes/`

主要字段：

- `process_code`
- `process_name`
- `description`
- `status`
- `factory`

### 11.3 产品工序关系

资源路径：`/api/product/product-processes/`

主要字段：

- `product`
- `process`
- `sequence`

### 11.4 工艺文件

资源路径：`/api/product/process-files/`

主要字段：

- `file_code`
- `file_name`
- `version`
- `file_path`
- `file_size`
- `description`
- `product_type`
- `status`
- `uploaded_by`
- `factory`

## 12. 设备模块 `/api/equipment`

| 资源 | 路径 |
| --- | --- |
| 设备档案 | `/api/equipment/equipment/` |
| 设备状态 | `/api/equipment/equipment-status/` |
| 维护计划 | `/api/equipment/maintenance-plans/` |
| 维护记录 | `/api/equipment/maintenance-records/` |
| 故障记录 | `/api/equipment/equipment-faults/` |

关键业务字段：

- 设备档案：`equipment_code`、`equipment_name`、`type`、`model`、`specification`、`manufacturer`、`supplier`、`status`、`location`
- 设备状态：`equipment`、`status`、`description`、`operator`
- 维护计划：`plan_code`、`plan_name`、`equipment`、`maintenance_type`、`scheduled_date`、`estimated_duration`、`status`、`assignee`
- 维护记录：`plan`、`equipment`、`maintenance_type`、`maintenance_date`、`duration`、`maintenance_content`、`maintenance_result`、`maintenance_by`
- 故障记录：`fault_code`、`equipment`、`fault_description`、`fault_time`、`fault_level`、`status`、`repair_content`、`repair_time`、`repair_by`

## 13. 生产模块 `/api/production`

| 资源 | 路径 |
| --- | --- |
| 生产计划 | `/api/production/plans/` |
| 工单 | `/api/production/work-orders/` |
| 条码关联 | `/api/production/barcode-relations/` |
| 条码扫描 | `/api/production/barcode-scans/` |
| 生产数据 | `/api/production/production-data/` |

关键业务字段：

- 生产计划：`plan_name`、`product_type`、`quantity`、`start_barcode`、`end_barcode`、`start_date`、`end_date`、`demand_date`、`source`、`status`、`customer`、`created_by`、`remark`
- 工单：`order_number`、`plan`、`product_type`、`quantity`、`start_barcode`、`end_barcode`、`status`、`demand_date`、`process_file`
- 条码关联：`main_barcode`、`main_barcode_type`、`sub_barcode`、`sub_barcode_type`、`product`、`work_order`
- 条码扫描：`barcode`、`barcode_type`、`work_order`、`process`、`scanner`、`scanning_location`、`parameters`
- 生产数据：`work_order`、`equipment`、`product_code`、`parameter_name`、`parameter_value`、`timestamp`、`operator`

## 14. 参数模块 `/api/parameter`

| 资源 | 路径 |
| --- | --- |
| 电表参数 | `/api/parameter/meter/` |
| 逆变器参数 | `/api/parameter/inverter/` |

关键业务字段：

- 电表参数：`product_code`、`voltage`、`current`、`power`、`frequency`、`accuracy`、`factory`
- 逆变器参数：`product_code`、`input_voltage`、`output_voltage`、`input_current`、`output_current`、`power`、`frequency_range`、`factory`

## 15. 质量模块 `/api/quality`

| 资源 | 路径 |
| --- | --- |
| 检验方案 | `/api/quality/plans/` |
| 检验任务 | `/api/quality/tasks/` |
| 来料检验 | `/api/quality/incoming/` |
| 过程检验 | `/api/quality/process/` |
| 退料检验 | `/api/quality/return/` |
| 出货检验 | `/api/quality/shipping/` |

关键业务字段：

- 检验方案：`plan_code`、`plan_name`、`inspection_type`、`product`、`status`、`factory`
- 检验任务：`task_code`、`inspection_type`、`plan`、`product`、`batch_number`、`quantity`、`status`、`assignee`
- 来料检验：`task`、`supplier`、`material_code`、`batch_number`、`inspection_result`、`inspector`、`inspection_time`
- 过程检验：`task`、`work_order`、`process`、`batch_number`、`inspection_result`、`inspector`、`inspection_time`
- 退料检验：`task`、`material_code`、`batch_number`、`return_reason`、`inspection_result`、`inspector`、`inspection_time`
- 出货检验：`task`、`order`、`product`、`batch_number`、`quantity`、`inspection_result`、`inspector`、`inspection_time`

## 16. 批次追踪模块 `/api/batch-tracking`

资源路径：`/api/batch-tracking/batches/`

主要字段：

- `batch_number`
- `product`
- `quantity`
- `production_date`
- `expiry_date`
- `status`
- `factory`

补充只读字段：

- `product_name`
- `product_code`
- `factory_name`

## 17. 建议的联调顺序

1. 调用 `/api/user/login` 获取 `token`
2. 调用 `/api/menu/menus/user-tree/` 获取当前用户菜单
3. 按业务模块调用各资源列表接口
4. 使用 `POST/PUT/PATCH/DELETE` 完成增删改
5. 使用角色、菜单、用户扩展接口完成权限分配

## 18. 备注

- 本文档依据当前代码静态整理生成，未依赖 Swagger。
- 部分模块为标准 CRUD，字段以对应 `serializers.py` 与 `models.py` 为准。
- 旧接口与新 `ViewSet` 接口并存，建议新开发优先使用带资源名的 REST 风格接口。
