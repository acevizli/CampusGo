from rest_framework import serializers
from .models import Location,Images
from django.contrib.auth.models import User
from rest_framework_simplejwt.tokens import RefreshToken


class serializeUser(serializers.ModelSerializer):

    class Meta:
        model = User
        fields = [
            "id",
            "username",
            "is_staff"
        ]
class serializeUserWithToken(serializeUser):
    token = serializers.SerializerMethodField(read_only=True)

    class Meta:
        model = User
        fields = [
            "id",
            "username",
            "is_staff",
            "token",
        ]

    def get_token(self, object):
        token = RefreshToken.for_user(object)
        return str(token.access_token)

class serializeLocations(serializers.ModelSerializer):
    imagename = serializers.SerializerMethodField(read_only=True)
    class Meta:
        model = Location
        fields = [
            "id",
            "user",
            "imagename"
        ]
    def get_imagename(self,object):
        return Images.objects.get(id=object.image.id).name