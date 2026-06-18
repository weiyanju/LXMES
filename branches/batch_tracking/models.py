# apps/batch_tracking/models.py
from django.db import models
from core.base import FactoryScopedModel
from product.models import Product


class BatchTracking(FactoryScopedModel):
    """批次追溯表"""
    batch_number = models.CharField(max_length=100, verbose_name='批次号')
    product = models.ForeignKey(
        Product,
        on_delete=models.PROTECT,
        verbose_name='产品'
    )
    quantity = models.PositiveIntegerField(verbose_name='数量')
    production_date = models.DateField(verbose_name='生产日期')
    expiry_date = models.DateField(null=True, blank=True, verbose_name='有效期至')
    status = models.CharField(max_length=20, default='active', verbose_name='状态')
    # created_at, updated_at, factory 由基类提供

    class Meta:
        db_table = 'batch_tracking'
        verbose_name = '批次追溯'
        verbose_name_plural = '批次追溯'
        # 同一工厂下批次号唯一（自动创建唯一索引）
        unique_together = [('batch_number', 'factory')]
        # 额外为生产日期创建普通索引，加速按日期范围查询
        indexes = [
            models.Index(fields=['production_date']),
        ]

    def __str__(self):
        return f'{self.batch_number} - {self.product.product_name}'