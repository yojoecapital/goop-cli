using System;

namespace GoogleDrivePushCli.Models.Schema;

public class ModelProperty<T>(
    string name, PropertyType propertyType,
    Func<T, object> getter, Action<T, object> setter
) where T : class
{
    public readonly string name = name;
    public readonly PropertyType propertyType = propertyType;
    public readonly Func<T, object> getter = getter;
    public readonly Action<T, object> setter = setter;
}