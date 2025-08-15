using System.Xml.Serialization;

namespace Updater.DataContractModels
{
	[XmlRoot("UpdateConfig")]
	public class UpdateConfig
	{
		public required string UpdaterTitle
		{
			get;
			set;
		}

		public required string SelfUpdatePath
		{
			get;
			set;
		}

		public int UpdaterVersion
		{
			get;
			set;
		} = 1;


		public required string GameStartPathRu
		{
			get;
			set;
		}

		public required string GameStartPathEng
		{
			get;
			set;
		}

		public required string PatchPath
		{
			get;
			set;
		}

		public required string SiteLink
		{
			get;
			set;
		}

		public required string RegLink
		{
			get;
			set;
		}

		public required string AboutServerLink
		{
			get;
			set;
		}

		public required string ForumLink
		{
			get;
			set;
		}

		public required string HelpLink
		{
			get;
			set;
		}

		public required string BonusLink
		{
			get;
			set;
		}

		public required string FBLink
		{
			get;
			set;
		}

		public required string DiscordLink
		{
			get;
			set;
		}

		public required string TelegramLink
		{
			get;
			set;
		}

		public required string VkLink
		{
			get;
			set;
		}

		public required string SupportLink
		{
			get;
			set;
		}

		public required string DonationLink
		{
			get;
			set;
		}

		public required string CabinetLink
		{
			get;
			set;
		}

		public required string L2Top
		{
			get;
			set;
		}

		public required string MMOTop
		{
			get;
			set;
		}

		public required string DownloadLink1
		{
			get;
			set;
		}

		public required string DownloadLink2
		{
			get;
			set;
		}

		public required string DownloadLink3
		{
			get;
			set;
		}
	}
}
