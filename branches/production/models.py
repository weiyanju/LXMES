# apps/production/models.py
from django.db import models
from core.base import FactoryScopedModel
from core.models import Customer, User
from product.models import Product, Process, ProcessFile


class ProductionPlan(FactoryScopedModel):
    SOURCE_CHOICES = ((1, '客户订单'), (2, '库存备货'))
    plan_name = models.CharField(max_length=100, verbose_name='计划名称')
    product_type = models.CharField(max_length=50, verbose_name='产品类型')
    product = models.ForeignKey(
        Product,
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='production_plans',
        verbose_name='产品'
    )
    quantity = models.PositiveIntegerField(verbose_name='计划数量')
    start_barcode = models.CharField(max_length=30, blank=True, verbose_name='开始条码')
    end_barcode = models.CharField(max_length=30, blank=True, verbose_name='结束条码')
    customer_start_barcode = models.CharField(max_length=50, blank=True, verbose_name='客户开始条码')
    customer_end_barcode = models.CharField(max_length=50, blank=True, verbose_name='客户结束条码')
    start_date = models.DateField(verbose_name='开始日期')
    end_date = models.DateField(verbose_name='结束日期')
    demand_date = models.DateField(verbose_name='需求日期')
    source = models.IntegerField(choices=SOURCE_CHOICES, verbose_name='来源')
    status = models.CharField(max_length=20, default='draft', verbose_name='状态')
    customer = models.ForeignKey(
        Customer,
        on_delete=models.PROTECT,
        null=True, blank=True,
        verbose_name='客户'
    )
    created_by = models.ForeignKey(
        User,
        on_delete=models.SET_NULL,
        null=True,
        related_name='created_plans',
        verbose_name='创建人'
    )
    remark = models.TextField(blank=True, verbose_name='备注')

    class Meta:
        db_table = 'production_plans'
        verbose_name = '生产计划'
        verbose_name_plural = '生产计划'

    def __str__(self):
        return f'{self.plan_name} ({self.product_type})'


class WorkOrder(FactoryScopedModel):
    order_number = models.CharField(max_length=50, unique=True, verbose_name='工单编号')
    plan = models.ForeignKey(
        ProductionPlan,
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='work_orders',
        verbose_name='生产计划'
    )
    product_type = models.CharField(max_length=50, verbose_name='产品类型')
    product = models.ForeignKey(
        Product,
        on_delete=models.PROTECT,
        null=True, blank=True,
        related_name='work_orders',
        verbose_name='产品'
    )
    quantity = models.PositiveIntegerField(verbose_name='数量')
    start_barcode = models.CharField(max_length=30, blank=True, verbose_name='开始条码')
    end_barcode = models.CharField(max_length=30, blank=True, verbose_name='结束条码')
    customer_start_barcode = models.CharField(max_length=50, blank=True, verbose_name='客户开始条码')
    customer_end_barcode = models.CharField(max_length=50, blank=True, verbose_name='客户结束条码')
    status = models.CharField(max_length=20, default='pending', verbose_name='状态')
    demand_date = models.DateField(null=True, blank=True, verbose_name='需求日期')
    process_file = models.ForeignKey(
        ProcessFile,
        on_delete=models.SET_NULL,
        null=True, blank=True,
        verbose_name='工艺文件'
    )
    start_time = models.DateTimeField(null=True, blank=True, verbose_name='开始时间')
    end_time = models.DateTimeField(null=True, blank=True, verbose_name='结束时间')
    remark = models.TextField(blank=True, verbose_name='备注')

    class Meta:
        db_table = 'work_orders'
        verbose_name = '工单'
        verbose_name_plural = '工单'

    def __str__(self):
        return f'{self.order_number} - {self.product_type}'


class BarcodeRelation(FactoryScopedModel):
    BARCODE_TYPES = [
        (1, '厂内条码'),
        (2, '外壳条码'),
        (3, '主板条码'),
        (4, '电源板条码'),
        (5, '通讯板条码'),
        (6, '继电器板条码'),
        (7, '驱动板条码'),
        (8, '键盘板条码'),
    ]
    main_barcode = models.CharField(max_length=50, verbose_name='主条码')
    main_barcode_type = models.IntegerField(choices=BARCODE_TYPES, verbose_name='主条码类型')
    sub_barcode = models.CharField(max_length=50, verbose_name='子条码')
    sub_barcode_type = models.IntegerField(choices=BARCODE_TYPES, verbose_name='子条码类型')
    product = models.ForeignKey(
        'product.Product',
        on_delete=models.PROTECT,
        verbose_name='产品'
    )
    work_order = models.ForeignKey(
        WorkOrder,
        on_delete=models.CASCADE,
        verbose_name='工单'
    )

    class Meta:
        db_table = 'barcode_relations'
        verbose_name = '条码关系'
        verbose_name_plural = '条码关系'

    def __str__(self):
        return f'{self.main_barcode} -> {self.sub_barcode}'


class BarcodeScan(FactoryScopedModel):
    BARCODE_TYPES = BarcodeRelation.BARCODE_TYPES
    barcode = models.CharField(max_length=50, verbose_name='条码')
    barcode_type = models.IntegerField(choices=BARCODE_TYPES, verbose_name='条码类型')
    work_order = models.ForeignKey(
        WorkOrder,
        on_delete=models.CASCADE,
        related_name='scans',
        verbose_name='工单'
    )
    process = models.ForeignKey(
        Process,
        on_delete=models.PROTECT,
        verbose_name='工序'
    )
    scanner = models.ForeignKey(
        User,
        on_delete=models.SET_NULL,
        null=True,
        verbose_name='扫描人'
    )
    scanning_location = models.CharField(max_length=100, blank=True, verbose_name='扫描位置')
    parameters = models.JSONField(default=dict, blank=True, verbose_name='扫描参数')
    # scanning_time 已移除，请使用基类的 created_at 代替

    class Meta:
        db_table = 'barcode_scans'
        verbose_name = '条码扫描记录'
        verbose_name_plural = '条码扫描记录'

    def __str__(self):
        return f'{self.barcode} @ {self.created_at}'


class ProductionData(FactoryScopedModel):
    work_order = models.ForeignKey(
        WorkOrder,
        on_delete=models.CASCADE,
        related_name='production_data',
        verbose_name='关联工单'
    )
    equipment = models.ForeignKey(
        'equipment.Equipment',
        on_delete=models.SET_NULL,
        null=True,
        verbose_name='关联设备'
    )
    product_code = models.CharField(max_length=50, verbose_name='产品编码')
    parameter_name = models.CharField(max_length=100, verbose_name='参数名称')
    parameter_value = models.CharField(max_length=100, verbose_name='参数值')
    timestamp = models.DateTimeField(verbose_name='采集时间')
    operator = models.ForeignKey(
        'core.User',
        on_delete=models.SET_NULL,
        null=True,
        verbose_name='操作人'
    )

    class Meta:
        db_table = 'production_data'
        verbose_name = '生产数据'
        verbose_name_plural = '生产数据'

    def __str__(self):
        return f'{self.work_order.order_number} - {self.parameter_name}'
