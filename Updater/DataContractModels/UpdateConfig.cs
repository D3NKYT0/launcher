using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("UpdateConfig")]
	public class UpdateConfig
	{
		public string UpdaterTitle
		{
			get;
			set;
		}

		public string SelfUpdatePath
		{
			get;
			set;
		}

		public int UpdaterVersion
		{
			get;
			set;
		} = 1;


		public string GameStartPathRu
		{
			get;
			set;
		}

		public string GameStartPathEng
		{
			get;
			set;
		}

		public string PatchPath
		{
			get;
			set;
		}

		public string SiteLink
		{
			get;
			set;
		}

		public string RegLink
		{
			get;
			set;
		}

		public string AboutServerLink
		{
			get;
			set;
		}

		public string ForumLink
		{
			get;
			set;
		}

		public string HelpLink
		{
			get;
			set;
		}

		public string BonusLink
		{
			get;
			set;
		}

		public string FBLink
		{
			get;
			set;
		}

		public string DiscordLink
		{
			get;
			set;
		}

		public string TelegramLink
		{
			get;
			set;
		}

		public string VkLink
		{
			get;
			set;
		}

		public string SupportLink
		{
			get;
			set;
		}

		public string DonationLink
		{
			get;
			set;
		}

		public string CabinetLink
		{
			get;
			set;
		}

		public string L2Top
		{
			get;
			set;
		}

		public string MMOTop
		{
			get;
			set;
		}

		public string DownloadLink1
		{
			get;
			set;
		}

		public string DownloadLink2
		{
			get;
			set;
		}

		public string DownloadLink3
		{
			get;
			set;
		}
	}
}
