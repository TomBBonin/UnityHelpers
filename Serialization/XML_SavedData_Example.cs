/*
 * A Data Container for XML Serialization. 
 * Ideally i would rather this be a struct for default public fields, but struct 
 * are copied when passed to functions in C#, so to avoid the hassle down the road,
 * use a class with all public members.
 * 
 * https://github.com/tombbonin
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
[XmlRoot("Saved_Data")]
public class SavedData
{
    // If you want a field in your class not to be serialized into the file, use this keyword
    [XmlIgnoreAttribute] public const string PlayerName = "PLAYER_NAME";

    // The documentation claims that this keyword allows to serialize private fields,
    // but i've never been able to get that to work, so i'm not sure... Currently it 
    // seems that only public fields are serialized.
    // It's other cool use is to expose private variables in the editor
    [SerializeField] private int _privateField = 8000;

    // Fields you want to write to the File
    public int Field1;
    public int Field2;

    // List of items from a SubClass you'd like to serialize
    [XmlArray("SubClasses")]
    [XmlArrayItem("SubC")]
    public List<SavedDataSubClass> SubClasses;

    public SavedData()
    {
        SubClasses = new List<SavedDataSubClass>();
        AssignDefaultValues();
    }

    public void AssignDefaultValues()
    {
        Field1 = -1;
        Field2 = -1;
        SubClasses.Clear();
    }

    public void RandomizeFields()
    {
        Field1 = Random.Range(0, 1000);
        Field2 = Random.Range(0, 1000);

        SubClasses.Clear();
        int nbSubCs = Random.Range(2, 5);
        for (int i = 0; i < nbSubCs; i++)
            SubClasses.Add(new SavedDataSubClass());
    }
}

[System.Serializable]
public class SavedDataSubClass
{
    public string _subField1;
    public string _subField2;
    private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public SavedDataSubClass()
    {
        _subField1 = GetRandomString();
        _subField2 = GetRandomString();
    }

    private string GetRandomString()
    {
        string retString = "";
        int strLength = Random.Range(2, 10);
        for (int i = 0; i < strLength; i++)
        {
            retString += ALPHABET[Random.Range(0, ALPHABET.Length)];
        }
        return retString;
    }
}