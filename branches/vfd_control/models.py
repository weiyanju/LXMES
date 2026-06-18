import uuid

from django.db import models

from core.base import FactoryScopedModel


class VfdDeviceModel(FactoryScopedModel):
    model_code = models.CharField(max_length=50, verbose_name='型号编码')
    model_name = models.CharField(max_length=100, verbose_name='型号名称')
    protocol = models.CharField(max_length=50, default='ModbusRtu', verbose_name='通信协议')
    baud_rate = models.PositiveIntegerField(default=9600, verbose_name='波特率')
    data_bits = models.PositiveSmallIntegerField(default=8, verbose_name='数据位')
    stop_bits = models.PositiveSmallIntegerField(default=1, verbose_name='停止位')
    parity = models.CharField(max_length=20, default='None', verbose_name='校验位')
    description = models.TextField(blank=True, verbose_name='描述')
    is_active = models.BooleanField(default=True, verbose_name='是否启用')

    class Meta:
        db_table = 'vfd_device_models'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'model_code'], name='unique_vfd_device_model_code'),
        ]
        verbose_name = 'VFD 设备型号'
        verbose_name_plural = 'VFD 设备型号'

    def __str__(self):
        return f'{self.model_code} - {self.model_name}'


class VfdLogicalPoint(FactoryScopedModel):
    device_model = models.ForeignKey(
        VfdDeviceModel,
        on_delete=models.CASCADE,
        related_name='logical_points',
        verbose_name='设备型号'
    )
    logical_key = models.CharField(max_length=100, verbose_name='逻辑点位 Key')
    name = models.CharField(max_length=100, verbose_name='点位名称')
    source = models.CharField(max_length=50, verbose_name='来源')
    data_type = models.CharField(max_length=50, verbose_name='数据类型')
    address = models.PositiveIntegerField(verbose_name='寄存器地址')
    function_code = models.PositiveSmallIntegerField(default=3, verbose_name='功能码')
    scale = models.DecimalField(max_digits=18, decimal_places=6, default=1, verbose_name='缩放系数')
    unit = models.CharField(max_length=50, blank=True, verbose_name='单位')
    is_writable = models.BooleanField(default=False, verbose_name='是否可写')
    description = models.TextField(blank=True, verbose_name='描述')
    is_active = models.BooleanField(default=True, verbose_name='是否启用')

    class Meta:
        db_table = 'vfd_logical_points'
        constraints = [
            models.UniqueConstraint(
                fields=['factory', 'device_model', 'logical_key'],
                name='unique_vfd_logical_point_key'
            ),
        ]
        verbose_name = 'VFD 逻辑点位'
        verbose_name_plural = 'VFD 逻辑点位'

    def __str__(self):
        return f'{self.logical_key} - {self.name}'


class VfdLogicalPointWriteOption(FactoryScopedModel):
    logical_point = models.ForeignKey(
        VfdLogicalPoint,
        on_delete=models.CASCADE,
        related_name='write_options',
        verbose_name='逻辑点位'
    )
    label = models.CharField(max_length=100, verbose_name='显示名称')
    value = models.CharField(max_length=100, verbose_name='写入值')
    sort_order = models.PositiveIntegerField(default=0, verbose_name='排序')
    is_default = models.BooleanField(default=False, verbose_name='是否默认')
    is_active = models.BooleanField(default=True, verbose_name='是否启用')

    class Meta:
        db_table = 'vfd_logical_point_write_options'
        constraints = [
            models.UniqueConstraint(
                fields=['factory', 'logical_point', 'value'],
                name='unique_vfd_logical_point_write_value'
            ),
        ]
        verbose_name = 'VFD 点位写入选项'
        verbose_name_plural = 'VFD 点位写入选项'

    def __str__(self):
        return f'{self.logical_point.logical_key} - {self.label}'


class VfdStation(FactoryScopedModel):
    station_code = models.CharField(max_length=50, verbose_name='工位编码')
    station_name = models.CharField(max_length=100, verbose_name='工位名称')
    location = models.CharField(max_length=100, blank=True, verbose_name='位置')
    ip_address = models.CharField(max_length=50, blank=True, verbose_name='IP 地址')
    is_active = models.BooleanField(default=True, verbose_name='是否启用')

    class Meta:
        db_table = 'vfd_stations'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'station_code'], name='unique_vfd_station_code'),
        ]
        verbose_name = 'VFD 测试工位'
        verbose_name_plural = 'VFD 测试工位'

    def __str__(self):
        return f'{self.station_code} - {self.station_name}'


