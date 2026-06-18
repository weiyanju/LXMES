# apps/batch_tracking/serializers.py
from rest_framework import serializers
from .models import BatchTracking


class BatchTrackingSerializer(serializers.ModelSerializer):
    product_name = serializers.CharField(source='product.product_name', read_only=True)
    product_code = serializers.CharField(source='product.product_code', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = BatchTracking
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']