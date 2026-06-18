# apps/batch_tracking/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import BatchTrackingViewSet

router = DefaultRouter()
router.register(r'batches', BatchTrackingViewSet, basename='batch-tracking')

urlpatterns = [
    path('', include(router.urls)),
]