from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('production', '0002_add_product_to_plan_and_work_order'),
    ]

    operations = [
        migrations.AddField(
            model_name='productionplan',
            name='customer_start_barcode',
            field=models.CharField(blank=True, max_length=50, verbose_name='客户开始条码'),
        ),
        migrations.AddField(
            model_name='productionplan',
            name='customer_end_barcode',
            field=models.CharField(blank=True, max_length=50, verbose_name='客户结束条码'),
        ),
        migrations.AddField(
            model_name='workorder',
            name='customer_start_barcode',
            field=models.CharField(blank=True, max_length=50, verbose_name='客户开始条码'),
        ),
        migrations.AddField(
            model_name='workorder',
            name='customer_end_barcode',
            field=models.CharField(blank=True, max_length=50, verbose_name='客户结束条码'),
        ),
    ]
