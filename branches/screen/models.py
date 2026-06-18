from django.db import models

class DefectRecord(models.Model):
    """生产不良记录表（对应日期表格）"""
    date = models.DateTimeField(verbose_name='日期')
    product = models.CharField(max_length=100, verbose_name='产品')
    report_qty = models.IntegerField(verbose_name='报工数量')
    good_qty = models.IntegerField(verbose_name='良品数')
    bad_qty = models.IntegerField(verbose_name='不良品数')

    class Meta:
        db_table = 'screen_defect_record'
        verbose_name = '不良记录'
        verbose_name_plural = '不良记录'

class EmployeePerformance(models.Model):
    """员工绩效表（对应Top5）"""
    employee_name = models.CharField(max_length=50, verbose_name='员工姓名')
    score = models.IntegerField(verbose_name='绩效分数')

    class Meta:
        db_table = 'screen_employee_performance'
        verbose_name = '员工绩效'
        verbose_name_plural = '员工绩效'

class SalesOrder(models.Model):
    """销售订单表（用于统计占比）"""
    status = models.CharField(max_length=20, verbose_name='状态')  # 已完成/进行中/未开始

    class Meta:
        db_table = 'screen_sales_order'
        verbose_name = '销售订单'
        verbose_name_plural = '销售订单'

class DefectTypeStat(models.Model):
    """不良品类型统计表（饼图数据）"""
    name = models.CharField(max_length=50, verbose_name='不良品项')
    value = models.IntegerField(verbose_name='数量')

    class Meta:
        db_table = 'screen_defect_type_stat'
        verbose_name = '不良品类型统计'
        verbose_name_plural = '不良品类型统计'

class ProcessPlan(models.Model):
    """工序计划表（Top5）"""
    process_name = models.CharField(max_length=50, verbose_name='工序名称')
    plan_qty = models.IntegerField(verbose_name='计划数量')

    class Meta:
        db_table = 'screen_process_plan'
        verbose_name = '工序计划'
        verbose_name_plural = '工序计划'

class WorkOrder(models.Model):
    """工单表（对应工单表格）"""
    order_no = models.CharField(max_length=50, verbose_name='工单编号')
    status = models.CharField(max_length=20, verbose_name='状态')
    product_code = models.CharField(max_length=50, verbose_name='产品编号')
    product_name = models.CharField(max_length=100, verbose_name='产品名称')
    spec = models.CharField(max_length=50, verbose_name='产品规格')
    plan_qty = models.IntegerField(verbose_name='计划数量')
    actual_qty = models.IntegerField(verbose_name='实际数量')

    class Meta:
        db_table = 'screen_work_order'
        verbose_name = '工单'
        verbose_name_plural = '工单'