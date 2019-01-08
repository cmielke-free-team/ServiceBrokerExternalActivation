using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmdatSSBEAService.Configuration
{
    public class NotificationServiceSection : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString")]
        public ServiceElement ConnectionString
        {
            get
            {
                return this["ConnectionString"] as ServiceElement;
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        [ConfigurationProperty("StoredProcedure")]
        public ServiceElement StoredProcedure
        {
            get
            {
                return this["StoredProcedure"] as ServiceElement;
            }
            set
            {
                this["StoredProcedure"] = value;
            }
        }

        [ConfigurationProperty("Applications")]
        public ApplicationsElement Applications
        {
            get
            {
                object o = this["Applications"];
                return o as ApplicationsElement;
            }
        }

        public NotificationServiceConfig CreateNotificationServiceFromConfig()
        {
            return new NotificationServiceConfig()
            {
                ConnectionString = this.ConnectionString.InnerText,
                StoredProcedure = this.StoredProcedure.InnerText
            };
        }
    }
}
