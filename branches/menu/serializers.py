# menu/serializers.py
from django.db import models
from role.models import SysRole
from rest_framework import serializers
from .models import SysMenu

class SysMenuSerializer(serializers.ModelSerializer):
    children = serializers.SerializerMethodField()

    class Meta:
        model = SysMenu
        fields = '__all__'
        read_only_fields = ['id', 'create_time', 'update_time']

    # 显式声明字段，允许空白和 null（与模型定义同步）
    icon = serializers.CharField(required=False, allow_blank=True, allow_null=True)
    path = serializers.CharField(required=False, allow_blank=True, allow_null=True)
    component = serializers.CharField(required=False, allow_blank=True, allow_null=True)
    perms = serializers.CharField(required=False, allow_blank=True, allow_null=True)
    remark = serializers.CharField(required=False, allow_blank=True, allow_null=True)
    parent_id = serializers.IntegerField(required=False, allow_null=True)

    def get_children(self, obj):
        if hasattr(obj, 'children'):
            return SysMenuSerializer(obj.children, many=True).data
        return []

    def validate(self, attrs):
        parent_id = attrs.get('parent_id')
        menu_type = attrs.get('menu_type')

        if parent_id is None:
            if menu_type != 'M':
                raise serializers.ValidationError({'menu_type': '根目录下只能添加目录'})
        else:
            try:
                parent = SysMenu.objects.get(id=parent_id)
            except SysMenu.DoesNotExist:
                raise serializers.ValidationError({'parent_id': '上级菜单不存在'})
            if parent.menu_type == 'C':
                raise serializers.ValidationError({'parent_id': '菜单下不能添加子节点'})
        return attrs

