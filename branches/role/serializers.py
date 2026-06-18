# role/serializers.py
from rest_framework import serializers
from .models import SysRole

class SysRoleSerializer(serializers.ModelSerializer):
    class Meta:
        model = SysRole
        fields = '__all__'
        read_only_fields = ['id', 'create_time', 'update_time']