class VfdStationSlot(FactoryScopedModel):
    station = models.ForeignKey(
        VfdStation,
        on_delete=models.CASCADE,
        related_name='slots',
        verbose_name='测试工位'
    )
    slot_code = models.CharField(max_length=50, verbose_name='槽位编码')
    slot_name = models.CharField(max_length=100, verbose_name='槽位名称')
    equipment = models.ForeignKey(
        'equipment.Equipment',
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='vfd_station_slots',
        verbose_name='关联设备'
    )
    is_active = models.BooleanField(default=True, verbose_name='是否启用')

    class Meta:
        db_table = 'vfd_station_slots'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'station', 'slot_code'], name='unique_vfd_station_slot_code'),
        ]
        verbose_name = 'VFD 测试槽位'
        verbose_name_plural = 'VFD 测试槽位'

    def __str__(self):
        return f'{self.station.station_code} - {self.slot_code}'


class VfdProcessPlan(FactoryScopedModel):
    plan_code = models.CharField(max_length=50, verbose_name='方案编码')
    plan_name = models.CharField(max_length=100, verbose_name='方案名称')
    product = models.ForeignKey(
        'product.Product',
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='vfd_process_plans',
        verbose_name='适用产品'
    )
    process = models.ForeignKey(
        'product.Process',
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='vfd_process_plans',
        verbose_name='所属工序'
    )
    status = models.CharField(max_length=20, default='draft', verbose_name='状态')
    description = models.TextField(blank=True, verbose_name='描述')
    created_by = models.ForeignKey(
        'user.SysUser',
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='created_vfd_process_plans',
        verbose_name='创建人'
    )

    class Meta:
        db_table = 'vfd_process_plans'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'plan_code'], name='unique_vfd_process_plan_code'),
        ]
        verbose_name = 'VFD 测试方案'
        verbose_name_plural = 'VFD 测试方案'

    def __str__(self):
        return f'{self.plan_code} - {self.plan_name}'


class VfdProcessPlanVersion(FactoryScopedModel):
    plan = models.ForeignKey(
        VfdProcessPlan,
        on_delete=models.CASCADE,
        related_name='versions',
        verbose_name='测试方案'
    )
    version_no = models.PositiveIntegerField(verbose_name='版本号')
    version_name = models.CharField(max_length=100, blank=True, verbose_name='版本名称')
    status = models.CharField(max_length=20, default='draft', verbose_name='状态')
    config_snapshot = models.JSONField(default=dict, blank=True, verbose_name='配置快照')
    published_at = models.DateTimeField(null=True, blank=True, verbose_name='发布时间')
    published_by = models.ForeignKey(
        'user.SysUser',
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='published_vfd_process_plan_versions',
        verbose_name='发布人'
    )

    class Meta:
        db_table = 'vfd_process_plan_versions'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'plan', 'version_no'], name='unique_vfd_plan_version_no'),
        ]
        verbose_name = 'VFD 测试方案版本'
        verbose_name_plural = 'VFD 测试方案版本'

    def __str__(self):
        return f'{self.plan.plan_code} v{self.version_no}'


class VfdProcessStep(FactoryScopedModel):
    plan_version = models.ForeignKey(
        VfdProcessPlanVersion,
        on_delete=models.CASCADE,
        related_name='steps',
        verbose_name='方案版本'
    )
    sequence = models.PositiveIntegerField(verbose_name='步骤序号')
    name = models.CharField(max_length=100, verbose_name='步骤名称')
    step_type = models.CharField(max_length=50, verbose_name='步骤类型')
    target_point = models.ForeignKey(
        VfdLogicalPoint,
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='target_steps',
        verbose_name='目标点位'
    )
    command_value = models.CharField(max_length=200, blank=True, verbose_name='命令值')
    compare_left_point = models.ForeignKey(
        VfdLogicalPoint,
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='left_compare_steps',
        verbose_name='比对左点位'
    )
    compare_right_point = models.ForeignKey(
        VfdLogicalPoint,
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='right_compare_steps',
        verbose_name='比对右点位'
    )
    tolerance_type = models.CharField(max_length=50, blank=True, verbose_name='容差类型')
    tolerance_value = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='容差值')
    lower_limit = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='下限')
    upper_limit = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='上限')
    expected_value = models.CharField(max_length=200, blank=True, verbose_name='期望值')
    failure_action = models.CharField(max_length=50, default='FailDevice', verbose_name='失败策略')
    max_retries = models.PositiveIntegerField(default=0, verbose_name='最大重试次数')
    affects_final_conclusion = models.BooleanField(default=True, verbose_name='是否影响最终结论')
    is_enabled = models.BooleanField(default=True, verbose_name='是否启用')

    class Meta:
        db_table = 'vfd_process_steps'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'plan_version', 'sequence'], name='unique_vfd_step_sequence'),
        ]
        verbose_name = 'VFD 测试步骤'
        verbose_name_plural = 'VFD 测试步骤'

    def __str__(self):
        return f'{self.sequence}. {self.name}'


