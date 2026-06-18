from django.db import models

class Department(models.Model):
    name = models.CharField(max_length=50, verbose_name='部门名称')
    remark = models.CharField(max_length=200, blank=True, verbose_name='备注')
    create_time = models.DateField(auto_now_add=True, verbose_name='创建时间')
    update_time = models.DateField(auto_now=True, verbose_name='更新时间')

    class Meta:
        db_table = 'department'
        verbose_name = '部门'
        verbose_name_plural = '部门'

    def __str__(self):
        return self.name