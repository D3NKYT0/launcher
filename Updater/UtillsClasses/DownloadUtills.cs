using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using Updater.DataContractModels;

namespace Updater.UtillsClasses
{
	public static class DownloadUtills
	{
		public static UpdateInfoModel GetUpdateInfo(string url)
		{
			string url2 = Path.Combine(url, "UpdateInfo.xml");
			XmlDocument xmlFromUrl = GetXmlFromUrl(url2);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(UpdateInfoModel));
			using TextReader textReader = new StringReader(xmlFromUrl.InnerXml);
			return (UpdateInfoModel)xmlSerializer.Deserialize(textReader);
		}

		public static UpdateConfig GetUpdateConfig(string url)
		{
			string url2 = Path.Combine(url, "UpdateConfig.xml");
			XmlDocument xmlFromUrl = GetXmlFromUrl(url2);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(UpdateConfig));
			using TextReader textReader = new StringReader(xmlFromUrl.InnerXml);
			return (UpdateConfig)xmlSerializer.Deserialize(textReader);
		}

		private static XmlDocument GetXmlFromUrl(string url)
		{
			XmlDocument xmlDocument = new XmlDocument();
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "GET";
			httpWebRequest.Accept = "application/json";
			using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
			{
				xmlDocument.Load(httpWebResponse.GetResponseStream());
			}
			return xmlDocument;
		}
	}
}