class VfdStationSession(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    station = models.ForeignKey(
        VfdStation,
        on_delete=models.PROTECT,
        related_name='sessions',
        verbose_name='测试工位'
    )
    process_plan_version = models.ForeignKey(
        VfdProcessPlanVersion,
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='sessions',
        verbose_name='方案版本'
    )
    plan_version_snapshot = models.JSONField(default=dict, blank=True, verbose_name='方案版本快照')
    operator_code = models.CharField(max_length=64, blank=True, verbose_name='操作员工号')
    operator_user = models.ForeignKey(
        'user.SysUser',
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='vfd_station_sessions',
        verbose_name='操作用户'
    )
    started_at = models.DateTimeField(verbose_name='开始时间')
    ended_at = models.DateTimeField(null=True, blank=True, verbose_name='结束时间')
    conclusion = models.CharField(max_length=50, blank=True, verbose_name='结论')

    class Meta:
        db_table = 'vfd_station_sessions'
        verbose_name = 'VFD 工位测试会话'
        verbose_name_plural = 'VFD 工位测试会话'

    def __str__(self):
        return f'{self.station.station_code} {self.started_at}'


class VfdDeviceRun(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    session = models.ForeignKey(
        VfdStationSession,
        on_delete=models.CASCADE,
        related_name='device_runs',
        verbose_name='测试会话'
    )
    slot = models.ForeignKey(
        VfdStationSlot,
        on_delete=models.PROTECT,
        related_name='device_runs',
        verbose_name='槽位'
    )
    barcode = models.CharField(max_length=50, db_index=True, verbose_name='内部主条码')
    work_order = models.ForeignKey(
        'production.WorkOrder',
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='vfd_device_runs',
        verbose_name='工单'
    )
    product = models.ForeignKey(
        'product.Product',
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='vfd_device_runs',
        verbose_name='产品'
    )
    conclusion = models.CharField(max_length=50, blank=True, verbose_name='结论')
    started_at = models.DateTimeField(verbose_name='开始时间')
    completed_at = models.DateTimeField(null=True, blank=True, verbose_name='完成时间')

    class Meta:
        db_table = 'vfd_device_runs'
        indexes = [
            models.Index(fields=['factory', 'started_at'], name='idx_vfd_run_factory_start'),
            models.Index(fields=['factory', 'conclusion'], name='idx_vfd_run_factory_result'),
        ]
        verbose_name = 'VFD 单机测试执行'
        verbose_name_plural = 'VFD 单机测试执行'

    def __str__(self):
        return self.barcode


class VfdStepRun(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    device_run = models.ForeignKey(
        VfdDeviceRun,
        on_delete=models.CASCADE,
        related_name='step_runs',
        verbose_name='单机测试执行'
    )
    process_step = models.ForeignKey(
        VfdProcessStep,
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='step_runs',
        verbose_name='原步骤'
    )
    sequence = models.PositiveIntegerField(verbose_name='步骤序号快照')
    step_name = models.CharField(max_length=100, verbose_name='步骤名称快照')
    step_type = models.CharField(max_length=50, verbose_name='步骤类型快照')
    step_config_snapshot = models.JSONField(default=dict, blank=True, verbose_name='步骤配置快照')
    conclusion = models.CharField(max_length=50, blank=True, verbose_name='结论')
    message = models.CharField(max_length=500, blank=True, verbose_name='消息')
    started_at = models.DateTimeField(verbose_name='开始时间')
    completed_at = models.DateTimeField(null=True, blank=True, verbose_name='完成时间')

    class Meta:
        db_table = 'vfd_step_runs'
        constraints = [
            models.UniqueConstraint(fields=['factory', 'device_run', 'sequence'], name='unique_vfd_step_run_sequence'),
        ]
        verbose_name = 'VFD 步骤执行'
        verbose_name_plural = 'VFD 步骤执行'

    def __str__(self):
        return f'{self.device_run.barcode} - {self.sequence}'


class VfdMeasurementResult(FactoryScopedModel):
    step_run = models.ForeignKey(
        VfdStepRun,
        on_delete=models.CASCADE,
        related_name='measurement_results',
        verbose_name='步骤执行'
    )
    point_key = models.CharField(max_length=100, verbose_name='点位 Key')
    source = models.CharField(max_length=50, verbose_name='来源')
    numeric_value = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='数值')
    text_value = models.CharField(max_length=200, blank=True, verbose_name='文本值')
    unit = models.CharField(max_length=50, blank=True, verbose_name='单位')
    conclusion = models.CharField(max_length=50, blank=True, verbose_name='结论')
    message = models.CharField(max_length=500, blank=True, verbose_name='消息')

    class Meta:
        db_table = 'vfd_measurement_results'
        verbose_name = 'VFD 测量结果'
        verbose_name_plural = 'VFD 测量结果'

    def __str__(self):
        return f'{self.point_key} {self.numeric_value or self.text_value}'


class VfdComparisonResult(FactoryScopedModel):
    step_run = models.ForeignKey(
        VfdStepRun,
        on_delete=models.CASCADE,
        related_name='comparison_results',
        verbose_name='步骤执行'
    )
    left_key = models.CharField(max_length=100, verbose_name='左侧点位')
    right_key = models.CharField(max_length=100, verbose_name='右侧点位')
    primary_value = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='主值')
    reference_value = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='参考值')
    difference_value = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='差值')
    difference_percent = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='差值百分比')
    tolerance_type = models.CharField(max_length=50, blank=True, verbose_name='容差类型')
    tolerance_value = models.DecimalField(max_digits=18, decimal_places=6, null=True, blank=True, verbose_name='容差值')
    conclusion = models.CharField(max_length=50, blank=True, verbose_name='结论')
    message = models.CharField(max_length=500, blank=True, verbose_name='消息')

    class Meta:
        db_table = 'vfd_comparison_results'
        verbose_name = 'VFD 比对结果'
        verbose_name_plural = 'VFD 比对结果'

    def __str__(self):
        return f'{self.left_key} vs {self.right_key}'


