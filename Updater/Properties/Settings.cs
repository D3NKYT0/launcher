using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Updater.Properties
{
	[CompilerGenerated]
	[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.5.0.0")]
	internal sealed class Settings : ApplicationSettingsBase
	{
		private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

		public static Settings Default => defaultInstance;

		[UserScopedSetting]
		[DebuggerNonUserCode]
		[DefaultSettingValue("1")]
		public int Lang
		{
			get
			{
				return (int)this["Lang"];
			}
			set
			{
				this["Lang"] = value;
			}
		}

		[UserScopedSetting]
		[DebuggerNonUserCode]
		[DefaultSettingValue("")]
		public string SavePath
		{
			get
			{
				return (string)this["SavePath"];
			}
			set
			{
				this["SavePath"] = value;
			}
		}
	}
}
