using Microsoft.Data.Sqlite;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Models.Schema;
using System.Collections;
using System.Collections.Generic;

namespace GoogleDrivePushCli.Repositories;

public abstract class Repository<T>(ModelSchema<T> modelSchema, SqliteConnection connection) where T : class, new()
{
    protected readonly ModelSchema<T> modelSchema = modelSchema;
    protected readonly SqliteConnection connection = connection;

    protected T CreateModelFrom(SqliteDataReader reader)
    {
        var model = new T();
        modelSchema.SetValues(model, reader);
        return model;
    }

    public void CreateTable()
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.CreateTableCommandText;
        command.ExecuteNonQuery();
    }

    public T SelectByKey(object valueOfKey)
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.SelectByKeyCommandText;
        command.Parameters.AddWithValue(modelSchema.KeyName, valueOfKey);
        var reader = command.ExecuteReader();
        if (!reader.Read()) return default;
        return CreateModelFrom(reader);
    }

    public IEnumerable<T> SelectAll()
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.SelectAllCommandText;
        var reader = command.ExecuteReader();
        while (reader.Read()) yield return CreateModelFrom(reader);
    }

    public int DeleteByKey(object valueOfKey)
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.DeleteByKeyCommandText;
        command.Parameters.AddWithValue(modelSchema.KeyName, valueOfKey);
        return command.ExecuteNonQuery();
    }

    public int DeleteAll()
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.DeleteAllCommandText;
        return command.ExecuteNonQuery();
    }

    public int Insert(T model)
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.InsertCommandText;
        // modelSchema.SetParameterWithKey(command, model.GetValueOfKey());
        // model.PopulatePropertiesInto((propertyIndex, value) => modelSchema.SetParameter(command, propertyIndex, value));
        return command.ExecuteNonQuery();
    }

    public int Update(T model)
    {
        var command = connection.CreateCommand();
        command.CommandText = modelSchema.UpdateByKeyCommandText;
        // modelSchema.SetParameterWithKey(command, model.GetValueOfKey());
        // model.PopulatePropertiesInto((propertyIndex, value) => modelSchema.SetParameter(command, propertyIndex, value));
        return command.ExecuteNonQuery();
    }
}