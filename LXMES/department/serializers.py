# department/serializers.py
from rest_framework import serializers
from .models import Department

class DepartmentSerializer(serializers.ModelSerializer):
    class Meta:
        model = Department
        fields = '__all__'
        # 自动生成且不可编辑的字段
        read_only_fields = ['id', 'create_time', 'update_time']