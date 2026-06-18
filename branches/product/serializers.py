# apps/product/serializers.py
from rest_framework import serializers
from .models import Product, Process, ProductProcess, ProcessFile
from core.models import Factory, User


class ProductSerializer(serializers.ModelSerializer):
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = Product
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ProcessSerializer(serializers.ModelSerializer):
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = Process
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ProductProcessSerializer(serializers.ModelSerializer):
    product_name = serializers.CharField(source='product.product_name', read_only=True)
    process_name = serializers.CharField(source='process.process_name', read_only=True)

    class Meta:
        model = ProductProcess
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ProcessFileSerializer(serializers.ModelSerializer):
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)
    uploaded_by_name = serializers.CharField(source='uploaded_by.name', read_only=True)

    class Meta:
        model = ProcessFile
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at', 'file_size']