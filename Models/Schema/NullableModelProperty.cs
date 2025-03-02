using System;

namespace GoogleDrivePushCli.Models.Schema;

public class NullableModelProperty<T>(
    string name, PropertyType propertyType, bool isNullable,
    Func<T, object> getter, Action<T, object> setter
) : ModelProperty<T>(name, propertyType, getter, setter) where T : class
{
    public readonly bool isNullable = isNullable;
}