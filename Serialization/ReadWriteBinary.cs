/*
 * Serialization helper for loading / saving objects
 * Identical to the XML version but the Binary file isn't human readbale.
 * See XML_SavedData example file to see how to setup your data class
 * 
 * Use Example : 
 * 
 * MyMapClass _map;
 * bool LoadMap(string mapName)
 * {
 *     bool loadSuccess;
 *     if (loadSuccess = ReadWriteBinary.Load<MyMapClass>(ref _map, mapName))
 *         Debug.Log("Loaded Map : " + mapName);
 *     else
 *     {
 *         Debug.LogError("FAILED to Load Map, loading default");
 *         _map = new MyMapClass();
 *     }
 * }
 * 
 * bool SaveMap(string mapName)
 * {
 *     bool saveSuccess;
 *     if (saveSuccess = ReadWriteBinary.Save<MyMapClass>(_map, mapName))
 *         Debug.Log("Saved map");
 *     else
 *         Debug.Log("FAILED to Save map");
 * }
 * 
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class ReadWriteBinary
{
	public static bool Save<ObjType>(ObjType data, string fileName)
	{
		bool success;
		try
		{
			BinaryFormatter binaryWriter = new BinaryFormatter();
			FileStream file = new FileStream(fileName, FileMode.Create);
            binaryWriter.Serialize(file, data);
			file.Close();
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
            BinaryFormatter binaryReader = new BinaryFormatter();
            FileStream file = new FileStream(fileName, FileMode.Open);
            data = (ObjType)binaryReader.Deserialize(file);
            file.Close();
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