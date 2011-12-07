using System.Configuration;

namespace EncryptFolders.Config
{
    public class FolderConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get
            {
                return (string)this["path"];
            }

            set
            {
                this["path"] = value;
            }
        }

        public static implicit operator string(FolderConfigurationElement folder)
        {
            return folder.Path;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}