# apps/core/models.py
from django.contrib.auth.models import AbstractUser
from django.db import models
from .base import BaseModel, FactoryScopedModel


class Factory(BaseModel):  # 改为继承 BaseModel
    factory_code = models.CharField(max_length=50, unique=True, verbose_name='工厂编码')
    factory_name = models.CharField(max_length=100, verbose_name='工厂名称')
    address = models.CharField(max_length=255, blank=True, verbose_name='工厂地址')
    contact_person = models.CharField(max_length=100, blank=True, verbose_name='联系人')
    contact_phone = models.CharField(max_length=20, blank=True, verbose_name='联系电话')
    status = models.CharField(max_length=20, default='active', verbose_name='状态')
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'core_factories'
        verbose_name = '工厂'
        verbose_name_plural = '工厂'

    def __str__(self):
        return f'{self.factory_code} - {self.factory_name}'


class Customer(BaseModel):
    """客户表"""
    customer_code = models.CharField(max_length=50, unique=True, verbose_name='客户编码')
    customer_name = models.CharField(max_length=100, verbose_name='客户名称')
    contact_person = models.CharField(max_length=100, blank=True)
    contact_phone = models.CharField(max_length=20, blank=True)
    email = models.EmailField(max_length=100, blank=True)
    address = models.CharField(max_length=255, blank=True)
    status = models.CharField(max_length=20, default='active')

    class Meta:
        db_table = 'core_customers'
        verbose_name = '客户'
        verbose_name_plural = '客户'

    def __str__(self):
        return f'{self.customer_code} - {self.customer_name}'


class User(AbstractUser):
    name = models.CharField(max_length=100, verbose_name='真实姓名')
    role = models.CharField(max_length=20, verbose_name='角色')
    department = models.CharField(max_length=100, blank=True, verbose_name='部门')
    factory = models.ForeignKey(
        'core.Factory',
        on_delete=models.CASCADE,
        null=True, blank=True,
        related_name='users',
        verbose_name='所属工厂'
    )
    created_at = models.DateTimeField(auto_now_add=True, verbose_name='创建时间')
    updated_at = models.DateTimeField(auto_now=True, verbose_name='更新时间')

    # 显式指定多对多关系的反向名称，避免与默认 auth.User 冲突
    groups = models.ManyToManyField(
        'auth.Group',
        verbose_name='groups',
        blank=True,
        help_text='The groups this user belongs to.',
        related_name='core_user_set',           # 自定义反向名
        related_query_name='core_user',
    )
    user_permissions = models.ManyToManyField(
        'auth.Permission',
        verbose_name='user permissions',
        blank=True,
        help_text='Specific permissions for this user.',
        related_name='core_user_set',           # 自定义反向名
        related_query_name='core_user',
    )

    class Meta:
        db_table = 'core_users'
        verbose_name = '用户'
        verbose_name_plural = '用户'


class Department(FactoryScopedModel):
    """部门表"""
    department_code = models.CharField(max_length=50, unique=True, verbose_name='部门编码')
    department_name = models.CharField(max_length=100, verbose_name='部门名称')
    parent = models.ForeignKey(
        'self',
        on_delete=models.SET_NULL,
        null=True, blank=True,
        verbose_name='父部门'
    )
    status = models.CharField(max_length=20, default='active')

    class Meta:
        db_table = 'core_departments'
        verbose_name = '部门'
        verbose_name_plural = '部门'


class Employee(FactoryScopedModel):
    """员工表"""
    employee_code = models.CharField(max_length=50, unique=True, verbose_name='员工编码')
    name = models.CharField(max_length=100, verbose_name='员工姓名')
    department = models.ForeignKey(
        Department,
        on_delete=models.SET_NULL,
        null=True, blank=True,
        verbose_name='所属部门'
    )
    position = models.CharField(max_length=100, blank=True)
    phone = models.CharField(max_length=20, blank=True)
    status = models.CharField(max_length=20, default='active')

    class Meta:
        db_table = 'core_employees'
        verbose_name = '员工'
        verbose_name_plural = '员工'