using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Filters;
using System.Web.Security;

namespace NitroBolt.WebSampler
{
    public class AuthenticationFilter : IAuthenticationFilter
    {
        public bool AllowMultiple
        {
            get
            {
                return true;
            }
        }

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var cookie = context.Request.Headers.GetCookies(FormsAuthentication.FormsCookieName).FirstOrDefault();
            if (cookie != null)
            {
                try
                {
                    var authTicket = FormsAuthentication.Decrypt(cookie[FormsAuthentication.FormsCookieName].Value);
                    context.Principal = AuthHlp.ToPrincipal(authTicket.Name);
                }
                catch
                {

                }
            }
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {

        }
    }
}