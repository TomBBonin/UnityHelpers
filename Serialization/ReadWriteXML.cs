using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml.Serialization;

public class ReadWriteXML
{
    public static bool Save<ObjType>(ObjType data, string fileName)
    {
        bool success;
        try
        {
            XmlSerializer XMLwriter = new XmlSerializer(typeof(ObjType));
            using (StreamWriter write = new StreamWriter(fileName))
            {
                XMLwriter.Serialize(write, data);
            }
            success = true;
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.ToString());
            success = false;
        }

        return success;
    }

    public static bool Load<ObjType>(ref ObjType data, string fileName)
    {
        bool success = false;
        try
        {
            XmlSerializer XMLreader = new XmlSerializer(typeof(ObjType));
            Stream dataStream = new FileStream(fileName, FileMode.Open);
            data = (ObjType)XMLreader.Deserialize(dataStream);
            dataStream.Close();
            success = true;
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.ToString());
            success = false;
        }
        return success;
    }
}
