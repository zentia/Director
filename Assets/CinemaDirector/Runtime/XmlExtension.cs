using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

public static class XmlExtension
{
    public static void Insert(this XmlNode xmlNode, XmlNode childNode, int index)
    {
        if (xmlNode.ChildNodes.Count == 0 || index < 0)
        {
            xmlNode.AppendChild(childNode);
            return;
        }
        var refNode = xmlNode.ChildNodes[index];
        xmlNode.InsertBefore(childNode, refNode);
    }

    public static object GetValue(this XmlNode xmlNode, Type type)
    {
        var val = xmlNode.Value;
        if (type.IsEnum)
        {
            
            int value;
            if (!int.TryParse(val, out value))
            {
                return Enum.Parse(type, val, true);
            }
            return Enum.ToObject(type, value);
        }
        switch(Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return bool.Parse(val);
            case TypeCode.Int32:
                return int.Parse(val);
            case TypeCode.Single:
                if (string.IsNullOrEmpty(val))
                    return 0f;
                return float.Parse(val);
            case TypeCode.String:
                return val;
        }
        return val;
    }

    public static void Serialize(object item, string path)
    {
        var serializer = new XmlSerializer(item.GetType());
        var writer = new StreamWriter(path);
        serializer.Serialize(writer.BaseStream, item);
        writer.Close();
    }

    public static T Deserialize<T>(string path)
    {
        var serializer = new XmlSerializer(typeof(T));
        var reader = new StreamReader(path);
        var deserializer = (T) serializer.Deserialize(reader.BaseStream);
        reader.Close();
        return deserializer;
    }
}