# apps/product/models.py
from django.db import models
from core.base import FactoryScopedModel, BaseModel


class Product(FactoryScopedModel):
    product_code = models.CharField(max_length=50, unique=True, verbose_name='产品编码')
    product_name = models.CharField(max_length=100, verbose_name='产品名称')
    product_type = models.CharField(max_length=50, verbose_name='产品类型')
    specification = models.CharField(max_length=255, blank=True)
    product_image = models.ImageField(upload_to='products/', blank=True, verbose_name='产品图片')
    status = models.CharField(max_length=20, default='active')

    class Meta:
        db_table = 'products'
        verbose_name = '产品'
        verbose_name_plural = '产品'

    def __str__(self):
        return f'{self.product_code} - {self.product_name}'


class Process(FactoryScopedModel):
    process_code = models.CharField(max_length=50, unique=True, verbose_name='工序编码')
    process_name = models.CharField(max_length=100, verbose_name='工序名称')
    description = models.CharField(max_length=255, blank=True)

    class Meta:
        db_table = 'processes'
        verbose_name = '工序'
        verbose_name_plural = '工序'

    def __str__(self):
        return f'{self.process_code} - {self.process_name}'


class ProductProcess(BaseModel):
    product = models.ForeignKey(
        Product,
        on_delete=models.CASCADE,
        related_name='product_processes',
        verbose_name='产品'
    )
    process = models.ForeignKey(
        Process,
        on_delete=models.CASCADE,
        related_name='product_processes',
        verbose_name='工序'
    )
    sequence = models.PositiveIntegerField(verbose_name='工序顺序')

    class Meta:
        db_table = 'product_processes'
        constraints = [
            models.UniqueConstraint(fields=['product', 'sequence'], name='unique_product_sequence'),
            models.UniqueConstraint(fields=['product', 'process'], name='unique_product_process')
        ]
        verbose_name = '产品工序'
        verbose_name_plural = '产品工序'

    def __str__(self):
        return f'{self.product.product_name} - {self.process.process_name} (顺序:{self.sequence})'


class ProcessFile(FactoryScopedModel):
    file_code = models.CharField(max_length=50, unique=True)
    file_name = models.CharField(max_length=100)
    version = models.CharField(max_length=20)
    file_path = models.CharField(max_length=255)  # 或 FileField
    file_size = models.BigIntegerField(default=0)
    description = models.TextField(blank=True)
    product_type = models.CharField(max_length=50, blank=True)
    status = models.CharField(max_length=20, default='active')
    uploaded_by = models.ForeignKey(
        'core.User',
        on_delete=models.SET_NULL,
        null=True,
        verbose_name='上传人'
    )

    class Meta:
        db_table = 'process_files'
        verbose_name = '工艺文件'
        verbose_name_plural = '工艺文件'

    def __str__(self):
        return f'{self.file_code} - {self.file_name}'