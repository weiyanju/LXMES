from django.db import models
from rest_framework import serializers
from role.models import SysRole


class SysMenu(models.Model):
    id = models.AutoField(primary_key=True)
    name = models.CharField(max_length=50, unique=True, verbose_name="菜单名称")
    icon = models.CharField(max_length=100, blank=True, null=True, verbose_name="菜单图标")
    parent_id = models.IntegerField(blank=True, null=True, verbose_name="父菜单ID")
    order_num = models.IntegerField(blank=True, null=True, verbose_name="显示顺序")
    path = models.CharField(max_length=200, blank=True, null=True, verbose_name="路由地址")
    component = models.CharField(max_length=255, blank=True, null=True, verbose_name="组件路径")
    menu_type = models.CharField(max_length=1, blank=True, null=True, verbose_name="菜单类型（M目录 C菜单 F按钮）")
    perms = models.CharField(max_length=100, blank=True, null=True, verbose_name="权限标识")
    create_time = models.DateField(blank=True, null=True, verbose_name="创建时间")
    update_time = models.DateField(blank=True, null=True, verbose_name="更新时间")
    remark = models.CharField(max_length=500, blank=True, null=True, verbose_name="备注")

    def __lt__(self, other):
        return self.order_num < other.order_num

    class Meta:
        db_table = "sys_menu"


# 保留以下序列化器，供旧版视图（如 TreeListView）使用
class SysMenuSerializer2(serializers.ModelSerializer):
    class Meta:
        model = SysMenu
        fields = '__all__'


class SysMenuSerializer(serializers.ModelSerializer):
    children = serializers.SerializerMethodField()

    def get_children(self, obj):
        if hasattr(obj, "children"):
            serializerMenuList = []
            for sysMenu in obj.children:
                serializerMenuList.append(SysMenuSerializer2(sysMenu).data)
            return serializerMenuList
        return []

    class Meta:
        model = SysMenu
        fields = '__all__'


class SysRoleMenu(models.Model):
    id = models.AutoField(primary_key=True)
    role = models.ForeignKey(SysRole, on_delete=models.PROTECT)
    menu = models.ForeignKey(SysMenu, on_delete=models.PROTECT)

    class Meta:
        db_table = "sys_role_menu"


class SysRoleMenuSerializer(serializers.ModelSerializer):
    class Meta:
        model = SysRoleMenu
        fields = '__all__'