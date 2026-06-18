from datetime import datetime
from rest_framework import serializers
from .models import SysUser
from department.models import Department
from django.contrib.auth.hashers import make_password, check_password
from role.models import SysRole, SysUserRole   # 新增导入

class SysUserSerializer(serializers.ModelSerializer):
    departments = serializers.PrimaryKeyRelatedField(
        many=True,
        queryset=Department.objects.all(),
        required=False
    )
    password = serializers.CharField(write_only=True, required=False, allow_blank=True)

    # 新增：角色字段，只读，返回角色列表
    roles = serializers.SerializerMethodField(read_only=True)

    class Meta:
        model = SysUser
        fields = '__all__'
        read_only_fields = ['id', 'create_time', 'update_time', 'login_date']

    def get_roles(self, obj):
        """获取用户拥有的角色列表"""
        role_ids = SysUserRole.objects.filter(user_id=obj.id).values_list('role_id', flat=True)
        roles = SysRole.objects.filter(id__in=role_ids)
        return [{'id': r.id, 'name': r.name} for r in roles]

    def validate_password(self, value):
        # 新增用户时必须有密码
        if self.instance is None and not value:
            raise serializers.ValidationError("密码不能为空")
        return value

    def create(self, validated_data):
        password = validated_data.pop('password', None)
        departments = validated_data.pop('departments', [])
        user = SysUser.objects.create(**validated_data)
        user.password = password or '123456'  # 明文存储
        user.avatar = validated_data.get('avatar', 'default.jpg')
        user.create_time = datetime.now().date()
        user.login_date = datetime.now().date()
        user.departments.set(departments)
        user.save()
        return user

    def update(self, instance, validated_data):
        password = validated_data.pop('password', None)
        departments = validated_data.pop('departments', None)
        for attr, value in validated_data.items():
            setattr(instance, attr, value)
        if password is not None:
            instance.password = password  # 明文更新
        instance.save()
        if departments is not None:
            instance.departments.set(departments)
        return instance