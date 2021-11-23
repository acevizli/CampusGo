from django.db import models
from django.contrib.auth.models import User
# Create your models here.
class Images(models.Model):
    id = models.AutoField(primary_key=True, editable=True)
    user = models.ForeignKey(User, on_delete=models.SET_NULL, null=True)
    name = models.CharField(max_length=150, null=True, blank=True)
    image = models.ImageField(null=True, blank=True)

    def __str__(self):
        return self.name
class Location(models.Model):
    id = models.AutoField(primary_key=True, editable=True)
    user = models.ForeignKey(User, on_delete=models.SET_NULL, null=True)
    longitude = models.FloatField(null=True, blank=True)
    latitude = models.FloatField(null=True, blank=True)
    accuracy = models.FloatField(null=True, blank=True)
    image = models.ForeignKey(Images, on_delete=models.SET_NULL, null=True)

    def __str__(self):
        return str(self.id)