class VfdCommandTrace(FactoryScopedModel):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    step_run = models.ForeignKey(
        VfdStepRun,
        on_delete=models.CASCADE,
        related_name='command_traces',
        verbose_name='步骤执行'
    )
    slot = models.ForeignKey(
        VfdStationSlot,
        on_delete=models.PROTECT,
        related_name='command_traces',
        verbose_name='槽位'
    )
    command_name = models.CharField(max_length=100, verbose_name='命令名称')
    target_point_key = models.CharField(max_length=100, blank=True, verbose_name='目标点位')
    request_json = models.JSONField(default=dict, blank=True, verbose_name='请求')
    response_json = models.JSONField(default=dict, blank=True, verbose_name='响应')
    is_success = models.BooleanField(default=False, verbose_name='是否成功')
    error_code = models.CharField(max_length=100, blank=True, verbose_name='错误码')
    message = models.CharField(max_length=500, blank=True, verbose_name='消息')

    class Meta:
        db_table = 'vfd_command_traces'
        verbose_name = 'VFD 命令追踪'
        verbose_name_plural = 'VFD 命令追踪'

    def __str__(self):
        return self.command_name


class VfdAuditLog(FactoryScopedModel):
    operator = models.ForeignKey(
        'user.SysUser',
        on_delete=models.SET_NULL,
        null=True, blank=True,
        related_name='vfd_audit_logs',
        verbose_name='操作人'
    )
    action = models.CharField(max_length=100, verbose_name='动作')
    target_type = models.CharField(max_length=100, verbose_name='对象类型')
    target_id = models.CharField(max_length=100, blank=True, verbose_name='对象 ID')
    before_json = models.JSONField(default=dict, blank=True, verbose_name='变更前')
    after_json = models.JSONField(default=dict, blank=True, verbose_name='变更后')

    class Meta:
        db_table = 'vfd_audit_logs'
        verbose_name = 'VFD 审计日志'
        verbose_name_plural = 'VFD 审计日志'

    def __str__(self):
        return f'{self.action} {self.target_type}'
