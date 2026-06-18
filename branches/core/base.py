# apps/core/base.py
from django.db import models


class BaseModel(models.Model):
    """
    所有表的通用基类（不包含工厂外键）。
    提供 created_at 和 updated_at 字段。
    """
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        abstract = True


class FactoryScopedModel(BaseModel):
    """
    带有工厂外键的抽象基类，适用于绝大多数业务表。
    自动添加 factory 字段（关联到 core.Factory）以及时间戳。
    """
    factory = models.ForeignKey(
        'core.Factory',
        on_delete=models.CASCADE,
        related_name='%(class)s_set',   # 动态生成反向关系名，例如 product_set
        verbose_name='所属工厂'
    )

    class Meta:
        abstract = True