using System.Configuration;

namespace EncryptFolders.Config
{
    public class FolderConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("folders", IsRequired = true)]
        [ConfigurationCollection(typeof(FolderConfigurationElement), 
            AddItemName = "add", 
            ClearItemsName = "clear", 
            RemoveItemName = "remove")]
        public GenericConfigurationElementCollection<FolderConfigurationElement> Folders
        {
            get
            {
                return (GenericConfigurationElementCollection<FolderConfigurationElement>)this["folders"];
            }
        }
    }
}