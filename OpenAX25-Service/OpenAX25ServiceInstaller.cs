using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace OpenAX25_Service
{
    [RunInstaller(true)]
    public class OpenAX25ServiceInstaller : Installer
    {

        public OpenAX25ServiceInstaller()
        {
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;
            ServiceInstaller serviceInstaller = new ServiceInstaller();
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.ServiceName = OpenAX25Service.SERVICE_NAME;
            serviceInstaller.DisplayName = "OpenAX25 Service";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

    }
}
