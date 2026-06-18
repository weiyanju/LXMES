import django.db.models.deletion
from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('product', '0001_initial'),
        ('production', '0001_initial'),
    ]

    operations = [
        migrations.AddField(
            model_name='productionplan',
            name='product',
            field=models.ForeignKey(
                blank=True,
                null=True,
                on_delete=django.db.models.deletion.PROTECT,
                related_name='production_plans',
                to='product.product',
                verbose_name='产品',
            ),
        ),
        migrations.AddField(
            model_name='workorder',
            name='product',
            field=models.ForeignKey(
                blank=True,
                null=True,
                on_delete=django.db.models.deletion.PROTECT,
                related_name='work_orders',
                to='product.product',
                verbose_name='产品',
            ),
        ),
    ]
