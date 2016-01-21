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