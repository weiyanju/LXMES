# apps/parameter/models.py
from django.db import models
from core.base import FactoryScopedModel


class MeterParameter(FactoryScopedModel):
    """电表参数表"""
    # 显式移除基类的 updated_at 字段（业务不需要）
    updated_at = None

    product_code = models.CharField(max_length=50, verbose_name='产品编码')
    voltage = models.CharField(max_length=20, blank=True, verbose_name='电压')
    current = models.CharField(max_length=20, blank=True, verbose_name='电流')
    power = models.CharField(max_length=20, blank=True, verbose_name='功率')
    frequency = models.CharField(max_length=20, blank=True, verbose_name='频率')
    accuracy = models.CharField(max_length=20, blank=True, verbose_name='精度')
    # created_at 由基类提供

    class Meta:
        db_table = 'meter_parameters'
        verbose_name = '电表参数'
        verbose_name_plural = '电表参数'

    def __str__(self):
        return f'{self.product_code} 电表参数 (ID:{self.id})'


class InverterParameter(FactoryScopedModel):
    """变频器参数表"""
    # 显式移除基类的 updated_at 字段
    updated_at = None

    product_code = models.CharField(max_length=50, verbose_name='产品编码')
    input_voltage = models.CharField(max_length=20, blank=True, verbose_name='输入电压')
    output_voltage = models.CharField(max_length=20, blank=True, verbose_name='输出电压')
    input_current = models.CharField(max_length=20, blank=True, verbose_name='输入电流')
    output_current = models.CharField(max_length=20, blank=True, verbose_name='输出电流')
    power = models.CharField(max_length=20, blank=True, verbose_name='功率')
    frequency_range = models.CharField(max_length=50, blank=True, verbose_name='频率范围')
    # created_at 由基类提供

    class Meta:
        db_table = 'inverter_parameters'
        verbose_name = '变频器参数'
        verbose_name_plural = '变频器参数'

    def __str__(self):
        return f'{self.product_code} 变频器参数 (ID:{self.id})'