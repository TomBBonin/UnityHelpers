/*
 * Serialization helper for loading / saving objects.
 * Identical to the Binary version but the file is human readbale.
 * See XML_SavedData example file to see how to setup your data class
 * 
 * Use Example : 
 * 
 * SavedData _data;
 * bool LoadData(string dataFileName)
 * {
 *     bool loadSuccess;
 *     if (loadSuccess = ReadWriteXML.Load<SavedData>(ref _data, dataFileName))
 *         Debug.Log("Loaded Data : " + dataFileName);
 *     else
 *     {
 *         Debug.LogError("FAILED to Load Data, loading default");
 *         _data = new SavedData();
 *     }
 * }
 * 
 * bool SaveData(string dataFileName)
 * {
 *     bool saveSuccess;
 *     if (saveSuccess = ReadWriteXML.Save<SavedData>(_data, dataFileName))
 *         Debug.Log("Saved Data");
 *     else
 *         Debug.Log("FAILED to Save Data");
 * }
 * 
 * 
 * https://github.com/tombbonin
 */

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
