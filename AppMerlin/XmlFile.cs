using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AppMerlin
{
  public class XmlFile : FileInput
  {
    private XDocument m_XDoc;
    private FileStream m_FileStream;
    private XmlSerializer m_Serializer;

    public XmlFile(string file) 
      : base(file)
    {
      m_XDoc = new XDocument();
    }

    public XmlFile(string filePath, string fileName)
      : base(filePath, fileName)
    {
      m_XDoc = new XDocument();
    }
    
    public XDocument GetXDocument()
    {
      return m_XDoc;
    }

    public override bool TryFileLoad<T>()
    {
      m_Serializer = new XmlSerializer(typeof(T));
      m_Serializer.UnreferencedObject += Serializer_UnreferencedObject;
      m_Serializer.UnknownAttribute += Serializer_UnknownAttribute;
      m_Serializer.UnknownElement += Serializer_UnknownElement;
      m_Serializer.UnknownNode += Serializer_UnknownNode;
      try
      {
        m_FileStream = new FileStream(m_FileName, FileMode.Open);
      }
      catch (Exception ex)
      {
        m_ErrorMessage = ex.Message + "\n\nCheck file path and read only attribute.";
        m_HelperMessage = "File could not be read or does not exist";
        return false;
      }

      return true;
    }
    
    public bool SerializeToFile<T>(T item)
    {
      XmlSerializer x = new XmlSerializer(typeof(T));
      TextWriter writer = new StreamWriter(m_FileName);
      try
      {
        x.Serialize(writer, item);
      }
      catch (Exception ex)
      {
        m_ErrorMessage = ex.Message;
        m_HelperMessage = "Error Serializing object of type: " + typeof(T).ToString();
        return false;
      }
      writer.Close();
      return true;
    }

    public bool DeserializeFromFile<T>(ref T item)
    {
      try
      {
        item = (T)m_Serializer.Deserialize(m_FileStream);
      }
      catch (Exception ex)
      {
        m_ErrorMessage = ex.Message;
        m_HelperMessage = "Corrupt or Bad File";
        return false;
      }
      m_FileStream.Close();
      return true;
    }

    public bool DeserializeProjectFile(ref TMCProject project)
    {
      try
      {
        project = (TMCProject)m_Serializer.Deserialize(m_FileStream);
      }
      catch (Exception ex)
      {
        m_ErrorMessage = ex.Message;
        m_HelperMessage = "Corrupt or Bad Project File, Check Selection";
        return false;
      }
      m_FileStream.Close();
      return true;
    }

    private void Serializer_UnreferencedObject (object sender, UnreferencedObjectEventArgs e)
    {
      Console.WriteLine("UnreferencedObject:");
    }

    private void Serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
    {
      Console.WriteLine("UnreferencedObject:");
    }

    private void Serializer_UnknownElement(object sender, XmlElementEventArgs e)
    {
      Console.WriteLine("UnreferencedObject:");
    }

    private void Serializer_UnknownNode(object sender, XmlNodeEventArgs e)
    {
      Console.WriteLine("UnreferencedObject:");
    }
  }
}
