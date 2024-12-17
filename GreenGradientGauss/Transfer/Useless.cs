using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTCP_Project
{
    internal class Useless
    {
        internal Useless()
        {
        }

        public void BakeCookies() => throw new CookieException();
    }
}
