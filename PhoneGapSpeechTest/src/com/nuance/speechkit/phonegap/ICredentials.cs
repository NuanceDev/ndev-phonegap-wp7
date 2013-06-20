using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.nuance.speechkit.phonegap
{
    interface ICredentials
    {

        /**
         * Returns the application ID
         */
        string getAppId();
        /**
         * Returns the application key
         */
        byte[] getAppKey();

    }
}
