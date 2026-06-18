from django.db import models
from rest_framework import serializers
from department.models import Department

# Create your models here.
class SysUser(models.Model):
    id = models.AutoField(primary_key=True)
    username = models.CharField(max_length=100, unique=True, verbose_name="用户名")
    password = models.CharField(max_length=100, verbose_name="密码")
    avatar = models.CharField(max_length=255, null=True, verbose_name="用户头像")
    departments = models.ManyToManyField(Department, blank=True, verbose_name='所属部门')
    email = models.CharField(max_length=100, null=True, verbose_name="用户邮箱")
    phonenumber = models.CharField(max_length=11, null=True, verbose_name="手机号码")
    login_date = models.DateField(null=True, verbose_name="最后登录时间")
    status = models.IntegerField(null=True, verbose_name="帐号状态（1正常 0停用）")
    create_time = models.DateField(null=True, verbose_name="创建时间", )
    update_time = models.DateField(null=True, verbose_name="更新时间")
    remark = models.CharField(max_length=500, null=True, verbose_name="备注")

    # ---------- 添加以下属性和方法以满足 Django 认证接口 ----------
    @property
    def is_authenticated(self):
        """始终返回 True，表示用户已认证（因为视图已通过 JWT 验证）"""
        return True

    @property
    def is_anonymous(self):
        """始终返回 False"""
        return False

    def get_username(self):
        """返回用户名字段"""
        return self.username

    def __str__(self):
        return self.username

    class Meta:
        db_table = "sys_user"


