# apps/quality/models.py
from django.db import models
from core.base import FactoryScopedModel  # 注意：您之前的导入路径为 core.models，请根据实际项目调整
from core.models import Employee, User
from product.models import Product, Process
from production.models import WorkOrder

class InspectionPlan(FactoryScopedModel):
    INSPECTION_TYPES = [
        ('incoming', '来料检验'),
        ('process', '过程检验'),
        ('return', '退料检验'),
        ('shipping', '出货检验'),
    ]

    plan_code = models.CharField(max_length=50, unique=True, verbose_name='方案编码')
    plan_name = models.CharField(max_length=100, verbose_name='方案名称')
    inspection_type = models.CharField(
        max_length=20, choices=INSPECTION_TYPES, verbose_name='检验类型'
    )
    product = models.ForeignKey(
        'product.Product',
        on_delete=models.PROTECT,
        related_name='inspection_plans',
        verbose_name='关联产品'
    )
    status = models.CharField(max_length=20, default='active', verbose_name='状态')
    # created_at, updated_at 由基类提供

    class Meta:
        db_table = 'inspection_plans'
        verbose_name = '质检方案'
        verbose_name_plural = '质检方案'

    def __str__(self):
        return f'{self.plan_code} - {self.plan_name}'


class InspectionTask(FactoryScopedModel):
    # 复用 InspectionPlan 的检验类型 choices，保持一致性
    INSPECTION_TYPES = InspectionPlan.INSPECTION_TYPES

    task_code = models.CharField(max_length=50, unique=True, verbose_name='任务编码')
    inspection_type = models.CharField(
        max_length=20, choices=INSPECTION_TYPES, verbose_name='检验类型'
    )
    plan = models.ForeignKey(
        InspectionPlan,
        on_delete=models.PROTECT,
        related_name='tasks',
        verbose_name='质检方案'
    )
    product = models.ForeignKey(
        'product.Product',
        on_delete=models.PROTECT,
        related_name='inspection_tasks',
        verbose_name='检验产品'
    )
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    quantity = models.PositiveIntegerField(verbose_name='检验数量')
    status = models.CharField(max_length=20, default='pending', verbose_name='任务状态')
    assignee = models.ForeignKey(
        'core.Employee',
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='assigned_tasks',
        verbose_name='指派人员'
    )
    # created_at, updated_at 由基类提供

    class Meta:
        db_table = 'inspection_tasks'
        verbose_name = '质检任务'
        verbose_name_plural = '质检任务'

    def __str__(self):
        return f'{self.task_code} ({self.get_inspection_type_display()})'

class IncomingInspection(FactoryScopedModel):
    """来料检验表"""
    task = models.ForeignKey(
        'InspectionTask',
        on_delete=models.CASCADE,
        related_name='incoming_inspections',
        verbose_name='待检任务'
    )
    supplier = models.ForeignKey(
        'core.Customer',  # 假设供应商即客户
        on_delete=models.PROTECT,
        null=True,
        blank=True,
        verbose_name='供应商'
    )
    material_code = models.CharField(max_length=50, blank=True, verbose_name='物料编码')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='incoming_inspections',
        verbose_name='检验员'
    )
    inspection_time = models.DateTimeField(null=True, blank=True, verbose_name='检验时间')

    class Meta:
        db_table = 'incoming_inspections'
        verbose_name = '来料检验'
        verbose_name_plural = '来料检验'

    def __str__(self):
        return f'{self.task} - 来料检验'


class ProcessInspection(FactoryScopedModel):
    """过程检验表"""
    task = models.ForeignKey(
        'InspectionTask',
        on_delete=models.CASCADE,
        related_name='process_inspections',
        verbose_name='待检任务'
    )
    work_order = models.ForeignKey(
        WorkOrder,
        on_delete=models.CASCADE,
        related_name='process_inspections',
        verbose_name='工单'
    )
    process = models.ForeignKey(
        Process,
        on_delete=models.PROTECT,
        null=True,
        blank=True,
        verbose_name='工序'
    )
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='process_inspections',
        verbose_name='检验员'
    )
    inspection_time = models.DateTimeField(null=True, blank=True, verbose_name='检验时间')

    class Meta:
        db_table = 'process_inspections'
        verbose_name = '过程检验'
        verbose_name_plural = '过程检验'

    def __str__(self):
        return f'{self.task} - 过程检验'


class ReturnInspection(FactoryScopedModel):
    """退料检验表"""
    task = models.ForeignKey(
        'InspectionTask',
        on_delete=models.CASCADE,
        related_name='return_inspections',
        verbose_name='待检任务'
    )
    material_code = models.CharField(max_length=50, blank=True, verbose_name='物料编码')
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    return_reason = models.CharField(max_length=255, blank=True, verbose_name='退料原因')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='return_inspections',
        verbose_name='检验员'
    )
    inspection_time = models.DateTimeField(null=True, blank=True, verbose_name='检验时间')

    class Meta:
        db_table = 'return_inspections'
        verbose_name = '退料检验'
        verbose_name_plural = '退料检验'

    def __str__(self):
        return f'{self.task} - 退料检验'


class ShippingInspection(FactoryScopedModel):
    """出货检验表"""
    task = models.ForeignKey(
        'InspectionTask',
        on_delete=models.CASCADE,
        related_name='shipping_inspections',
        verbose_name='待检任务'
    )
    order = models.ForeignKey(
        WorkOrder,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='shipping_inspections',
        verbose_name='订单(工单)'
    )
    product = models.ForeignKey(
        Product,
        on_delete=models.PROTECT,
        null=True,
        blank=True,
        verbose_name='产品'
    )
    batch_number = models.CharField(max_length=100, blank=True, verbose_name='批次号')
    quantity = models.PositiveIntegerField(verbose_name='检验数量')
    inspection_result = models.CharField(max_length=20, blank=True, verbose_name='检验结果')
    inspector = models.ForeignKey(
        Employee,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name='shipping_inspections',
        verbose_name='检验员'
    )
    inspection_time = models.DateTimeField(null=True, blank=True, verbose_name='检验时间')

    class Meta:
        db_table = 'shipping_inspections'
        verbose_name = '出货检验'
        verbose_name_plural = '出货检验'

    def __str__(self):
        return f'{self.task} - 出货检验'
