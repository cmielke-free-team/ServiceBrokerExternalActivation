using System.Configuration;

namespace EmdatSSBEAService.Configuration
{
    [ConfigurationCollection(typeof(ApplicationElement), AddItemName = "Application")]
    public class ApplicationsElement : ConfigurationElementCollection
    {
        public ApplicationElement this[int index]
        {
            get { return (ApplicationElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ApplicationElement application)
        {
            BaseAdd(application);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ApplicationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ApplicationElement)element).AppName;
        }

        public void Remove(ApplicationElement application)
        {
            BaseRemove(application.AppName);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }
}