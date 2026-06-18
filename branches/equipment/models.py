# apps/equipment/models.py
from django.db import models
from core.base import FactoryScopedModel
from core.models import Employee, User


class Equipment(FactoryScopedModel):
    equipment_code = models.CharField(max_length=50, unique=True, verbose_name='设备编码')
    equipment_name = models.CharField(max_length=100, verbose_name='设备名称')
    type = models.CharField(max_length=50, verbose_name='设备类型')
    model = models.CharField(max_length=100, blank=True, verbose_name='设备型号')
    specification = models.CharField(max_length=255, blank=True, verbose_name='设备规格')
    manufacturer = models.CharField(max_length=100, blank=True, verbose_name='制造商')
    supplier = models.CharField(max_length=100, blank=True, verbose_name='供应商')
    status = models.CharField(max_length=20, default='active', verbose_name='状态')
    location = models.CharField(max_length=100, blank=True, verbose_name='位置')
    purchase_date = models.DateField(null=True, blank=True, verbose_name='购买日期')
    installation_date = models.DateField(null=True, blank=True, verbose_name='安装日期')
    warranty_end_date = models.DateField(null=True, blank=True, verbose_name='质保结束日期')
    # 已移除冗余字段 last_maintenance，请通过 maintenance_records 查询最新维护日期

    class Meta:
        db_table = 'equipment'
        verbose_name = '设备'
        verbose_name_plural = '设备'

    def __str__(self):
        return f'{self.equipment_code} - {self.equipment_name}'


class EquipmentStatus(FactoryScopedModel):
    equipment = models.ForeignKey(
        Equipment,
        on_delete=models.CASCADE,
        related_name='status_records',
        verbose_name='设备'
    )
    status = models.CharField(max_length=20, verbose_name='状态')
    description = models.CharField(max_length=255, blank=True, verbose_name='状态描述')
    operator = models.ForeignKey(
        User,
        on_delete=models.SET_NULL,
        null=True,
        related_name='equipment_status_changes',
        verbose_name='操作人'
    )

    class Meta:
        db_table = 'equipment_status'
        verbose_name = '设备状态'
        verbose_name_plural = '设备状态'

    def __str__(self):
        return f'{self.equipment} - {self.status} ({self.created_at})'


class MaintenancePlan(FactoryScopedModel):
    plan_code = models.CharField(max_length=50, unique=True, verbose_name='计划编码')
    plan_name = models.CharField(max_length=100, verbose_name='计划名称')
    equipment = models.ForeignKey(
        Equipment,
        on_delete=models.CASCADE,
        related_name='maintenance_plans',
        verbose_name='设备'
    )
    maintenance_type = models.CharField(max_length=50, verbose_name='维护类型')
    scheduled_date = models.DateField(verbose_name='计划维护日期')
    estimated_duration = models.PositiveIntegerField(verbose_name='预计维护时长(小时)')
    status = models.CharField(max_length=20, default='pending', verbose_name='状态')
    assignee = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        related_name='assigned_plans',
        verbose_name='指派人员'
    )

    class Meta:
        db_table = 'maintenance_plans'
        verbose_name = '维护计划'
        verbose_name_plural = '维护计划'

    def __str__(self):
        return f'{self.plan_code} - {self.plan_name} ({self.equipment})'


class MaintenanceRecord(FactoryScopedModel):
    # 此表不需要 updated_at，显式移除
    updated_at = None

    plan = models.ForeignKey(
        MaintenancePlan,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='maintenance_records',
        verbose_name='维护计划'
    )
    equipment = models.ForeignKey(
        Equipment,
        on_delete=models.CASCADE,
        related_name='maintenance_records',
        verbose_name='设备'
    )
    maintenance_type = models.CharField(max_length=50, verbose_name='维护类型')
    maintenance_date = models.DateField(verbose_name='维护日期')
    duration = models.PositiveIntegerField(verbose_name='维护时长(小时)')
    maintenance_content = models.TextField(verbose_name='维护内容')
    maintenance_result = models.CharField(max_length=20, verbose_name='维护结果')
    maintenance_by = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        related_name='maintained_records',
        verbose_name='维护人员'
    )

    class Meta:
        db_table = 'maintenance_records'
        verbose_name = '维护记录'
        verbose_name_plural = '维护记录'

    def __str__(self):
        return f'{self.equipment} - {self.maintenance_date} {self.maintenance_type}'


class EquipmentFault(FactoryScopedModel):
    fault_code = models.CharField(max_length=50, verbose_name='故障编码')
    equipment = models.ForeignKey(
        Equipment,
        on_delete=models.CASCADE,
        related_name='faults',
        verbose_name='设备'
    )
    fault_description = models.TextField(verbose_name='故障描述')
    fault_time = models.DateTimeField(verbose_name='故障发生时间')
    fault_level = models.CharField(max_length=20, verbose_name='故障级别')
    status = models.CharField(max_length=20, verbose_name='状态')
    repair_content = models.TextField(blank=True, verbose_name='维修内容')
    repair_time = models.DateTimeField(null=True, blank=True, verbose_name='维修时间')
    repair_by = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        related_name='repaired_faults',
        verbose_name='维修人员'
    )

    class Meta:
        db_table = 'equipment_faults'
        verbose_name = '故障记录'
        verbose_name_plural = '故障记录'

    def __str__(self):
        return f'{self.fault_code} - {self.equipment} ({self.fault_time})'